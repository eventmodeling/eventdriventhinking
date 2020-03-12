using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace EventDrivenThinking.EventInference.EventStore
{
    public static class TypeSourceHash
    {
        public static Guid Xor(this Guid a, Guid b)
        {
            var keyBytes = a.ToByteArray();
            var srcBytes = b.ToByteArray();
            for (int i = 0; i < keyBytes.Length; i++) keyBytes[i] ^= srcBytes[i];
            return new Guid(keyBytes);
        }
        public static Guid ComputeSourceHash(this Type classType)
        {
            using (var hash = MD5.Create())
            {
                hash.Initialize();

                foreach (var i in classType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                                                       BindingFlags.Instance | BindingFlags.Static |
                                                       BindingFlags.GetField | BindingFlags.SetField |
                                                       BindingFlags.GetProperty | BindingFlags.SetProperty |
                                                       BindingFlags.InvokeMethod).Where(x=>x.DeclaringType != typeof(object)))
                {
                    switch (i)
                    {
                        case MethodInfo m:
                            hash.ComputeHash(m.GetMethodBody().GetILAsByteArray());
                            break;

                        case PropertyInfo p:
                            if (p.CanRead) hash.ComputeHash(p.GetMethod.GetMethodBody().GetILAsByteArray());
                            if (p.CanWrite) hash.ComputeHash(p.SetMethod.GetMethodBody().GetILAsByteArray());

                            break;
                        case FieldInfo f:
                            hash.ComputeHash(Encoding.Default.GetBytes(f.FieldType.FullName));
                            hash.ComputeHash(Encoding.Default.GetBytes(f.Name));
                            break;
                        default:
                            break;
                    }
                }
                return new Guid(hash.Hash);
            }
        }
    }
}