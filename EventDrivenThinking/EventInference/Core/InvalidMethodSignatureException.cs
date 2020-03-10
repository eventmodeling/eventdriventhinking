using System;
using System.Reflection;

namespace EventDrivenThinking.EventInference.Core
{
    public class InvalidMethodSignatureException : Exception
    {
        public string MethodName { get; set; }
        public string ClassName { get; set; }

        public InvalidMethodSignatureException()
        {
            
        }
        public InvalidMethodSignatureException(MethodInfo mth)
        {
            MethodName = mth.Name;
            ClassName = mth.DeclaringType?.FullName;
        }
    }
}