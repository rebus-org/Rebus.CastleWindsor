using System;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.CastleWindsor.Tests
{
    public class CastleWindsorActivationContext : IActivationContext
    {
        public IHandlerActivator CreateActivator(Action<IHandlerRegistry> handlerConfig, out IActivatedContainer container)
        {
            var windsorContainer = new WindsorContainer();

            handlerConfig.Invoke(new HandlerRegistry(windsorContainer));

            container = new ActivatedContainer(windsorContainer);

            return new CastleWindsorContainerAdapter(windsorContainer);
        }

        public IBus CreateBus(Action<IHandlerRegistry> handlerConfig, Func<RebusConfigurer, RebusConfigurer> configureBus, out IActivatedContainer container)
        {
            var windsorContainer = new WindsorContainer();

            handlerConfig.Invoke(new HandlerRegistry(windsorContainer));
            container = new ActivatedContainer(windsorContainer);

            return configureBus(Configure.With(new CastleWindsorContainerAdapter(windsorContainer))).Start();
        }

        private class HandlerRegistry : IHandlerRegistry
        {
            private readonly WindsorContainer _windsorContainer;

            public HandlerRegistry(WindsorContainer windsorContainer)
            {
                _windsorContainer = windsorContainer;
            }

            public IHandlerRegistry Register<THandler>() where THandler : class, IHandleMessages
            {
                _windsorContainer.Register(
                    Component
                        .For(GetHandlerInterfaces(typeof(THandler)))
                        .ImplementedBy<THandler>()
                        .LifestyleTransient()
                    );

                return this;
            }

            Type[] GetHandlerInterfaces(Type type)
            {
                return type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                    .ToArray();
            }
        }

        private class ActivatedContainer : IActivatedContainer
        {
            private readonly WindsorContainer _windsorContainer;

            public ActivatedContainer(WindsorContainer windsorContainer)
            {
                _windsorContainer = windsorContainer;
            }

            public IBus ResolveBus()
            {
                return _windsorContainer.Resolve<IBus>();
            }

            public void Dispose()
            {
                _windsorContainer.Dispose();
            }
        }
    }
}