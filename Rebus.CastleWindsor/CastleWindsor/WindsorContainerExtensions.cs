﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;
using Castle.Windsor;
using Rebus.Config;
using Rebus.Handlers;
// ReSharper disable UnusedMember.Global

namespace Rebus.CastleWindsor;

/// <summary>
/// Extension methods for making it easy to register Rebus handlers in your <see cref="WindsorContainer"/>
/// </summary>
public static class WindsorContainerExtensions
{
    /// <summary>
    /// Uses an instance lifestyle where the instance is bound to (and thus will re-used across) the current Rebus transaction context
    /// </summary>
    public static ComponentRegistration<TService> PerRebusMessage<TService>(this LifestyleGroup<TService> lifestyleGroup) where TService : class
    {
        if (lifestyleGroup == null) throw new ArgumentNullException(nameof(lifestyleGroup));
        return lifestyleGroup.Registration.LifestylePerRebusMessage();
    }

    /// <summary>
    /// Uses an instance lifestyle where the instance is bound to (and thus will re-used across) the current Rebus transaction context
    /// </summary>
    public static ComponentRegistration<TService> LifestylePerRebusMessage<TService>(this ComponentRegistration<TService> registration) where TService : class
    {
        if (registration == null) throw new ArgumentNullException(nameof(registration));
        return registration.LifestyleScoped<RebusScopeAccessor>();
    }

    /// <summary>
    /// Automatically picks up all handler types from the assembly containing <typeparamref name="THandler"/> and registers them in the container
    /// </summary>
    public static IWindsorContainer AutoRegisterHandlersFromAssemblyOf<THandler>(this IWindsorContainer container)
    {
        if (container == null) throw new ArgumentNullException(nameof(container));

        var assemblyToRegister = typeof(THandler).Assembly;

        return RegisterAssembly(container, assemblyToRegister);
    }

    /// <summary>
    /// Automatically picks up all handler types from the specified assembly and registers them in the container
    /// </summary>
    public static IWindsorContainer AutoRegisterHandlersFromAssembly(this IWindsorContainer container, Assembly assemblyToRegister)
    {
        if (container == null) throw new ArgumentNullException(nameof(container));
        if (assemblyToRegister == null) throw new ArgumentNullException(nameof(assemblyToRegister));

        return RegisterAssembly(container, assemblyToRegister);
    }

    /// <summary>
    /// Registers the given handler type under the implemented handler interfaces
    /// </summary>
    public static IWindsorContainer RegisterHandler<THandler>(this IWindsorContainer container) where THandler : IHandleMessages
    {
        if (container == null) throw new ArgumentNullException(nameof(container));
        RegisterType(container, typeof(THandler), false);
        return container;
    }

    static IWindsorContainer RegisterAssembly(IWindsorContainer container, Assembly assemblyToRegister)
    {
        if (container == null) throw new ArgumentNullException(nameof(container));
        if (assemblyToRegister == null) throw new ArgumentNullException(nameof(assemblyToRegister));

        var typesToAutoRegister = GetConcreteTypes(assemblyToRegister)
            .Select(type => new
            {
                Type = type,
                ImplementedHandlerInterfaces = GetImplementedHandlerInterfaces(type).ToList()
            })
            .Where(a => a.ImplementedHandlerInterfaces.Any());

        foreach (var type in typesToAutoRegister)
        {
            RegisterType(container, type.Type, true);
        }

        return container;
    }

    static IEnumerable<Type> GetConcreteTypes(Assembly assemblyToRegister)
    {
        return assemblyToRegister.GetTypes()
            .Where(type => !type.IsInterface && !type.IsAbstract);
    }

    static void RegisterType(IWindsorContainer container, Type typeToRegister, bool auto)
    {
        if (container == null) throw new ArgumentNullException(nameof(container));
        if (typeToRegister == null) throw new ArgumentNullException(nameof(typeToRegister));

        var implementedHandlerInterfaces = GetImplementedHandlerInterfaces(typeToRegister).ToArray();

        if (!implementedHandlerInterfaces.Any()) return;

        container.Register(
            Component.For(implementedHandlerInterfaces)
                .ImplementedBy(typeToRegister)
                .LifestyleTransient()
                .Named($"{typeToRegister.FullName} ({(auto ? "auto-registered" : "manually registered")})")
        );
    }

    static IEnumerable<Type> GetImplementedHandlerInterfaces(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        return type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>));
    }
}