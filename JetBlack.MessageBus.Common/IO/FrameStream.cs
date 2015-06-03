using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace JetBlack.MessageBus.Common.IO
{
    public static class FrameStream
    {
        public static IObserver<DisposableValue<ArraySegment<byte>>> ToFrameStreamObserver(this Stream stream, CancellationToken token)
        {
            return Observer.Create<DisposableValue<ArraySegment<byte>>>(async disposableBuffer =>
            {
                await WriteFrame(stream, disposableBuffer.Value, token);
            });
        }

        private static async Task WriteFrame(this Stream stream, ArraySegment<byte> byteBuffer, CancellationToken token)
        {
            var headerBuffer = BitConverter.GetBytes(byteBuffer.Count);
            await stream.WriteAsync(headerBuffer, 0, headerBuffer.Length, token);
            await stream.WriteAsync(byteBuffer.Array, byteBuffer.Offset, byteBuffer.Count, token);
            await stream.FlushAsync(token);
        }

        public static IObservable<DisposableValue<ArraySegment<byte>>> ToFrameStreamObservable(this Stream stream, BufferManager bufferManager)
        {
            return Observable.Create<DisposableValue<ArraySegment<byte>>>(async (observer, token) =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var disposableBuffer = await ReadFrame(stream, bufferManager, token);
                        if (disposableBuffer == DisposableValue<ArraySegment<byte>>.Empty)
                            break;

                        observer.OnNext(disposableBuffer);
                    }

                    observer.OnCompleted();
                }
                catch (Exception error)
                {
                    observer.OnError(error);
                }
            });
        }

        private static async Task<DisposableValue<ArraySegment<byte>>> ReadFrame(this Stream stream, BufferManager bufferManager, CancellationToken token)
        {
            var headerBuffer = new byte[sizeof(int)];
            if (await stream.ReadBytesCompletelyAsync(headerBuffer, headerBuffer.Length, token) != headerBuffer.Length)
                return DisposableValue<ArraySegment<byte>>.Empty;
            var length = BitConverter.ToInt32(headerBuffer, 0);

            var buffer = bufferManager.TakeBuffer(length);
            if (await stream.ReadBytesCompletelyAsync(buffer, length, token) != length)
            {
                bufferManager.ReturnBuffer(buffer);
                return DisposableValue<ArraySegment<byte>>.Empty;
            }
            
            return DisposableValue.Create(new ArraySegment<byte>(buffer, 0, length), Disposable.Create(() => bufferManager.ReturnBuffer(buffer)));
        }

        public static async Task<int> ReadBytesCompletelyAsync(this Stream stream, byte[] buf, int length, CancellationToken token)
        {
            var read = 0;
            while (read < length)
            {
                var remaining = length - read;
                var bytes = await stream.ReadAsync(buf, read, remaining, token);
                if (bytes == 0)
                    return read;

                read += bytes;
            }
            return read;
        }
    }
}
