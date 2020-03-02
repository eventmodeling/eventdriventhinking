using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;



namespace EventDrivenThinking.Reflection
{
    

    public class Ctor<TInterface>
    {
        private static readonly ConcurrentDictionary<Type, Func<TInterface>> _ctors =
            new ConcurrentDictionary<Type, Func<TInterface>>();

        /// <summary>
        ///     Creates type, returns cast object.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="instanceType"></param>
        /// <returns></returns>
        public static TInterface Create(Type instanceType)
        {
            //return (TInterface) Activator.CreateInstance(instanceType);
            return _ctors.GetOrAdd(instanceType, CreateCtorFunc)();
        }

        private static Func<TInterface> CreateCtorFunc(Type type)
        {
            var ctor = type.GetConstructor(Array.Empty<Type>());
            var dm = new DynamicMethod($"CreateOrGet{type.Name}",
                MethodAttributes.Static | MethodAttributes.Public,
                CallingConventions.Standard,
                typeof(TInterface),
                Array.Empty<Type>(),
                typeof(Ctor<TInterface>), true);

            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            var ctorFunc = (Func<TInterface>) dm.CreateDelegate(typeof(Func<TInterface>));
            return ctorFunc;
        }
    }
}