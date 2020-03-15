using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace EventDrivenThinking.Reflection
{
    
    /// <summary>
    ///     Each factory should be singleton in the app. Make sure it is.
    /// </summary>
    public abstract class MarkupFactoryBase
    {
        public const string AssemblyName = "DynamicMarkupAssembly";

        private static ModuleBuilder moduleBuilder;
        protected readonly List<Type> _services;

        private readonly Lazy<Type> _markupType;

        protected MarkupFactoryBase()
        {
            _services = new List<Type>();
            _markupType = new Lazy<Type>(CreateMarkupType);
        }

        public abstract string TypeFullName { get; }

        public Type MarkupType => _markupType.Value;

        public IEnumerable<Type> Services => _services;

        protected ModuleBuilder ModuleBuilder
        {
            get
            {
                if (moduleBuilder == null)
                {
                    var assemblyBuilder =
                        AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(AssemblyName),
                            AssemblyBuilderAccess.Run);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
                }

                return moduleBuilder;
            }
        }

        protected abstract Type CreateMarkupType();

        public T Create<T>()
        {
            return Ctor<T>.Create(MarkupType);
        }
    }
}