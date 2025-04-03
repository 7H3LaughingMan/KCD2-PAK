using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using KCD2K.PAK;

namespace KCD2.PAK;

public class PakArchiveEntry
{
    private readonly PakVersionMadeByPlatform _versionMadeByPlatform;
    private PakVersionNeededValues _versionMadeBySpecification;
    private PakVersionNeededValues _versionToExtract;
    private BitFlagValues _generalPurposeBitFlag;
    private CompressionMethodValues _storedCompressionMethod;
    private DateTimeOffset _lastModified;
    private long _compressedSize;
    private long _uncompressedSize;
    private long _offsetOfLocalHeader;
    private uint _crc32;
    private uint _externalFileAttr;
    private string _storedEntryName = string.Empty;
    private byte[] _storedEntryNameBytes = [];
    private readonly CompressionLevel _compressionLevel;

    internal PakArchiveEntry(string entryName, CompressionLevel compressionLevel) : this(entryName)
    {
        _compressionLevel = compressionLevel;
        if (_compressionLevel == CompressionLevel.NoCompression)
            CompressionMethod = CompressionMethodValues.Stored;
        _generalPurposeBitFlag = MapDeflateCompressionOptions(_generalPurposeBitFlag, _compressionLevel, CompressionMethod);
    }

    internal PakArchiveEntry(RelativePath entryName, CompressionLevel compressionLevel) : this(entryName.ToString('/'), compressionLevel)
    {

    }

    internal PakArchiveEntry(string entryName)
    {
        _versionMadeByPlatform = PakVersionMadeByPlatform.Windows;
        _versionMadeBySpecification = PakVersionNeededValues.Default;
        _versionToExtract = PakVersionNeededValues.Default;
        _compressionLevel = CompressionLevel.Optimal;
        CompressionMethod = CompressionMethodValues.Deflate;
        _generalPurposeBitFlag = MapDeflateCompressionOptions(0, _compressionLevel, CompressionMethod);
        _lastModified = DateTimeOffset.Now;

        _compressedSize = 0;
        _uncompressedSize = 0;
        _externalFileAttr = 0;

        _offsetOfLocalHeader = 0;
        _crc32 = 0;

        FullName = entryName;

        if (_storedEntryNameBytes.Length > ushort.MaxValue)
            throw new ArgumentException("Entry names cannont require more than 2^16 bits.");
    }

    internal PakArchiveEntry(RelativePath entryName) : this(entryName.ToString('/'))
    {

    }

    public uint Crc32 => _crc32;

    public long TotalLength => 76 + _compressedSize + (_storedEntryNameBytes.Length * 2);

    public long CompressedLength => _compressedSize;

    public string FullName
    {
        get => _storedEntryName;
        private set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(FullName));

            _storedEntryNameBytes = PakHelper.GetEncodedTruncatedBytesFromString(value, null, 0, out bool isUTF8);
            _storedEntryName = value;

            if (isUTF8)
                _generalPurposeBitFlag |= BitFlagValues.UnicodeFileNameAndComment;
            else
                _generalPurposeBitFlag &= ~BitFlagValues.UnicodeFileNameAndComment;

            DetectEntryNameVersion();
        }
    }

    public DateTimeOffset LastWriteTime
    {
        get => _lastModified;
        set => _lastModified = value;
    }

    public long Length => _uncompressedSize;

    public string Name => ParseFileName(FullName, _versionMadeByPlatform);

    internal long OffsetOfLocalHeader => _offsetOfLocalHeader;

    public Stream Open(MemoryStream memoryStream) => GetDataCompressor(memoryStream, true, (sender, e) =>
    {
        memoryStream.Position = 0;
    });

    public override string ToString() => FullName;

    private CompressionMethodValues CompressionMethod
    {
        get => _storedCompressionMethod;
        set
        {
            if (value == CompressionMethodValues.Deflate)
                VersionToExtractAtLeast(PakVersionNeededValues.Deflate);
            else if (value == CompressionMethodValues.Deflate64)
                VersionToExtractAtLeast(PakVersionNeededValues.Deflate64);
            _storedCompressionMethod = value;
        }
    }

    internal void WriteCentralDirectoryFileHeader(Stream baseStream)
    {
        Span<byte> cdStaticHeader = stackalloc byte[46];

        // Central Directory File Header
        cdStaticHeader[0] = 0x50;
        cdStaticHeader[1] = 0x4B;
        cdStaticHeader[2] = 0x01;
        cdStaticHeader[3] = 0x02;

        cdStaticHeader[4] = (byte)_versionMadeBySpecification;
        cdStaticHeader[5] = (byte)PakVersionMadeByPlatform.Windows;
        BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[6..], (ushort)_versionToExtract);
        BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[8..], (ushort)_generalPurposeBitFlag);
        BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[10..], (ushort)CompressionMethod);
        BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[12..], PakHelper.DateTimeToDosTime(_lastModified.DateTime));
        BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[16..], _crc32);
        BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[20..], (uint)_compressedSize);
        BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[24..], (uint)_uncompressedSize);
        BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[28..], (ushort)_storedEntryNameBytes.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[38..], _externalFileAttr);
        BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[42..], (uint)_offsetOfLocalHeader);

        baseStream.Write(cdStaticHeader);
        baseStream.Write(_storedEntryNameBytes);
    }

    private void DetectEntryNameVersion()
    {
        if (ParseFileName(_storedEntryName, _versionMadeByPlatform) == "")
            VersionToExtractAtLeast(PakVersionNeededValues.ExplicitDirectory);
    }

    private CheckSumAndSizeWriteStream GetDataCompressor(Stream backingStream, bool leaveBackingStreamOpen, EventHandler? onClose)
    {
        Debug.Assert(CompressionMethod == CompressionMethodValues.Deflate || CompressionMethod == CompressionMethodValues.Stored);

        bool isIntermediateStream = true;
        Stream compressorStream;
        switch (CompressionMethod)
        {
            case CompressionMethodValues.Stored:
                compressorStream = backingStream;
                isIntermediateStream = false;
                break;
            case CompressionMethodValues.Deflate:
            case CompressionMethodValues.Deflate64:
            default:
                compressorStream = new DeflateStream(backingStream, _compressionLevel, leaveBackingStreamOpen);
                break;
        }

        bool leaveCompressorStreamOpenOnClose = leaveBackingStreamOpen && !isIntermediateStream;
        var checkSumStream = new CheckSumAndSizeWriteStream(
            compressorStream,
            backingStream,
            leaveCompressorStreamOpenOnClose,
            this,
            onClose,
            (initialPosition, currentPosition, checkSum, backing, thisRef, closeHandler) =>
            {
                thisRef._crc32 = checkSum;
                thisRef._uncompressedSize = currentPosition;
                thisRef._compressedSize = backing.Position - initialPosition;
                closeHandler?.Invoke(thisRef, EventArgs.Empty);
            }
        );

        return checkSumStream;
    }

    private static CompressionLevel MapCompressionLevel(BitFlagValues generalPurposeBitFlag, CompressionMethodValues compressionMethod)
    {
        if (compressionMethod == CompressionMethodValues.Deflate || compressionMethod == CompressionMethodValues.Deflate64)
        {
            return ((int)generalPurposeBitFlag & 0x6) switch
            {
                0 => CompressionLevel.Optimal,
                2 => CompressionLevel.SmallestSize,
                4 => CompressionLevel.Fastest,
                6 => CompressionLevel.Fastest,
                _ => CompressionLevel.Optimal,
            };
        }
        else
        {
            return CompressionLevel.NoCompression;
        }
    }

    private static BitFlagValues MapDeflateCompressionOptions(BitFlagValues generalPurposeBitFlag, CompressionLevel compressionLevel, CompressionMethodValues compressionMethod)
    {
        ushort deflateCompressionOptions = (ushort)(
            compressionMethod == CompressionMethodValues.Deflate || compressionMethod == CompressionMethodValues.Deflate64
                ? compressionLevel switch
                {
                    CompressionLevel.Optimal => 0,
                    CompressionLevel.SmallestSize => 2,
                    CompressionLevel.Fastest => 6,
                    CompressionLevel.NoCompression => 6,
                    _ => 0
                }
                : 0);

        return (BitFlagValues)(((int)generalPurposeBitFlag & ~0x6) | deflateCompressionOptions);
    }

    internal void WriteLocalFileHeader(Stream baseStream)
    {
        _offsetOfLocalHeader = baseStream.Position;

        Span<byte> lfStaticHeader = stackalloc byte[30];

        // Local File Header
        lfStaticHeader[0] = 0x50;
        lfStaticHeader[1] = 0x4B;
        lfStaticHeader[2] = 0x03;
        lfStaticHeader[3] = 0x04;

        BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader[4..], (ushort)_versionToExtract);
        BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader[6..], (ushort)_generalPurposeBitFlag);
        BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader[8..], (ushort)CompressionMethod);
        BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader[10..], PakHelper.DateTimeToDosTime(_lastModified.DateTime));
        BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader[14..], _crc32);
        BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader[18..], (uint)_compressedSize);
        BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader[22..], (uint)_uncompressedSize);
        BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader[26..], (ushort)_storedEntryNameBytes.Length);

        baseStream.Write(lfStaticHeader);
        baseStream.Write(_storedEntryNameBytes);
    }

    private void VersionToExtractAtLeast(PakVersionNeededValues value)
    {
        if (_versionToExtract < value)
            _versionToExtract = value;
        if (_versionMadeBySpecification < value)
            _versionMadeBySpecification = value;
    }

    internal static string ParseFileName(string path, PakVersionMadeByPlatform madeByPlatform) => madeByPlatform == PakVersionMadeByPlatform.Windows ? GetFileName_Windows(path) : GetFileName_Unix(path);

    private static string GetFileName_Windows(string path)
    {
        int i = path.AsSpan().LastIndexOfAny('\\', '/', ':');
        return i >= 0
            ? path[(i + 1)..]
            : path;
    }

    private static string GetFileName_Unix(string path)
    {
        int i = path.LastIndexOf('/');
        return i >= 0
            ? path[(i + 1)..]
            : path;
    }

    [Flags]
    internal enum BitFlagValues : ushort
    {
        IsEncrypted = 0x1,
        DataDescriptor = 0x8,
        UnicodeFileNameAndComment = 0x800
    }

    internal enum CompressionMethodValues : ushort
    {
        Stored = 0x0,
        Deflate = 0x8,
        Deflate64 = 0x9,
        BZip2 = 0xC,
        LZMA = 0xE
    }
}
