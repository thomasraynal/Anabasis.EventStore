using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public static class StringExtensions
    {
        public static byte[] ToBase64ByteArray(this string str)
        {
            return Encoding.UTF8.GetBytes(ToBase64String(str));
        }

        public static string ToBase64String(this string str)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }
    }
}
