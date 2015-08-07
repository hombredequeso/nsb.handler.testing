using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;

namespace HandlerTesting.OneCommandMultipleEvents
{
	public class MyCommand : ICommand
	{
		public int Count { get; set; }
	}

	public interface IMyEvent : IEvent
	{
		int X { get; set; }
	}

	public class OneCommandMultipleEventsHandler : IHandleMessages<MyCommand>
	{
		private readonly IBus _bus;

		public OneCommandMultipleEventsHandler(IBus bus)
		{
			_bus = bus;
		}

		public void Handle(MyCommand cmd)
		{
			Enumerable
				.Range(0, cmd.Count)
				.ToList()
				.ForEach(i =>
					_bus.Publish<IMyEvent>(e => { e.X = i; })
				);
		}
	}

	[TestFixture]
	public class MyTests
	{
		[Test]
		public void Basic_Handler_Expectation_Performing_Count()
		{
			Test.Initialize(cb => cb.TypesToScan(new Type[] {typeof (IMyEvent)}));

			List<IMessage> allMessagesRaised = new List<IMessage>();
			var cmd = new MyCommand() {Count = 5};
			int publishCount = 0;
			Test.Handler(b => new OneCommandMultipleEventsHandler(b))
				.ExpectPublish<IMyEvent>(e =>
				{
					allMessagesRaised.Add(e);
					++publishCount;
					return publishCount == cmd.Count;
					// This would fail, because not enough events were raised.
					return publishCount == cmd.Count + 1;
				})
				.OnMessage(cmd);

			Assert.AreEqual(cmd.Count, publishCount);
			Assert.AreEqual(cmd.Count, allMessagesRaised.Count());
		}

		// This test will fail.
		[Test]
		public void Basic_Handler_Expectation_Too_Many_Events_Raised()
		{
			Test.Initialize(cb => cb.TypesToScan(new Type[] {typeof (IMyEvent)}));

			// Try putting a break point in each of the Expect<Not>Publishes below to see the behaviour.
			// Each of the Expects is called for all the messages.
			// The ExpectPublish finally returns true when it reaches the expected number of events. 
			// If not enough events were raised, it would fail as above.
			// The ExpectNotPublish returns true, until the number of messages is greater than expected, then it returns false and fails.

			// Actually raise 5 events, but test to expect 4 events.
			int eventsToRaise = 5;
			int expectedNumberOfEventsToRaise = 4;
			var cmd = new MyCommand() {Count = eventsToRaise};
			int publishCount = 0, notPublishCount = 0;
			Test.Handler(b => new OneCommandMultipleEventsHandler(b))
				.ExpectPublish<IMyEvent>(e =>
				{
					++publishCount;
					// cmd.Count (5) events will be raised. Let's pretend we only want 4
					return publishCount == expectedNumberOfEventsToRaise;
				})
				.ExpectNotPublish<IMyEvent>(e =>
				{
					// This ExpectNotPublish will fail when the 5th event is raised.
					++notPublishCount;
					return notPublishCount > expectedNumberOfEventsToRaise;
				})
				.OnMessage(cmd);
		}
	}
}