using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBlack.MessageBus.Common.Collections;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class NotificationRepository
    {
        private readonly Dictionary<string, ISet<Interactor>> _topicPatternToNotifiables = new Dictionary<string, ISet<Interactor>>();
        private readonly Dictionary<string, Regex> _topicPatternToRegex = new Dictionary<string, Regex>();

        public void RemoveInteractor(Interactor interactor)
        {
            // Remove the interactor where it appears in the notifiables, remembering any topics which are left without any interactors.
            var topicsWithoutInteractors = new HashSet<string>();
            foreach (var topicPatternToNotifiable in _topicPatternToNotifiables.Where(x => x.Value.Contains(interactor)))
            {
                topicPatternToNotifiable.Value.Remove(interactor);
                if (topicPatternToNotifiable.Value.Count == 0)
                    topicsWithoutInteractors.Add(topicPatternToNotifiable.Key);
            }

            // Remove any topics left without interactors.
            foreach (var topic in topicsWithoutInteractors)
            {
                _topicPatternToNotifiables.Remove(topic);
                _topicPatternToRegex.Remove(topic);
            }
        }

        public void AddRequest(Interactor notifiable, string topicPattern, IObserver<SourceMessage<Regex>> notificationObserver)
        {
            // Find or create the set of notifiables for this topic, and cache the regex for the topic pattern.
            ISet<Interactor> notifiables;
            Regex topicRegex;
            if (!_topicPatternToNotifiables.TryGetValue(topicPattern, out notifiables))
            {
                _topicPatternToNotifiables.Add(topicPattern, notifiables = new HashSet<Interactor>());
                _topicPatternToRegex.Add(topicPattern, topicRegex = new Regex(topicPattern));
            }
            else if (!notifiables.Contains(notifiable))
                topicRegex = _topicPatternToRegex[topicPattern];
            else
                return;

            // Add to the notifiables for this topic pattern and inform the subscription manager of the new notification request.
            notifiables.Add(notifiable);
            notificationObserver.OnNext(SourceMessage.Create(notifiable, topicRegex));
        }

        public void RemoveRequest(Interactor notifiable, string topicPattern)
        {
            // Does this topic pattern have any notifiable interactors?
            ISet<Interactor> notifiables;
            if (!_topicPatternToNotifiables.TryGetValue(topicPattern, out notifiables))
                return;

            // Is this interactor in the set of notifiables for this topic pattern?
            if (!notifiables.Contains(notifiable))
                return;

            // Remove the interactor from the set of notifiables.
            notifiables.Remove(notifiable);

            // Are there any interactors left listening to this topic pattern?
            if (notifiables.Count != 0)
                return;

            // Remove the empty pattern from the caches.
            _topicPatternToNotifiables.Remove(topicPattern);
            _topicPatternToRegex.Remove(topicPattern);
        }

        public ISet<Interactor> FindNotifiables(string topic)
        {
            return _topicPatternToRegex
                .Where(x => x.Value.IsMatch(topic))
                .SelectMany(x => _topicPatternToNotifiables[x.Key])
                .ToSet();
        }
    }
}
