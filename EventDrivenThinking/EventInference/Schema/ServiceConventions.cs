using System;

namespace EventDrivenThinking.EventInference.Schema
{
    public static class ServiceConventions
    {
        static ServiceConventions()
        {
            GetCategoryFromNamespaceFunc = ns =>
            {
                var ix = ns.LastIndexOf('.');
                if (ix > 0)
                    return ns.Substring(ix + 1);
                return ns;
            };
            GetActionNameFromCommandFunc = commandType => commandType.Name;
        }
        public static Func<string,string> GetCategoryFromNamespaceFunc { get; set; }
        public static Func<Type, string> GetActionNameFromCommandFunc { get; set; }

        public static string GetProjectionStreamFromType(Type t)
        {
            return $"{GetCategoryFromNamespace(t.Namespace)}Projection";
        }
        public static string GetCategoryFromNamespace(string ns)
        {
            return GetCategoryFromNamespaceFunc(ns);
        }

        public static string GetActionNameFromCommand(Type commandType)
        {
            return GetActionNameFromCommandFunc(commandType);
        }
    }
}