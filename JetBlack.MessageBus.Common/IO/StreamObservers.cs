using System;
using System.IO;
using System.Reactive;
using System.Threading;

namespace JetBlack.MessageBus.Common.IO
{
    public static class StreamObservers
    {
        public static IObserver<ArraySegment<byte>> ToStreamObserver(this Stream stream, CancellationToken token)
        {
            return Observer.Create<ArraySegment<byte>>(async buffer =>
            {
                await stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, token);
                await stream.FlushAsync(token);
            });
        }
    }
}
