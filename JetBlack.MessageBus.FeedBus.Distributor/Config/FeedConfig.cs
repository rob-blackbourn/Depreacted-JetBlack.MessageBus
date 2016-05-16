using System.Collections.Generic;

namespace JetBlack.MessageBus.FeedBus.Distributor.Config
{
    public class FeedConfig
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="feed">The feed.</param>
        /// <param name="allow">Allowed roles.</param>
        /// <param name="deny">Denied roles.</param>
        /// <param name="requiresEntitlement">Indicates whether the feed requires entitlement.</param>
        /// <param name="clients">The client configurations.</param>
        public FeedConfig(string feed, ClientRole allow, ClientRole deny, bool requiresEntitlement, IList<ClientConfig> clients)
        {
            Feed = feed;
            Allow = allow;
            Deny = deny;
            RequiresEntitlement = requiresEntitlement;
            Clients = clients;
        }

        /// <summary>
        /// The feed.
        /// </summary>
        public readonly string Feed;

        /// <summary>
        /// The roles feed clients may assume. If present this overrides the distributor roles.
        /// </summary>
        public readonly ClientRole Allow;

        /// <summary>
        /// The roles feed clients may not assume. If present this overrides the distributor roles.
        /// </summary>
        public readonly ClientRole Deny;

        /// <summary>
        /// Indicates whether the feed requires entitlement.
        /// </summary>
        public readonly bool RequiresEntitlement;

        /// <summary>
        /// The client configurations.
        /// </summary>
        public readonly IList<ClientConfig> Clients;
    }
}
