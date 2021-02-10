# Rebus.CastleWindsor

[![install from nuget](https://img.shields.io/nuget/v/Rebus.Castle.Windsor.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.Castle.Windsor)

Provides a Castle Windsor container adapter for [Rebus](https://github.com/rebus-org/Rebus).

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

---

Use it like this:

```csharp
	// 1 your application lives inside the container:
	readonly IWindsorContainer container = new WindsorContainer();

	// 2

	// 3 ...which is OF COURSE disposed when your application shuts down:
	container.Dispose();
```

In position 2 above, you will probably do something like this:

```csharp
	container.Install(FromAssembly.This());
```

and then you will have a bunch of installers in your assembly. This can be fine and dandy, just remember to not start the bus until you have everything registered correctly in the container.

Because this can be hard to get right, and because I prefer less magical ways of doing things, I usually just do this:

```csharp
	container
		.Install(new ThisAndThatInstaller())
		.Install(new SomethingElseInstaller())
		.Install(new RebusHandlersInstaller())
		.Install(new RebusInstaller())
		.Install(new BackgroundTimersInstaller())
		;
```

which makes me absolutely sure that I know the order in which stuff is registered and started.

My `RebusHandlersInstaller` usually just looks like this:
```csharp

    public class RebusHandlersInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.AutoRegisterHandlersFromAssemblyOf<SomeHandler>();
        }
    }
```

and then the `RebusInstaller` looks somewhat like this:
```csharp
    public class RebusInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            Configure.With(new CastleWindsorContainerAdapter(container))
                .(...)
                .Start();
        }
    }
```
and that is basically all there is to it :)
