using JetBlack.MessageBus.Common;
using JetBlack.MessageBus.Common.IO;
using JetBlack.MessageBus.Common.Network;
using System;
using System.IO;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.ServiceModel.Channels;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public static class MessageExtensions
    {
        public static IObservable<Message> ToMessageObservable(this TcpClient tcpClient, BufferManager bufferManager)
        {
            return Observable.Create<Message>(
                observer => tcpClient.ToFrameClientObservable(bufferManager).Subscribe(
                    disposableBuffer =>
                    {
                        using (var messageStream = new MemoryStream(disposableBuffer.Value.Array, disposableBuffer.Value.Offset, disposableBuffer.Value.Count, false, false))
                        {
                            var message = Message.Read(messageStream);
                            observer.OnNext(message);
                        }
                        disposableBuffer.Dispose();
                    },
                    observer.OnError,
                    observer.OnCompleted));
        }

        public static IObserver<Message> ToMessageObserver(this TcpClient tcpClient, BufferManager bufferManager, CancellationToken token)
        {
            var observer = tcpClient.ToFrameClientObserver(token);

            return Observer.Create<Message>(message =>
            {
                var messageStream = new BufferedMemoryStream(bufferManager, 256);
                message.Write(messageStream);
                var buffer = new ArraySegment<byte>(messageStream.GetBuffer(), 0, (int)messageStream.Length);
                observer.OnNext(DisposableValue.Create(buffer, messageStream));
            });
        }
    }
}