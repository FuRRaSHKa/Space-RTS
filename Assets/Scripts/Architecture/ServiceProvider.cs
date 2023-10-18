using System;
using System.Collections.Generic;

namespace HalloGames.Architecture.Services
{
    public interface IService
    {

    }

    public interface IServiceProvider
    {
        public void AddService<TService>(TService service) where TService : IService;
        public void RemoveService<TService>();
        public TService GetService<TService>() where TService : IService;
        public void ClearServices();
    }

    public class ServiceProvider : IServiceProvider
    {
        private Dictionary<Type, IService> _services;

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
            return (TService)_services[typeof(TService)];
        }

        public void AddService<TService>(TService service) where TService : IService
        {
            _services.Add(typeof(TService), service);
        }

        public void RemoveService<TService>()
        {
            _services.Remove(typeof(TService));
        }
    }

}