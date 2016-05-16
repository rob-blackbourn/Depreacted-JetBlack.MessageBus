using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using log4net;

namespace JetBlack.MessageBus.FeedBus.Distributor.Config
{
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string _defaultName = null;
        private Dictionary<string, DistributorConfig> _distributors = null;

        /// <summary>
        /// The default configuration name.
        /// </summary>
        public string DefaultName
        {
            get { return _defaultName; }
            set { _defaultName = value; }
        }

        /// <summary>
        /// Returns the requested named configuration.
        /// </summary>
        /// <param name="name">A configuration name.</param>
        /// <returns>The requested configuration.</returns>
        public DistributorConfig this[string name]
        {
            get
            {
                if (_distributors.ContainsKey(name))
                    return _distributors[name];
                else
                    return null;
            }
        }

        /// <summary>
        /// Returns the default configuration.
        /// </summary>
        public DistributorConfig DefaultConfig
        {
            get { return this[_defaultName]; }
        }

        /// <summary>
        /// The hook into <see cref="System.Configuration.ConfigurationManager"/>
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="configContext">The config context.</param>
        /// <param name="section">The xml document section.</param>
        /// <returns>The parsed configuration.</returns>
        public object Create(object parent, object configContext, XmlNode section)
        {
            if (section == null)
                return null;

#if DEBUG
            const string defaultName = "debug";
#else
            const string defaultName = "release";
#endif
            if (section.Attributes[defaultName] == null)
            {
                Log.ErrorFormat("Unable to find configuration default \"{0}\".", defaultName);
                return null;
            }

            _defaultName = section.Attributes[defaultName].Value;
            if (string.IsNullOrEmpty(_defaultName))
            {
                Log.ErrorFormat("Unable to determine default configuration for tag \"{0}\"", defaultName);
                return null;
            }

            _distributors = CreateDistributors(section.SelectNodes("add"));
            if (_distributors == null || _distributors.Count == 0)
            {
                Log.ErrorFormat("Unable to read configuration for tag \"add\"");
                return null;
            }

            return this;
        }

        private Dictionary<string, DistributorConfig> CreateDistributors(XmlNodeList distributorNodes)
        {
            var distributors = new Dictionary<string, DistributorConfig>();

            if (distributorNodes == null)
            {
                Log.ErrorFormat("At least one distributor must be defined.");
                return null;
            }

            foreach (var distributorNode in distributorNodes)
            {
                var distributor = CreateDistributor((XmlElement)distributorNode);
                if (distributor == null)
                {
                    Log.ErrorFormat("Unable to read configuration for distributor");
                    return null;
                }

                distributors.Add(distributor.Name, distributor);
            }

            return distributors;
        }

        private DistributorConfig CreateDistributor(XmlElement distributorElement)
        {
            string name = distributorElement.GetAttribute("name");
            if (string.IsNullOrEmpty(name))
            {
                Log.ErrorFormat("Unable to read adapter configuration for tag \"name\"");
                return null;
            }

            int port = -1;
            if (!int.TryParse(distributorElement.GetAttribute("port"), out port))
            {
                Log.ErrorFormat("Unable to read or understand adapter configuration for tag \"port\"");
                return null;
            }

            ClientRole allow;
            TryParse(distributorElement.GetAttribute("allow"), out allow);

            ClientRole deny;
            TryParse(distributorElement.GetAttribute("deny"), out deny);

            var feedConfigurations = CreateFeedConfigurations(distributorElement.SelectSingleNode("feedConfigurations"));

            return new DistributorConfig(name, port, allow, deny, feedConfigurations);
        }

        private IList<FeedConfig> CreateFeedConfigurations(XmlNode feedConfigurationsNode)
        {
            var feedConfigurations = new List<FeedConfig>();

            if (feedConfigurationsNode != null)
            {
                foreach (var feedConfigurationNode in feedConfigurationsNode.SelectNodes("feedConfiguration"))
                {
                    var feedConfiguration = CreateFeedConfiguration((XmlElement)feedConfigurationNode);
                    if (feedConfiguration == null)
                    {
                        Log.ErrorFormat("Failed to read the feedConfiguration element");
                        return null;
                    }
                    feedConfigurations.Add(feedConfiguration);
                }
            }

            return feedConfigurations;
        }

        private FeedConfig CreateFeedConfiguration(XmlElement feedConfigurationElement)
        {
            var feed = feedConfigurationElement.GetAttribute("feed");
            if (string.IsNullOrWhiteSpace(feed))
            {
                Log.ErrorFormat("Unable to read the feed attribute of the feedConfiguration element");
                return null;
            }

            ClientRole allow;
            if (string.IsNullOrWhiteSpace(feedConfigurationElement.GetAttribute("allow")))
                allow = ClientRole.None;
            else
            {
                if (!TryParse(feedConfigurationElement.GetAttribute("allow"), out allow))
                {
                    Log.ErrorFormat("Unable to parse \"allow\" attribute.");
                    return null;
                }
            }

            ClientRole deny;
            if (string.IsNullOrWhiteSpace(feedConfigurationElement.GetAttribute("deny")))
                deny = ClientRole.None;
            else
            {
                if (!TryParse(feedConfigurationElement.GetAttribute("deny"), out deny))
                {
                    Log.ErrorFormat("Unable to parse \"deny\" attribute.");
                    return null;
                }
            }

            bool requiresEntitlement;
            if (string.IsNullOrWhiteSpace(feedConfigurationElement.GetAttribute("requiresEntitlement")))
                requiresEntitlement = false;
            else
            {
                if (!bool.TryParse(feedConfigurationElement.GetAttribute("requiresEntitlement"), out requiresEntitlement))
                {
                    Log.ErrorFormat("Unable to parse \"requiresEntitlement\" attribute.");
                    return null;
                }
            }

            var clients = CreateClients(feedConfigurationElement.SelectSingleNode("clients"));

            return new FeedConfig(feed, allow, deny, requiresEntitlement, clients);
        }

        private IList<ClientConfig> CreateClients(XmlNode clientsNode)
        {
            var clients = new List<ClientConfig>();
            if (clientsNode != null)
            {
                foreach (XmlElement clientElement in clientsNode.SelectNodes("client"))
                {
                    var client = CreateClient(clientElement);
                    if (client == null)
                    {
                        Log.ErrorFormat("Unable to read the client section");
                        return null;
                    }
                    clients.Add(client);
                }
            }
            return clients;
        }

        private ClientConfig CreateClient(XmlElement clientElement)
        {
            var ipAddress = Dns.GetHostAddresses(clientElement.GetAttribute("host")).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            if (ipAddress == null)
            {
                Log.ErrorFormat("Failed to read host for client.");
                return null;
            }

            var user = clientElement.GetAttribute("user");
            if (string.IsNullOrWhiteSpace(user))
            {
                Log.ErrorFormat("Failed to read user for client.");
                return null;
            }

            ClientRole allow;
            if (string.IsNullOrWhiteSpace(clientElement.GetAttribute("allow")))
                allow = ClientRole.None;
            else
            {
                if (!TryParse(clientElement.GetAttribute("allow"), out allow))
                {
                    Log.ErrorFormat("Unable to parse \"allow\" attribute.");
                    return null;
                }
            }

            ClientRole deny;
            if (string.IsNullOrWhiteSpace(clientElement.GetAttribute("deny")))
                deny = ClientRole.None;
            else
            {
                if (!TryParse(clientElement.GetAttribute("deny"), out deny))
                {
                    Log.ErrorFormat("Unable to parse \"deny\" attribute.");
                    return null;
                }
            }

            return new ClientConfig(ipAddress, user, allow, deny);
        }

        private bool TryParse(string s, out ClientRole clientRole)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                clientRole = ClientRole.None;
                return true;
            }

            clientRole = ClientRole.None;
            foreach (var part in s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                ClientRole role;
                if (!Enum.TryParse(part, out role))
                {
                    clientRole = ClientRole.None;
                    return false;
                }

                clientRole |= role;
            }

            return true;
        }
    }
}
