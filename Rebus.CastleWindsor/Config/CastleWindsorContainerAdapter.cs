using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
// ReSharper disable ArgumentsStyleLiteral

// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable 1998

namespace Rebus.Config
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that is backed by a Windsor Container
    /// </summary>
    public class CastleWindsorContainerAdapter : IContainerAdapter
    {
        readonly IWindsorContainer _windsorContainer;

        /// <summary>
        /// Constructs the Windsor handler activator
        /// </summary>
        public CastleWindsorContainerAdapter(IWindsorContainer windsorContainer)
        {
            _windsorContainer = windsorContainer ?? throw new ArgumentNullException(nameof(windsorContainer));
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var handlerInstances = GetAllHandlerInstances<TMessage>();

            void DisposeInstances(ITransactionContext _)
            {
                foreach (var instance in handlerInstances)
                {
                    _windsorContainer.Release(instance);
                }
            }

            transactionContext.OnDisposed(DisposeInstances);

            return handlerInstances;
        }

        /// <summary>
        /// Stores the bus instance
        /// </summary>
        public void SetBus(IBus bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus), "You need to provide a bus instance in order to call this method!");

            if (_windsorContainer.Kernel.HasComponent(typeof(IBus)))
            {
                throw new InvalidOperationException("An IBus service is already registered in this container. If you want to host multiple Rebus instances in a single process, please use separate container instances for them.");
            }

            _windsorContainer
                .Register(
                    Component.For<IBus>().Instance(bus).LifestyleSingleton(),

                    Component.For<ISyncBus>()
                        .UsingFactoryMethod(k => k.Resolve<IBus>().Advanced.SyncBus)
                        .LifestyleSingleton(),

                    Component.For<InstanceDisposer>(),

                    Component.For<IMessageContext>()
                        .UsingFactoryMethod(kernel =>
                        {
                            var currentMessageContext = MessageContext.Current;
                            if (currentMessageContext == null)
                            {
                                throw new InvalidOperationException("Attempted to inject the current message context from MessageContext.Current, but it was null! Did you attempt to resolve IMessageContext from outside of a Rebus message handler?");
                            }
                            return currentMessageContext;
                        }, managedExternally: true)
                        .LifestyleTransient()
                );

            _windsorContainer.Resolve<InstanceDisposer>();
        }

        /// <summary>
        /// containehack to makes sure we dispose the bus instance when the container is disposed
        /// </summary>
        class InstanceDisposer : IDisposable
        {
            readonly IBus _bus;

            public InstanceDisposer(IBus bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));

            public void Dispose() => _bus.Dispose();
        }

        static readonly ConcurrentDictionary<Type, Type[]> _handledMessageTypesCache = new ConcurrentDictionary<Type, Type[]>();

        List<IHandleMessages<TMessage>> GetAllHandlerInstances<TMessage>()
        {
            var implementedInterfaces = _handledMessageTypesCache
                .GetOrAdd(typeof(TMessage), _ => typeof(TMessage).GetBaseTypes().Concat(new[] { typeof(TMessage) }).Select(type => typeof(IHandleMessages<>).MakeGenericType(type)).ToArray());

            return implementedInterfaces
                .SelectMany(implementedInterface => _windsorContainer.ResolveAll(implementedInterface).Cast<IHandleMessages>())
                .Cast<IHandleMessages<TMessage>>()
                .ToList();
        }
    }
}
