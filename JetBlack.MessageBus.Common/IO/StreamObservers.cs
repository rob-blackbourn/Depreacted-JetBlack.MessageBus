using System;
using System.IO;
using System.Reactive;
using System.Threading;

namespace JetBlack.MessageBus.Common.IO
{
    public static class StreamObservers
    {
        public static IObserver<ByteBuffer> ToStreamObserver(this Stream stream, CancellationToken token)
        {
            return Observer.Create<ByteBuffer>(async buffer =>
            {
                await stream.WriteAsync(buffer.Bytes, 0, buffer.Length, token);
                await stream.FlushAsync(token);
            });
        }
    }
}
