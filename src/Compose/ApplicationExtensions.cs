﻿using Microsoft.Framework.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Compose
{
    public static class ApplicationExtensions
    {
		#region Composition

		public static void UseServices(this Application app, Action<IServiceCollection> configureServices)
		{
			app.Services = new ServiceCollection();
			app.PreServiceConfiguration(app.Services);
			configureServices(app.Services);
			app.Services.TryAdd(ServiceDescriptor.Singleton<DynamicEmitter, DynamicEmitter>());
			app.ApplicationServices = app.Services.BuildServiceProvider();
			if (app.ContainsTransitionMarkers())
				app.ApplyTransitions();
			app.ApplicationServices = app.Services.BuildServiceProvider();
			app.PostServiceConfiguration(app.Services.ToList().AsReadOnly());
		}

		public static void UseServices(this Application app, Func<IServiceCollection, IServiceProvider> configureServices)
		{
			app.Services = new ServiceCollection();
			app.PreServiceConfiguration(app.Services);
			app.ApplicationServices = configureServices(app.Services);
			app.Services.TryAdd(ServiceDescriptor.Singleton<DynamicEmitter, DynamicEmitter>());
			if (app.ContainsTransitionMarkers())
				app.ApplyTransitions();
			app.ApplicationServices = configureServices(app.Services);
			app.PostServiceConfiguration(app.Services.ToList().AsReadOnly());
		}

		#endregion

		#region Transitions

		public static Provider<TService> AddProvider<TService>(this Application app, Action<IServiceCollection> configureServices)
		{
			// intentionally not performing actions here; defer execution so that providers can be added prior to service declarations
			return new ServiceCollectionProvider<TService>(() => app.ApplicationServices, () => app.Services, configureServices);
		}

		public static void Transition<TService>(this Application app, Provider<TService> provider)
		{
			var transitional = app.ApplicationServices.GetRequiredService<TransitionManager<TService>>();
			// TODO : Consider auto-transitional markers for non-transitional services detected for provider
			if (transitional == null) throw new InvalidOperationException($"{typeof(TService).Name} must be registered as a Transitional Service (services.AddTransitional<{typeof(TService).Name}, TImplementation>()");
			transitional.Change(() => provider.GetService());
		}

		internal static Type CreateProxy(this Application app, TypeInfo serviceTypeInfo)
        {
            var emitter = app.ApplicationServices.GetService<DynamicEmitter>();
            if (emitter == null) emitter = app.GetRegisteredDynamicEmitter();
			return emitter.GetManagedDynamicProxy(serviceTypeInfo);
        }

        internal static TService CreateProxy<TService>(this Application app, TypeInfo injectionTypeInfo) where TService : class
        {
			var serviceType = typeof(TService);
			var serviceTypeInfo = serviceType.GetTypeInfo();
			var injectionType = injectionTypeInfo.AsType();
            var proxyType = app.CreateProxy(serviceTypeInfo);
			return app.ApplicationServices.GetRequiredService<TService>();
        }

        private static DynamicEmitter GetRegisteredDynamicEmitter(this Application app)
        {
            return app.ApplicationServices.GetRequiredService<DynamicEmitter>();
        }

		#endregion

		#region Snapshotting

		public static void Snapshot(this Application app)
		{
			app.ApplicationServices?.GetService<TransitionManagerContainer>()?.Snapshot();
		}

		public static void Restore(this Application app)
		{
			app.ApplicationServices?.GetService<TransitionManagerContainer>()?.Restore();
		}

		#endregion
	}
}