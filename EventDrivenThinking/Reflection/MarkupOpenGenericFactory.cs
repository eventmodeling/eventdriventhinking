using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventDrivenThinking.Utils;

namespace EventDrivenThinking.Reflection
{
    
    public class MarkupOpenGenericFactory : MarkupFactoryBase
    {
        
        public MarkupOpenGenericFactory(Type baseSourceType, Type openGenericServiceType)
        {
            var typeSignature = $"{baseSourceType.FullName}{openGenericServiceType.Name}.Marked";
            
            var tb = ModuleBuilder.DefineType(typeSignature,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                baseSourceType);

            var methods = openGenericServiceType.GetMethods();
            if (methods.Length > 1) throw new NotSupportedException("Only one method is supported for generic interface");
            var method = methods[0];
            foreach (var m in baseSourceType.GetMethods())
            {
                if (m.Name == method.Name)
                {
                    if(!m.IsVirtual)
                        throw new InvalidOperationException($"Method '{m.Name}' of class '{baseSourceType.FullName}' must be virtual.");

                    var genericParameters = openGenericServiceType.GetGenericArguments();
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
                        var makeGenericType = openGenericServiceType.MakeGenericType(genericArguments);
                        _services.Add(makeGenericType);
                        tb.AddInterfaceImplementation(makeGenericType);
                    }
                }
            }

            try
            {
                _markupType = tb.CreateType();
            }
            catch (TypeLoadException ex)
            {
                if (!baseSourceType.IsPublic)
                    throw new InvalidOperationException($"Please add [assembly:InternalsVisibleTo(\"DynamicMarkupAssembly\")] to '{baseSourceType.Assembly}'",ex);
                if (!openGenericServiceType.IsPublic)
                    throw new InvalidOperationException($"Please add [assembly:InternalsVisibleTo(\"DynamicMarkupAssembly\")] to '{openGenericServiceType.Assembly}'",ex);

            }
        }

        
    }
}