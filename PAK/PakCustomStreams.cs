

using System.Diagnostics;
using System.IO.Hashing;
using KCD2.PAK;

namespace KCD2K.PAK;

public class CheckSumAndSizeWriteStream : Stream
{
    private readonly Stream _baseStream;
    private readonly Stream _baseBaseStream;
    private long _position;
    private Crc32 _checksum;
    private readonly bool _leaveOpenOnClose;
    private readonly bool _canWrite;
    private bool _isDisposed;
    private bool _everWritten;
    private long _initialPosition;
    private readonly PakArchiveEntry _pakArchiveEntry;
    private readonly EventHandler? _onClose;
    private readonly Action<long, long, uint, Stream, PakArchiveEntry, EventHandler?> _saveCrcAndSizes;

    public CheckSumAndSizeWriteStream(Stream baseStream, Stream baseBaseStream, bool leaveOpenOnClose, PakArchiveEntry entry,
        EventHandler? onClose, Action<long, long, uint, Stream, PakArchiveEntry, EventHandler?> saveCrcAndSizes)
    {
        _baseStream = baseStream;
        _baseBaseStream = baseBaseStream;
        _position = 0;
        _checksum = new();
        _leaveOpenOnClose = leaveOpenOnClose;
        _canWrite = true;
        _isDisposed = false;
        _initialPosition = 0;
        _pakArchiveEntry = entry;
        _onClose = onClose;
        _saveCrcAndSizes = saveCrcAndSizes;
    }

    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            throw new NotSupportedException("This stream does not support seeking.");
        }
    }

    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return _position;
        }
        set
        {
            ThrowIfDisposed();
            throw new NotSupportedException("This stream does not support seeking.");
        }
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => _canWrite;

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().ToString());
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        throw new NotSupportedException("This stream does not support reading.");
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        throw new NotSupportedException("This stream does not support seeking.");
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        throw new NotSupportedException("SetLength requires a stream that supports seeking and writing.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);

        ThrowIfDisposed();
        Debug.Assert(CanWrite);

        if (count == 0)
            return;

        if (!_everWritten)
        {
            _initialPosition = _baseBaseStream.Position;
            _everWritten = true;
        }

        _checksum.Append(buffer[offset..(offset + count)]);
        _baseStream.Write(buffer, offset, count);
        _position += count;
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        Debug.Assert(CanWrite);

        if (buffer.Length == 0)
            return;

        if (!_everWritten)
        {
            _initialPosition = _baseBaseStream.Position;
            _everWritten = true;
        }

        _checksum.Append(buffer);
        _baseStream.Write(buffer);
        _position += buffer.Length;
    }

    public override void WriteByte(byte value) => Write(new ReadOnlySpan<byte>(in value));

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);
        return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        Debug.Assert(CanWrite);

        return !buffer.IsEmpty
            ? Core(buffer, cancellationToken)
            : default;

        async ValueTask Core(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_everWritten)
            {
                _initialPosition = _baseBaseStream.Position;
                _everWritten = true;
            }

            _checksum.Append(buffer.Span);

            await _baseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            _position += buffer.Length;
        }
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        Debug.Assert(CanWrite);
        _baseStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _baseStream.FlushAsync(cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            if (!_everWritten)
                _initialPosition = _baseBaseStream.Position;
            if (!_leaveOpenOnClose)
                _baseStream.Dispose();
            _saveCrcAndSizes?.Invoke(_initialPosition, Position, _checksum.GetCurrentHashAsUInt32(), _baseBaseStream, _pakArchiveEntry, _onClose);
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
