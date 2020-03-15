using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Unity.Lifetime;

namespace EventDrivenThinking.Integrations.Unity
{

    public class UnityServiceCollection : IServiceCollection
    {
        private readonly ServiceCollection _serviceCollection;
        private readonly IUnityContainer _unityContainer;

        public UnityServiceCollection(IUnityContainer unityContainer)
        {
            _unityContainer = unityContainer;
            _serviceCollection = new ServiceCollection();
        }
        

        public void Add(ServiceDescriptor item)
        {
            ((ICollection < ServiceDescriptor > )_serviceCollection).Add(item);
            Register(item);
        }

        private void Register(ServiceDescriptor item)
        {
            //var isRegistered = _unityContainer.IsRegistered(item.ServiceType);

            //if(!isRegistered)
            switch (item.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    if(item.ImplementationType != null)
                        _unityContainer.RegisterSingleton(item.ServiceType, item.ImplementationType);
                    else if (item.ImplementationInstance != null)
                        _unityContainer.RegisterInstance(item.ServiceType, item.ImplementationInstance);
                    else if (item.ImplementationFactory != null)
                        _unityContainer.RegisterFactory(item.ServiceType, u => item.ImplementationFactory(u.Resolve<IServiceProvider>()), FactoryLifetime.Singleton);
                    break;
                case ServiceLifetime.Scoped:
                    if (item.ImplementationType != null)
                    {
                        if (_unityContainer.IsRegistered(item.ServiceType))
                        {
                            // heuristic
                            var type = _unityContainer.Registrations.First(x => x.RegisteredType == item.ServiceType);
                            
                            if (!_unityContainer.IsRegistered(type.MappedToType,type.MappedToType.FullName))
                                _unityContainer.RegisterType(item.ServiceType, type.MappedToType,
                                    type.MappedToType.FullName, new HierarchicalLifetimeManager());

                            _unityContainer.RegisterType(item.ServiceType, item.ImplementationType,
                                new HierarchicalLifetimeManager());
                        } else
                            _unityContainer.RegisterType(item.ServiceType, item.ImplementationType, new HierarchicalLifetimeManager());
                    }
                    else if (item.ImplementationInstance != null)
                        throw new NotImplementedException();
                    else if (item.ImplementationFactory != null)
                        _unityContainer.RegisterFactory(item.ServiceType, u => item.ImplementationFactory(u.Resolve<IServiceProvider>()),
                            new HierarchicalLifetimeManager());
                    break;
                    break;
                case ServiceLifetime.Transient:
                    if (item.ImplementationType != null)
                        _unityContainer.RegisterType(item.ServiceType, item.ImplementationType, new TransientLifetimeManager());
                    else if (item.ImplementationFactory != null)
                        _unityContainer.RegisterFactory(item.ServiceType, u => item.ImplementationFactory(u.Resolve<IServiceProvider>()),
                            FactoryLifetime.Transient);
                    else throw new NotImplementedException();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Unregister(ServiceDescriptor item)
        {
            throw new NotImplementedException();
        }
        public void Clear()
        {
            _serviceCollection.Clear();
        }

        public bool Contains(ServiceDescriptor item)
        {
            return _serviceCollection.Contains(item);
        }

        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            _serviceCollection.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ServiceDescriptor> GetEnumerator()
        {
            return _serviceCollection.GetEnumerator();
        }

        public int IndexOf(ServiceDescriptor item)
        {
            return _serviceCollection.IndexOf(item);
        }

        public void Insert(int index, ServiceDescriptor item)
        {
            _serviceCollection.Insert(index, item);
            Register(item);
        }

        public bool Remove(ServiceDescriptor item)
        {
            Unregister(item);
            return _serviceCollection.Remove(item);
        }

        public void RemoveAt(int index)
        {
            var item = this[index];
            Unregister(item);

            _serviceCollection.RemoveAt(index);
        }

        public int Count => _serviceCollection.Count;

        public bool IsReadOnly => _serviceCollection.IsReadOnly;

        public ServiceDescriptor this[int index]
        {
            get => _serviceCollection[index];
            set
            {
                Unregister(_serviceCollection[index]);
                _serviceCollection[index] = value;
                Register(value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
