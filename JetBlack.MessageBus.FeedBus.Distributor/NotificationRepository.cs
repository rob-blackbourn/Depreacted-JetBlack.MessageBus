using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal class NotificationRepository
    {
        private readonly IDictionary<string, ISet<IInteractor>> _feedToNotifiables = new Dictionary<string, ISet<IInteractor>>();

        public void RemoveInteractor(IInteractor interactor)
        {
            // Remove the interactor where it appears in the notifiables, remembering any feeds which are left without any interactors.
            var feedsWithoutInteractors = new HashSet<string>();
            foreach (var feedNotifiable in _feedToNotifiables.Where(x => x.Value.Contains(interactor)))
            {
                feedNotifiable.Value.Remove(interactor);
                if (feedNotifiable.Value.Count == 0)
                    feedsWithoutInteractors.Add(feedNotifiable.Key);
            }

            // Remove any feeds left without interactors.
            foreach (var feed in feedsWithoutInteractors)
                _feedToNotifiables.Remove(feed);
        }

        public void AddRequest(IInteractor notifiable, string feed, IObserver<SourceMessage<string>> notificationObserver)
        {
            // Find or create the set of notifiables for this feed.
            ISet<IInteractor> notifiables;
            if (!_feedToNotifiables.TryGetValue(feed, out notifiables))
                _feedToNotifiables.Add(feed, notifiables = new HashSet<IInteractor>());
            else if (notifiables.Contains(notifiable))
                return;

            // Add to the notifiables for this topic pattern and inform the subscription manager of the new notification request.
            notifiables.Add(notifiable);
            notificationObserver.OnNext(SourceMessage.Create(notifiable, feed));
        }

        public void RemoveRequest(IInteractor notifiable, string feed)
        {
            // Does this feed have any notifiable interactors?
            ISet<IInteractor> notifiables;
            if (!_feedToNotifiables.TryGetValue(feed, out notifiables))
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
            _feedToNotifiables.Remove(feed);
        }

        public ISet<IInteractor> FindNotifiables(string feed)
        {
            ISet<IInteractor> notifiables;
            if (!_feedToNotifiables.TryGetValue(feed, out notifiables))
                return new HashSet<IInteractor>();
            else
                return new HashSet<IInteractor>(notifiables);
        }
    }
}
