using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.Ui
{
    public static class ViewModelFactory<T>
    {
        private static Type _proxyType;

        private static Type ProxyType
        {
            get
            {
                if (_proxyType == null)
                    _proxyType = Create(typeof(T));
                return _proxyType;
            }
        }

        private static TypeBuilder GetTypeBuilder(Type t)
        {
            var typeSignature = $"{t.FullName}.Dynamic";
            var an = new AssemblyName(typeSignature);
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            var tb = moduleBuilder.DefineType(typeSignature,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                t);

            tb.AddInterfaceImplementation(typeof(INotifyPropertyChanged));

            var DelegateCombine = typeof(Delegate).GetMethod("Combine", new[] {typeof(Delegate), typeof(Delegate)});
            var DelegateRemove = typeof(Delegate).GetMethod("Remove", new[] {typeof(Delegate), typeof(Delegate)});
            var InvokeDelegate = typeof(PropertyChangedEventHandler).GetMethod("Invoke");
            var eventBack = tb.DefineField("PropertyChanged", typeof(PropertyChangedEventArgs),
                FieldAttributes.Private);
            var CreateEventArgs = typeof(PropertyChangedEventArgs).GetConstructor(new[] {typeof(string)});

            GenerateCtor(t,tb);
            GenerateAddPropertyChanged(tb, eventBack, DelegateCombine);
            GenerateRemovePropertyChanged(tb, eventBack, DelegateRemove);

            var raiseEvent = GenerateRaiseEventMethod(tb, eventBack, CreateEventArgs, InvokeDelegate);

            foreach (var pinfo in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var methodInfo = pinfo.GetSetMethod();
                if (methodInfo != null)
                {
                    if (methodInfo.IsVirtual)
                    {
                        if (pinfo.PropertyType.IsGenericType && pinfo.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
                            GenerateCollectionGet(pinfo, tb);
                         else 
                            GenerateGetSet(tb, pinfo, raiseEvent);
                    }
                }
                else
                {
                    // Only get?
                    methodInfo = pinfo.GetGetMethod();
                    if (methodInfo.IsVirtual && pinfo.PropertyType.IsGenericType && pinfo.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
                        GenerateCollectionGet(pinfo, tb);
                }
            }

            return tb;
        }

        private static void GenerateCtor(Type type, TypeBuilder tb)
        {
            var ifDefaultIsDefined = type.GetConstructors().Any(x => x.GetParameters().Length == 0);
            if (!ifDefaultIsDefined)
            {
                var ctorMth = type.GetConstructors().OrderByDescending(x => x.GetParameters().Length).First();
                var ctorParams = ctorMth.GetParameters().Select(x=>x.ParameterType).ToArray();
                var builder = tb.DefineConstructor(
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, CallingConventions.Standard, ctorParams);
                var gen = builder.GetILGenerator();
                gen.Emit(OpCodes.Ldarg_0);
                if(ctorParams.Length >= 1)
                    gen.Emit(OpCodes.Ldarg_1);
                if (ctorParams.Length >= 2)
                    gen.Emit(OpCodes.Ldarg_2);
                if (ctorParams.Length >= 3)
                    gen.Emit(OpCodes.Ldarg_3);
                if(ctorParams.Length > 3)
                    throw new NotImplementedException("Sorry, ctor with more than 3 params is not supported.");

                gen.Emit(OpCodes.Call, ctorMth);
                gen.Emit(OpCodes.Ret);
            }
        }

        private static void GenerateCollectionGet(PropertyInfo pinfo, TypeBuilder tb)
        {
            var itemType = pinfo.PropertyType.GetGenericArguments()[0];
            var pb = tb.DefineProperty(
                pinfo.Name, PropertyAttributes.None, pinfo.PropertyType, Type.EmptyTypes);
            var getAttr =
                MethodAttributes.Public | MethodAttributes.SpecialName |
                MethodAttributes.HideBySig | MethodAttributes.Virtual;

            GenerateGetToCollectionMethodOptimized(tb, pinfo, itemType, getAttr, pb);
            //GenerateGetToCollectionMethod(tb, pinfo, itemType, getAttr, pb);
        }

        private static void GenerateGetSet(TypeBuilder tb, PropertyInfo pinfo, MethodBuilder raiseEvent)
        {
            var pb = tb.DefineProperty(
                pinfo.Name, PropertyAttributes.None, pinfo.PropertyType, Type.EmptyTypes);
            var attr =
                MethodAttributes.Public | MethodAttributes.SpecialName |
                MethodAttributes.HideBySig | MethodAttributes.Virtual;

            if (pinfo.CanRead)
                GenerateGetMethod(tb, pinfo, attr, pb);
            if (pinfo.CanWrite)
                GenerateSetMethodWithIf(tb, pinfo, attr, raiseEvent, pb);
        }

        private static void GenerateAddPropertyChanged(TypeBuilder tb, FieldBuilder eventBack,
            MethodInfo DelegateCombine)
        {
            var addPropertyChanged = tb.DefineMethod(
                "add_PropertyChanged", MethodAttributes.Public |
                                       MethodAttributes.Virtual |
                                       MethodAttributes.SpecialName |
                                       MethodAttributes.Final |
                                       MethodAttributes.HideBySig |
                                       MethodAttributes.NewSlot, typeof(void),
                new[] {typeof(PropertyChangedEventHandler)});

            var gen = addPropertyChanged.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, eventBack);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, DelegateCombine);
            gen.Emit(OpCodes.Castclass, typeof(PropertyChangedEventHandler));
            gen.Emit(OpCodes.Stfld, eventBack);
            gen.Emit(OpCodes.Ret);
        }

        private static void GenerateRemovePropertyChanged(TypeBuilder tb, FieldBuilder eventBack,
            MethodInfo DelegateRemove)
        {
            ILGenerator gen;
            var AddPropertyChanged = tb.DefineMethod(
                "remove_PropertyChanged",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName |
                MethodAttributes.Final |
                MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(void), new[] {typeof(PropertyChangedEventHandler)});
            gen = AddPropertyChanged.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, eventBack);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, DelegateRemove);
            gen.Emit(OpCodes.Castclass, typeof(PropertyChangedEventHandler));
            gen.Emit(OpCodes.Stfld, eventBack);
            gen.Emit(OpCodes.Ret);
        }

        private static MethodBuilder GenerateRaiseEventMethod(TypeBuilder tb, FieldBuilder eventBack,
            ConstructorInfo CreateEventArgs, MethodInfo InvokeDelegate)
        {
            ILGenerator gen;
            var raiseEvent = tb.DefineMethod(
                "OnPropertyChanged", MethodAttributes.Public,
                typeof(void), new[] {typeof(string)});
            gen = raiseEvent.GetILGenerator();
            var lblDelegateOk = gen.DefineLabel();
            gen.DeclareLocal(typeof(PropertyChangedEventHandler));
            
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, eventBack);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Ceq);
            gen.Emit(OpCodes.Brtrue, lblDelegateOk);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Newobj, CreateEventArgs);
            gen.Emit(OpCodes.Callvirt, InvokeDelegate);
            gen.MarkLabel(lblDelegateOk);
            gen.Emit(OpCodes.Ret);
            return raiseEvent;
        }

        private static void GenerateSetMethod(TypeBuilder tb, PropertyInfo pinfo, MethodAttributes getSetAttr,
            MethodBuilder raiseEvent, PropertyBuilder pb)
        {
            ILGenerator gen;
            var setMethod =
                tb.DefineMethod(
                    "set_" + pinfo.Name, getSetAttr, null, new[] {pinfo.PropertyType});
            gen = setMethod.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, pinfo.GetSetMethod());

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, pinfo.Name);
            gen.Emit(OpCodes.Call, raiseEvent);

            gen.Emit(OpCodes.Ret);
            pb.SetSetMethod(setMethod);
        }

        private static void GenerateSetMethodWithIf(TypeBuilder tb, PropertyInfo pinfo, MethodAttributes getSetAttr,
            MethodBuilder raiseEvent, PropertyBuilder pb)
        {
            ILGenerator gen;
            var setMethod =
                tb.DefineMethod(
                    "set_" + pinfo.Name, getSetAttr, null, new[] {pinfo.PropertyType});
            gen = setMethod.GetILGenerator();
            var resultLb = gen.DefineLabel();
            var prv = gen.DeclareLocal(pinfo.PropertyType);
            var b = gen.DeclareLocal(typeof(bool));

            // prv = Get
            //gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, pinfo.GetGetMethod());
            gen.Emit(OpCodes.Stloc_0);

            // if
            var mth = typeof(EqualityComparer<>).MakeGenericType(pinfo.PropertyType).GetMethod("get_Default");
            gen.Emit(OpCodes.Call, mth);
            gen.Emit(OpCodes.Ldloc, prv);
            gen.Emit(OpCodes.Ldarg_1);

            // equals.
            var mth2 = typeof(EqualityComparer<>).MakeGenericType(pinfo.PropertyType)
                .GetMethod("Equals", new[] {pinfo.PropertyType, pinfo.PropertyType});
            gen.Emit(OpCodes.Callvirt, mth2);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ceq);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldloc, b);

            gen.Emit(OpCodes.Brfalse, resultLb);
            //gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, pinfo.GetSetMethod());
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldstr, pinfo.Name);
            gen.Emit(OpCodes.Call, raiseEvent);

            gen.MarkLabel(resultLb);

            gen.Emit(OpCodes.Ret);
            pb.SetSetMethod(setMethod);
        }

        private static void GenerateGetMethod(TypeBuilder tb, PropertyInfo pinfo, MethodAttributes getSetAttr,
            PropertyBuilder pb)
        {
            ILGenerator gen;
            var getMethod =
                tb.DefineMethod(
                    "get_" + pinfo.Name, getSetAttr, pinfo.PropertyType, Type.EmptyTypes);
            gen = getMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, pinfo.GetGetMethod());
            gen.Emit(OpCodes.Ret);
            pb.SetGetMethod(getMethod);
        }

        private static void GenerateGetToCollectionMethod(TypeBuilder tb, PropertyInfo pinfo, Type itemType,
            MethodAttributes attr,
            PropertyBuilder pb)
        {
            var field = tb.DefineField($"__{pinfo.Name}", pinfo.PropertyType, FieldAttributes.Private);
            ILGenerator gen;
            var getMethod =
                tb.DefineMethod(
                    "get_" + pinfo.Name, attr, pinfo.PropertyType, Type.EmptyTypes);
            gen = getMethod.GetILGenerator();

            var l1 = gen.DeclareLocal(typeof(bool));
            var l2 = gen.DeclareLocal(pinfo.PropertyType);
            var ret = gen.DefineLabel();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Ceq);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Brfalse_S, ret);

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, pinfo.GetGetMethod());
            var collectionType = typeof(ViewModelCollection<>).MakeGenericType(itemType);
            var ctor = collectionType.GetConstructor(new[] {pinfo.PropertyType});

            gen.Emit(OpCodes.Newobj, ctor);
            gen.Emit(OpCodes.Stfld, field);

            gen.MarkLabel(ret);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
            gen.Emit(OpCodes.Stloc_1);

            //
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Ret);

            pb.SetGetMethod(getMethod);
        }

        private static void GenerateGetToCollectionMethodOptimized(TypeBuilder tb, PropertyInfo pinfo, Type itemType,
            MethodAttributes attr,
            PropertyBuilder pb)
        {
            var field = tb.DefineField($"__{pinfo.Name}", pinfo.PropertyType, FieldAttributes.Private);
            ILGenerator gen;
            var getMethod =
                tb.DefineMethod(
                    "get_" + pinfo.Name, attr, pinfo.PropertyType, Type.EmptyTypes);
            gen = getMethod.GetILGenerator();

            var l1 = gen.DeclareLocal(pinfo.PropertyType);
            var l2 = gen.DeclareLocal(pinfo.PropertyType);
            var r1 = gen.DefineLabel();


            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
            gen.Emit(OpCodes.Dup);
            gen.Emit(OpCodes.Brtrue_S, r1);

            gen.Emit(OpCodes.Pop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, pinfo.GetGetMethod());
            var collectionType = typeof(ViewModelCollection<>).MakeGenericType(itemType);
            var ctor = collectionType.GetConstructor(new[] {pinfo.PropertyType});

            gen.Emit(OpCodes.Newobj, ctor);
            gen.Emit(OpCodes.Dup);
            gen.Emit(OpCodes.Stloc, l1);
            gen.Emit(OpCodes.Stfld, field);
            gen.Emit(OpCodes.Ldloc, l1);

            gen.MarkLabel(r1);
            gen.Emit(OpCodes.Stloc, l2);

            gen.Emit(OpCodes.Ldloc, l2);
            gen.Emit(OpCodes.Ret);

            pb.SetGetMethod(getMethod);
        }

        private static Type Create(Type root)
        {
            var tb = GetTypeBuilder(root);

            var t = tb.CreateType();

            return t;
        }

        /// <summary>
        /// Creates a proxy that implements INotifyPropertyChanted interface
        /// </summary>
        /// <returns></returns>
        public static T Create()
        {
            return Ctor<T>.Create(ProxyType);
           
        }
        /// <summary>
        /// Creates a proxy that implements INotifyPropertyChanted interface
        /// </summary>
        /// <returns></returns>
        public static T Create(IServiceProvider serviceProvider)
        {
           return (T)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, ProxyType);
        }
    }
    public interface IViewModelCollection<T> : INotifyCollectionChanged,
        ICollection<T>, INotifyPropertyChanged, IModel
    {

    }


    public class ViewModelCollection<T> : IViewModelCollection<T>
    {
        private readonly IList<T> _inner;

        public ViewModelCollection(IList<T> inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_inner).GetEnumerator();
        }

        public void Add(T item)
        {
            _inner.Add(item);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
            OnPropertyChanged(nameof(Count));
        }

        public void Clear()
        {
            _inner.Clear();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
            OnPropertyChanged(nameof(Count));
        }

        public bool Contains(T item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            var index = _inner.IndexOf(item);
            if (index >= 0)
            {
                _inner.RemoveAt(index);
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
                OnPropertyChanged(nameof(Count));
                return true;
            }

            return false;

        }

        public int Count => _inner.Count;

        public bool IsReadOnly => _inner.IsReadOnly;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            var ev = CollectionChanged;
            ev?.Invoke(this, args);
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }
        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}