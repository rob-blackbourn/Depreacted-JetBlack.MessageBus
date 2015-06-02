﻿using System;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.ServiceModel.Channels;
using System.Threading;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.Common.Network
{
public static class FrameClientExtensions
    {
        public static ISubject<DisposableByteBuffer, DisposableByteBuffer> ToFrameClientSubject(this TcpClient client, BufferManager bufferManager, CancellationToken token)
        {
            return Subject.Create(client.ToFrameClientObserver(token), client.ToFrameClientObservable(bufferManager));
        }

        public static IObservable<DisposableByteBuffer> ToFrameClientObservable(this TcpClient client, BufferManager bufferManager)
        {
            return client.GetStream().ToFrameStreamObservable(bufferManager);
        }

        public static IObserver<DisposableByteBuffer> ToFrameClientObserver(this TcpClient client, CancellationToken token)
        {
            return client.GetStream().ToFrameStreamObserver(token);
        }
    }
}
