using System.Buffers.Binary;
using System.Collections.ObjectModel;

namespace KCD2.PAK;

public class PakArchive : IDisposable
{
    private static readonly long _pakSizeLimit = (long)Bytes.FromGigabytes(2);
    private readonly Stream _archiveStream;
    private readonly List<PakArchiveEntry> _entries;
    private readonly ReadOnlyCollection<PakArchiveEntry> _entriesCollection;
    private readonly bool _leaveOpen;
    private long _fileSize;
    private bool _isDisposed;

    public PakArchive(Stream stream, bool leaveOpen = false)
    {
        if (!stream.CanWrite)
            throw new ArgumentException("Cannot use create mode on a non-writable stream.");

        _archiveStream = stream;
        _entries = [];
        _entriesCollection = new(_entries);
        _leaveOpen = leaveOpen;
        _fileSize = 22;
        _isDisposed = false;
    }

    public ReadOnlyCollection<PakArchiveEntry> Entries
    {
        get
        {
            ThrowIfDisposed();
            return _entriesCollection;
        }
    }

    public long Length => _fileSize;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            WriteFile();
            CloseStreams();
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool CheckEntry(PakArchiveEntry entry)
    {
        if (_entries.Count + 1 > ushort.MaxValue)
            return false;

        if (_fileSize + entry.TotalLength > _pakSizeLimit)
            return false;

        return true;
    }

    public void AddEntry(PakArchiveEntry entry, Stream data)
    {
        _entries.Add(entry);
        _fileSize += entry.TotalLength;
        entry.WriteLocalFileHeader(_archiveStream);
        data.CopyTo(_archiveStream);
    }

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    private void CloseStreams()
    {
        if (!_leaveOpen)
            _archiveStream.Dispose();
    }

    private void WriteFile()
    {
        long startOfCentralDirectory = _archiveStream.Position;

        foreach (PakArchiveEntry entry in _entries)
            entry.WriteCentralDirectoryFileHeader(_archiveStream);

        long sizeOfCentralDirectory = _archiveStream.Position - startOfCentralDirectory;

        WriteEpilogue(startOfCentralDirectory, sizeOfCentralDirectory);
    }

    private void WriteEpilogue(long startOfCentralDirectory, long sizeOfCentralDirectory)
    {
        Span<byte> eocdBlock = stackalloc byte[22];

        // End Of Central Directory Record
        eocdBlock[0] = 0x50;
        eocdBlock[1] = 0x4B;
        eocdBlock[2] = 0x05;
        eocdBlock[3] = 0x06;

        BinaryPrimitives.WriteUInt16LittleEndian(eocdBlock[4..], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(eocdBlock[6..], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(eocdBlock[8..], (ushort)_entries.Count);
        BinaryPrimitives.WriteUInt16LittleEndian(eocdBlock[10..], (ushort)_entries.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(eocdBlock[12..], (uint)sizeOfCentralDirectory);
        BinaryPrimitives.WriteUInt32LittleEndian(eocdBlock[16..], (uint)startOfCentralDirectory);
        BinaryPrimitives.WriteUInt16LittleEndian(eocdBlock[20..], 0);

        _archiveStream.Write(eocdBlock);
    }
}
