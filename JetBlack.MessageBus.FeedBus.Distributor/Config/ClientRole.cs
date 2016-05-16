using System;

namespace JetBlack.MessageBus.FeedBus.Distributor.Config
{
    [Flags]
    public enum ClientRole
    {
        None = 0x00,
        Subscribe = 0x01,
        Publish = 0x02,
        Notify = 0x04
    }
}
