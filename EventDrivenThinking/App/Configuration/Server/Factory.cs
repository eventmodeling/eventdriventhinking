using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventDrivenThinking.App.Configuration.Server
{
    public interface IFactoryBuilder
    {
        Func<IServiceProvider, object> Create();
        void Configure(Type t);
    }
    public interface IFactoryBuilder<TInterface> : IFactoryBuilder
    {
        new Func<IServiceProvider, TInterface> Create();
        void Configure<D>() where D: class, TInterface;
    }

    public class FactoryBuilder<TInterface> : IFactoryBuilder<TInterface>
    {
        private Type _type;

        public Func<IServiceProvider, TInterface> Create()
        {
            return (sp => (TInterface)ActivatorUtilities.CreateInstance(sp, _type));
        }

        public void Configure(Type t)
        {
            if (typeof(TInterface).IsAssignableFrom(t))
                _type = t;
            else throw new ArgumentException($"Type {t.Name} is not assignable to {typeof(TInterface).Name}.");
        }

        public void Configure<D>() where D : class, TInterface
        {
            this._type = typeof(D);
        }


        Func<IServiceProvider, object> IFactoryBuilder.Create()
        {
            return (sp => ActivatorUtilities.CreateInstance(sp,_type));
        }
    }

    public static class FactoryExtensions
    {
        public static IFactoryBuilder CreateFactory(this Type interfaceType)
        {
            var ft = typeof(FactoryBuilder<>).MakeGenericType(interfaceType);
            return (IFactoryBuilder)Activator.CreateInstance(ft);
        }
        public static IFactoryBuilder GetFactoryBuilder(this IServiceProvider sp, Type interfaceType)
        {
            return (IFactoryBuilder) sp.GetService(typeof(IFactoryBuilder<>).MakeGenericType(interfaceType));
        }
        public static IFactoryBuilder AddFactoryBuilder(this IServiceCollection collection, Type interfaceType)
        {
            var builderType = typeof(FactoryBuilder<>).MakeGenericType(interfaceType);
            var builder = (IFactoryBuilder)Activator.CreateInstance(builderType);
            var builderInterface = typeof(IFactoryBuilder<>).MakeGenericType(interfaceType);
            
            collection.AddSingleton(builderInterface, builder);

            return builder;
        }
    }
}