using System;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Transport.InMem;

namespace Rebus.CastleWindsor.Tests
{
    public class Snippets
    {
        public class RebusInstaller : IWindsorInstaller
        {
            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                Configure.With(new CastleWindsorContainerAdapter(container))
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimmelim"))
                    .Start();
            }
        }

        public class RebusHandlersInstaller : IWindsorInstaller
        {
            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                container.AutoRegisterHandlersFromAssemblyOf<SomeHandler>();
            }
        }

        class SomeHandler : IHandleMessages<string>
        {
            public Task Handle(string message)
            {
                throw new NotImplementedException();
            }
        }
    }
}