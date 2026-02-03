namespace PkgMaker.Utils;

internal class ConcatStreams(IEnumerable<Stream> streams)
    : Stream
{
    public override long Length => streams.Sum(x => x.Length);

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    private readonly Queue<Stream> streams = new(streams);

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = 0;

        while (count > 0 && streams.Count > 0)
        {
            var bytesRead = streams.First().Read(buffer, offset, count);
            if (bytesRead == 0)
            {
                streams.Dequeue().Dispose();
                continue;
            }

            read += bytesRead;
            offset += bytesRead;
            count -= bytesRead;
        }

        return read;
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}