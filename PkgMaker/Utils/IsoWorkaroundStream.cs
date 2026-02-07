using PkgMaker.Services;

namespace PkgMaker.Utils;

/// <summary>
/// Recreates inner stream in case of rewind, maintains its own position, enforces exact reads (which is bad at end of stream)
/// </summary>
/// <remarks>DO NOT USE for read-to-end operations like full file downloading</remarks>
internal sealed class IsoWorkaroundStream : Stream
{
    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => innerStream.Length;

    public override long Position
    {
        get => position;

        set
        {
            if (position == value)
            {
                return;
            }

            if (dumbFactory)
            {
                // can only recreate stream from start
                if (value < position)
                {
                    // ignores argument, it's dumb!
                    RecreateStreamAtOffset(0);
                    // fast forward from start
                    Main.Log($"Streaming ISO: reset from {position} to {value}, wasted {value} bytes");
                    FastForward(innerStream, 0, value);
                }
                else
                {
                    // fast forward from current position
                    Main.Log($"Streaming ISO: fast forward from {position} to {value}, wasted {value - position} bytes");
                    FastForward(innerStream, position, value);
                }
            }
            else
            {
                // can recreate stream directly at given offset
                Main.Log($"Streaming ISO: seek from {position} to {value}");
                RecreateStreamAtOffset(value);
            }

            position = value;
        }
    }

    private readonly Func<long, Stream> streamFactoryAtOffset;
    private readonly bool dumbFactory;
    private Stream innerStream;
    private long position;

    /// <summary>
    /// FTP stream can be recreated at given offset
    /// </summary>
    public IsoWorkaroundStream(Func<long, Stream> streamFactoryAtOffset)
    {
        this.streamFactoryAtOffset = streamFactoryAtOffset;
        innerStream = streamFactoryAtOffset(0);
    }

    /// <summary>
    /// HTTP stream can only be created from scratch, and does not even support position or length
    /// </summary>
    public IsoWorkaroundStream(Func<Stream> dumbStreamFactory) : this(_ => dumbStreamFactory()) => dumbFactory = true;

    public override int Read(byte[] buffer, int offset, int count)
    {
        // it's not right, but it's a workaround for CDReader that crashes sometimes when read returns less bytes
        innerStream.ReadExactly(buffer.AsSpan(offset, count));
        position += count;
        return count;
    }

    public override async ValueTask DisposeAsync()
    {
        await innerStream.DisposeAsync();
        await base.DisposeAsync();
    }

    public override void Flush() => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private void RecreateStreamAtOffset(long value)
    {
        innerStream.Dispose();
        innerStream = streamFactoryAtOffset(value);
    }

    private static Stream FastForward(Stream s, long pos, long dst)
    {
        var x = dst - pos;
        var chunks = x / DevNull.Length;
        var remainder = x % DevNull.Length;
        for (var i = 0; i < chunks; i++)
        {
            s.ReadExactly(DevNull.AsSpan(new Range(0, DevNull.Length)));
        }

        s.ReadExactly(DevNull.AsSpan(new Range(0, (int) remainder)));
        return s;
    }

    private static readonly byte[] DevNull = new byte[Constants.IsoStreamingBufferSize];
}
