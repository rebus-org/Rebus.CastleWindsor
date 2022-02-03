using Castle.Windsor;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Transport.InMem;

namespace Rebus.CastleWindsor.Tests;

[TestFixture]
public class CheckTheApi
{
    [Test]
    public void DelayStartingTheBus()
    {
        using var container = new WindsorContainer();

        var starter = Configure.With(new CastleWindsorContainerAdapter(container))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "who-cares"))
            .Create();

        var bus = container.Resolve<IBus>();

        Assert.That(bus.Advanced.Workers.Count, Is.EqualTo(0));

        starter.Start();

        Assert.That(bus.Advanced.Workers.Count, Is.EqualTo(1));
    }
}