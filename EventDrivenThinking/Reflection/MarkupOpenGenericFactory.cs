using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventDrivenThinking.Utils;

namespace EventDrivenThinking.Reflection
{


    public class MarkupOpenGenericFactory : MarkupFactoryBase
    {
        private static ConcurrentDictionary<string, MarkupOpenGenericFactory> _factories = new ConcurrentDictionary<string, MarkupOpenGenericFactory>();
        public static MarkupOpenGenericFactory Create(Type baseSourceType, Type openGenericServiceType)
        {
            var key = GetFullTypeName(baseSourceType, openGenericServiceType);
            return _factories.GetOrAdd(key, (key) => new MarkupOpenGenericFactory(baseSourceType, openGenericServiceType));
        }

        private readonly Type _baseSourceType;
        private readonly Type _openGenericServiceType;
        public override string TypeFullName { get; }

        private MarkupOpenGenericFactory(Type baseSourceType, Type openGenericServiceType)
        {
            _baseSourceType = baseSourceType;
            _openGenericServiceType = openGenericServiceType;
            TypeFullName = GetFullTypeName(baseSourceType, openGenericServiceType);
        }

        private static string GetFullTypeName(Type baseSourceType, Type openGenericServiceType)
        {
            return $"{baseSourceType.FullName}{openGenericServiceType.Name}_Marked";
        }

        protected override Type CreateMarkupType()
        {
            var tb = ModuleBuilder.DefineType(TypeFullName,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    _baseSourceType);

            var methods = _openGenericServiceType.GetMethods();
            if (methods.Length > 1) throw new NotSupportedException("Only one method is supported for generic interface");
            var method = methods[0];
            var methodsToScan = _baseSourceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic)
                .Where(x=>x.DeclaringType != typeof(object))
                .ToArray();
            foreach (var m in methodsToScan)
            {
                if (m.Name == method.Name)
                {
                    if (!m.IsVirtual)
                        throw new InvalidOperationException($"Method '{m.Name}' of class '{_baseSourceType.FullName}' must be virtual.");

                    var genericParameters = _openGenericServiceType.GetGenericArguments();
                    Type[] genericArguments = new Type[genericParameters.Length];
                    // Name=T
                    if (method.ReturnType.IsGenericParameter)
                    {
                        var index = genericParameters.IndexOf(x => x.Name == method.ReturnType.Name);
                        genericArguments[index] = m.ReturnType;
                    }
                    else
                    {
                        var templateType = method.ReturnType;
                        var patternMatchingType = m.ReturnType;

                        var templateArguments = templateType.GetGenericArguments();
                        var patternArguments = patternMatchingType.GetGenericArguments();

                        void Search(Type[] templateArguments, Type[] patternArguments)
                        {
                            for (var i = 0; i < templateArguments.Length; i++)
                            {
                                var arg = templateArguments[i];
                                if (arg.IsGenericParameter)
                                {
                                    var index = genericParameters.IndexOf(x => x.Name == arg.Name);
                                    genericArguments[index] = patternArguments[i];
                                }
                                else
                                {
                                    Search(arg.GetGenericArguments(), patternArguments[i].GetGenericArguments());
                                }
                            }
                        }

                        Search(templateArguments, patternArguments);
                    }

                    var parameters = method.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var paramType = parameters[i].ParameterType;
                        if (paramType.IsGenericParameter)
                        {
                            var index = genericParameters.IndexOf(x => x.Name == paramType.Name);
                            genericArguments[index] = m.GetParameters()[i].ParameterType;
                        }
                    }
                    if (genericArguments.All(x => x != null))
                    {
                        var makeGenericType = _openGenericServiceType.MakeGenericType(genericArguments);
                        _services.Add(makeGenericType);
                        tb.AddInterfaceImplementation(makeGenericType);
                    }
                }
            }

            try
            {
                return tb.CreateType();
            }
            catch (TypeLoadException ex)
            {
                if (!_baseSourceType.IsPublic)
                    throw new InvalidOperationException($"Please add [assembly:InternalsVisibleTo(\"DynamicMarkupAssembly\")] to '{_baseSourceType.Assembly}'", ex);
                if (!_openGenericServiceType.IsPublic)
                    throw new InvalidOperationException($"Please add [assembly:InternalsVisibleTo(\"DynamicMarkupAssembly\")] to '{_openGenericServiceType.Assembly}'", ex);
                throw;
            }
        }


    }
}