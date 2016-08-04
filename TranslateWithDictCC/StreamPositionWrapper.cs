using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TranslateWithDictCC
{
    class StreamPositionWrapper : Stream
    {
        Stream stream;
        long position;

        public override bool CanRead
        {
            get
            {
                return stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return stream.CanWrite;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return stream.CanTimeout;
            }
        }

        public override long Length
        {
            get
            {
                return stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int ReadTimeout
        {
            get
            {
                return stream.ReadTimeout;
            }
            set
            {
                stream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return stream.WriteTimeout;
            }
            set
            {
                stream.WriteTimeout = value;
            }
        }

        public StreamPositionWrapper(Stream stream, long position = 0)
        {
            this.stream = stream;
            this.position = position;
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = stream.Read(buffer, offset, count);

            position += bytesRead;

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
            position += count;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int bytesRead = await stream.ReadAsync(buffer, offset, count, cancellationToken);
            position += bytesRead;
            return bytesRead;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return stream.FlushAsync(cancellationToken);
        }

        public override int ReadByte()
        {
            int readByte = stream.ReadByte();

            if (readByte != -1)
                position++;

            return readByte;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await stream.WriteAsync(buffer, offset, count, cancellationToken);
            position += count;
        }

        public override void WriteByte(byte value)
        {
            stream.WriteByte(value);
            position++;
        }

        private bool disposedValue = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                        stream = null;
                    }
                }

                disposedValue = true;
            }
        }
    }
}
