# Some message bus examples

This is work in progress!

I am re-writing the real time message bus, that you can find a small version of on github (JetBlack.TopicBus),
and a more complete version on codeplex.

I have completed the following goals (note the tenses!):
* I wanted to use reactive extentions to simplify the threading and locking. A version of this exists in JetBlack.TopicBus, and I have done some more work to simplify the approach here.
* I wanted to convert the network layer to use reactive extensions as it seemed like a more natural way to express the solution. I wanted to make full use of async/await calls to keep the code simple and efficient. You can find some experiments on this in JetBlack.Networks.
* I wanted to make the distributor data agnostic. My original implementation serialised a dictionary of known types.

The following is on my todo/wish list.
* Test the caching publisher.
* Understand the close and fault behaviour of clients and the distributor.
* Improve the client adapters. A naive user should not need to know about network addresses or the dynamics of sockets and address resolution.
* Add authentication. I would like to support an SASL style authenticator that is OS agnostic.
* Add authorisation. Many data feeds have permissioning, such that data should only be distributed to those that are paying for it. This is often true at the field level. I need to do this without the distributor knowing the structure of the data.
