using System;
using System.Security.Cryptography;
using System.Text;

namespace EventDrivenThinking.Tests.Common
{
    public static class StringExtensions
    {
        public static Guid ToGuid(this string str)
        {
            if (!Guid.TryParse(str, out Guid result))
            {
                using (var hash = MD5.Create())
                {
                    hash.Initialize();
                    var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(str));
                    result = new Guid(bytes);
                }
            }

            return result;
        }
    }
}