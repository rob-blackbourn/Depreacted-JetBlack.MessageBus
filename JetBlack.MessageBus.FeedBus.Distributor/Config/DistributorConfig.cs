using System.Collections.Generic;

namespace JetBlack.MessageBus.FeedBus.Distributor.Config
{
    public class DistributorConfig
    {
        /// <summary>
        /// Construct an adapter configuration object.
        /// </summary>
        /// <param name="name">The configuration name.</param>
        /// <param name="port">The configuration port.</param>
        /// <param name="allow">Allowed roles.</param>
        /// <param name="deny">Denied roles.</param>
        /// <param name="feedConfigurations">The feed configurations.</param>
        public DistributorConfig(string name, int port, ClientRole allow, ClientRole deny, IList<FeedConfig> feedConfigurations)
        {
            Name = name;
            Port = port;
            Allow = allow;
            Deny = deny;
            FeedConfigurations = new Dictionary<string, FeedConfig>();
            foreach (var feedConfiguration in feedConfigurations)
                FeedConfigurations.Add(feedConfiguration.Feed, feedConfiguration);
        }

        /// <summary>
        /// The configuration name.
        /// </summary>
        public string Name;

        /// <summary>
        /// The port to connect to.
        /// </summary>
        public int Port;

        /// <summary>
        /// The roles distributor clients may assume.
        /// </summary>
        public readonly ClientRole Allow;

        /// <summary>
        /// The roles distributor client may not assume.
        /// </summary>
        public readonly ClientRole Deny;

        /// <summary>
        /// The feed configurations.
        /// </summary>
        public readonly IDictionary<string, FeedConfig> FeedConfigurations;
    }
}
