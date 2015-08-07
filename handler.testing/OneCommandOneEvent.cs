using System;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;

namespace HandlerTesting.OneCommandOneEvent
{
    public class MyCommand: ICommand
    {
    }

	public interface IMyEvent : IEvent
	{
		int X { get; set; }
	}

	public class OneCommandOneEventHandler : IHandleMessages<MyCommand>
	{
		private readonly IBus _bus;
		public OneCommandOneEventHandler(IBus bus)
		{
			_bus = bus;
		}

		public void Handle(MyCommand message)
		{
			_bus.Publish<IMyEvent>(e =>
			{
				e.X = 123;
			});
		}
	}

	[TestFixture]
	public class MyTests
	{
		[Test]
		public void Basic_Handler_Expectation()
		{
			Test.Initialize(cb => cb.TypesToScan(new Type[] {typeof (IMyEvent)}));

			var cmd = new MyCommand();
			Test.Handler(b => new OneCommandOneEventHandler(b))
				.ExpectPublish<IMyEvent>(e => true)
				.OnMessage(cmd);
		}
	}





}
