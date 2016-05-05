using System;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.ServiceModel.Channels;
using System.Threading;

namespace JetBlack.MessageBus.Common.Network
{
    public static class FrameClientExtensions
    {
        //public static ISubject<DisposableValue<ArraySegment<byte>>, DisposableValue<ArraySegment<byte>>> ToFrameClientAsyncSubject(this TcpClient client, BufferManager bufferManager, CancellationToken token)
        //{
        //    return Subject.Create(client.ToFrameClientAsyncObserver(token), client.ToFrameClientAsyncObservable(bufferManager));
        //}

        public static IObservable<DisposableValue<ArraySegment<byte>>> ToFrameClientAsyncObservable(this TcpClient client, BufferManager bufferManager)
        {
            return client.GetStream().ToFrameStreamAsyncObservable(bufferManager);
        }

        //public static IObserver<DisposableValue<ArraySegment<byte>>> ToFrameClientAsyncObserver(this TcpClient client, CancellationToken token)
        //{
        //    return client.GetStream().ToFrameStreamAsyncObserver(token);
        //}

        public static IObserver<DisposableValue<ArraySegment<byte>>> ToFrameClientObserver(this TcpClient client)
        {
            return client.GetStream().ToFrameStreamObserver();
        }
    }
}
