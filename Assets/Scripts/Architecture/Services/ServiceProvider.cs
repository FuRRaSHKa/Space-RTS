using System;
using System.Collections.Generic;

namespace HalloGames.Architecture.Services
{
    public interface IService
    {

    }

    public interface IServiceRegistry 
    {
        public void AddService<TService>(TService service) where TService : IService;
        public void AddService(Type serviceType, IService service);
        public void RemoveService<TService>();
        public void RemoveService(Type serviceType);
    }

    public interface IServiceProvider : IServiceRegistry
    {
        public TService GetService<TService>() where TService : IService;
        public void ClearServices();
    }

    public class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, IService> _services;

        public ServiceProvider()
        {
            _services = new Dictionary<Type, IService>();
        }

        public void ClearServices()
        {
            _services.Clear();
        }

        public TService GetService<TService>() where TService : IService
        {
            if (_services.TryGetValue(typeof(TService), out var service))
                return (TService)service;

            throw new InvalidOperationException( $"Service '{typeof(TService).FullName}' is not registered.");

        }

        public void AddService<TService>(TService service) where TService : IService
        {
            _services.Add(typeof(TService), service);
        }

        public void RemoveService<TService>()
        {
            _services.Remove(typeof(TService));
        }

        public void AddService(Type serviceType, IService service)
        {
            _services.Add(serviceType, service);
        }

        public void RemoveService(Type serviceType)
        {
            _services.Remove(serviceType);
        }
    }

}