using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts;
using Rebus.Transport;

namespace Rebus.CastleWindsor.Tests;

[TestFixture]
public class CheckResolutionSpeed : FixtureBase
{
    /*
Initial:
100000 resolutions took 1,4 s - that's 71708,4 /s
100000 resolutions took 1,3 s - that's 75356,7 /s
100000 resolutions took 1,3 s - that's 75734,5 /s
100000 resolutions took 1,3 s - that's 74942,4 /s
100000 resolutions took 1,3 s - that's 75289,9 /s

Cache generic types:
100000 resolutions took 0,8 s - that's 122710,3 /s
100000 resolutions took 0,7 s - that's 133980,5 /s
100000 resolutions took 0,7 s - that's 135853,4 /s
100000 resolutions took 0,7 s - that's 135229,6 /s
100000 resolutions took 0,7 s - that's 136566,6 /s
Update to Rebus 6:
100000 resolutions took 0,7 s - that's 141762,3 /s
100000 resolutions took 0,7 s - that's 140963,6 /s
100000 resolutions took 0,7 s - that's 141642,6 /s
100000 resolutions took 0,7 s - that's 141358,6 /s
100000 resolutions took 0,7 s - that's 140901,2 /s

    */
    [TestCase(100000)]
    [Repeat(5)]
    public async Task JustResolveManyTimes(int count)
    {
        var container = Using(new WindsorContainer());

        container.RegisterHandler<StringHandler>();

        var handlerActivator = new CastleWindsorContainerAdapter(container);

        var stopwatch = Stopwatch.StartNew();

        for (var counter = 0; counter < count; counter++)
        {
            using (var scope = new RebusTransactionScope())
            {
                await handlerActivator.GetHandlers("this is my message", scope.TransactionContext);
            }
        }

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"{count} resolutions took {elapsedSeconds:0.0} s - that's {count / elapsedSeconds:0.0} /s");
    }

    class StringHandler : IHandleMessages<string>
    {
        public Task Handle(string message)
        {
            throw new System.NotImplementedException();
        }
    }
}