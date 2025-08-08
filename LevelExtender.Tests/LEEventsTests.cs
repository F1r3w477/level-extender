using System;
using NUnit.Framework;

namespace LevelExtender.Tests
{
    [TestFixture]
    public class LEEventsTests
    {
        [Test]
        public void RaiseEvent_InvokesSubscriber_WithCorrectSenderAndArgs()
        {
            var events = new LEEvents();
            object? receivedSender = null;
            EXPEventArgs? receivedArgs = null;

            events.OnXPChanged += (sender, args) =>
            {
                receivedSender = sender;
                receivedArgs = args;
            };

            var evt = new EXPEventArgs { Key = 42 };
            events.RaiseEvent(evt);

            Assert.That(receivedSender, Is.SameAs(events));
            Assert.That(receivedArgs, Is.Not.Null);
            Assert.That(receivedArgs!.Key, Is.EqualTo(42));
        }

        [Test]
        public void RaiseEvent_WithNoSubscribers_DoesNotThrow()
        {
            var events = new LEEvents();
            Assert.DoesNotThrow(() => events.RaiseEvent(new EXPEventArgs { Key = 1 }));
        }

        [Test]
        public void Unsubscribe_RemovesHandler_HandlerNoLongerInvoked()
        {
            var events = new LEEvents();
            int calls = 0;

            EventHandler<EXPEventArgs> handler = (_, __) => calls++;
            events.OnXPChanged += handler;

            events.RaiseEvent(new EXPEventArgs());
            events.OnXPChanged -= handler;
            events.RaiseEvent(new EXPEventArgs());

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void MultipleSubscribers_AllReceiveEvent()
        {
            var events = new LEEvents();
            int a = 0, b = 0, c = 0;

            events.OnXPChanged += (_, __) => a++;
            events.OnXPChanged += (_, __) => b++;
            events.OnXPChanged += (_, __) => c++;

            events.RaiseEvent(new EXPEventArgs { Key = 7 });

            Assert.That(a, Is.EqualTo(1));
            Assert.That(b, Is.EqualTo(1));
            Assert.That(c, Is.EqualTo(1));
        }

        [Test]
        public void HandlerThrows_ExceptionBubblesAndStopsInvocation()
        {
            var events = new LEEvents();
            int callsAfterThrow = 0;

            events.OnXPChanged += (_, __) => throw new InvalidOperationException("boom");
            events.OnXPChanged += (_, __) => callsAfterThrow++; // will not be reached

            var ex = Assert.Throws<InvalidOperationException>(() =>
                events.RaiseEvent(new EXPEventArgs { Key = 99 })
            );

            Assert.That(ex!.Message, Is.EqualTo("boom"));
            Assert.That(callsAfterThrow, Is.EqualTo(0));
        }

        [Test]
        public void EXPEventArgs_Key_IsSettable()
        {
            var args = new EXPEventArgs { Key = 10 };
            Assert.That(args.Key, Is.EqualTo(10));
            args.Key = 123;
            Assert.That(args.Key, Is.EqualTo(123));
        }
    }
}
