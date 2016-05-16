using System.Collections.Generic;
using System.Linq;
using System.Net;
using JetBlack.MessageBus.FeedBus.Distributor.Config;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal class PermissionManager
    {
        private readonly DistributorConfig _configuration;
        private readonly IPAddress _ipAddress;
        private readonly string _user;
        private readonly IDictionary<string, IDictionary<ClientRole, bool>> feedDecision = new Dictionary<string, IDictionary<ClientRole, bool>>();

        public PermissionManager(DistributorConfig configuration, IPAddress ipAddress, string user)
        {
            _configuration = configuration;
            _ipAddress = ipAddress;
            _user = user;
        }

        public bool HasRole(string feed, ClientRole role)
        {
            IDictionary<ClientRole, bool> roleDecision;
            if (!feedDecision.TryGetValue(feed, out roleDecision))
                feedDecision.Add(feed, roleDecision = new Dictionary<ClientRole, bool>());
            bool decision;
            if (roleDecision.TryGetValue(role, out decision))
                return decision;

            // Check the distributor.
            if ((_configuration.Allow & role) == role)
                decision = true;
            // Deny overrides allow.
            if ((_configuration.Deny & role) == role)
                decision = false;

            // Is there a feed definition?
            FeedConfig feedConfiguration;
            if (_configuration.FeedConfigurations.TryGetValue(feed, out feedConfiguration))
            {
                // Feed configuration overrides distributor.
                if ((feedConfiguration.Allow & role) == role)
                    decision = true;
                // Deny overrides allow.
                if ((feedConfiguration.Deny & role) == role)
                    decision = false;

                // Is there a client definition?
                var client = feedConfiguration.Clients.FirstOrDefault(c => IPAddress.Equals(c.IPAddress, _ipAddress) && string.Equals(c.User, _user));
                if (client != null)
                {
                    // Client configuration overrides feed.
                    if ((client.Allow & role) == role)
                        decision = true;
                    // Deny overrides allow.
                    if ((client.Deny & role) == role)
                        decision = false;
                }
            }

            // Cache the decision;
            roleDecision.Add(role, decision);

            return decision;
        }
    }
}
