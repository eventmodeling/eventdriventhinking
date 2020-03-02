using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace EventDrivenThinking.Reflection
{
    public abstract class MarkupFactoryBase
    {
        public const string AssemblyName = "DynamicMarkupAssembly";
        protected Type _markupType;
        protected readonly List<Type> _services;
        public Type MarkupType => _markupType;

        public IEnumerable<Type> Services => _services;

        private static ModuleBuilder moduleBuilder;

        public MarkupFactoryBase()
        {
            _services = new List<Type>();
        }
        protected ModuleBuilder ModuleBuilder
        {
            get
            {
                if (moduleBuilder == null)
                {
                    var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(AssemblyName), AssemblyBuilderAccess.Run);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
                }

                return moduleBuilder;
            }
        }
        public T Create<T>()
        {
            return Ctor<T>.Create(_markupType);
        }
    }
}