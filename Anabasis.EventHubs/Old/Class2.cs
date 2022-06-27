using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Old
{
    public static class MD5Extensions
    {
        /// <summary>
        /// Return hexadecimal representation of the input string MD5
        /// </summary>
        public static string ToMd5(this string input, Encoding encoding = null)
        {
            var md5Bytes = input.ToMd5Bytes(encoding);

            var sb = new StringBuilder();
            for (int i = 0; i < md5Bytes.Length; i++)
                sb.Append(md5Bytes[i].ToString("x2"));

            return sb.ToString();
        }

        /// <summary>
        /// Convert the input string to a byte array and compute the hash.
        /// </summary>
        public static byte[] ToMd5Bytes(this string input, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            var bytes = encoding.GetBytes(input);
            return bytes.ToMd5Bytes();
        }

        /// <summary>
        /// Returns the Md5 hash computed from the input byte array
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] ToMd5Bytes(this byte[] input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(input);
                return data;
            }
        }

        /// <summary>
        /// Returns the Md5 hash computed from the input byte array
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] ToMd5Bytes(this IEnumerable<byte[]> input)
        {
            using (var md5 = MD5.Create())
            {
                foreach (var block in input)
                {
                    md5.TransformBlock(block, 0, block.Length, block, 0);
                }
                md5.TransformFinalBlock(new byte[0], 0, 0);

                return md5.Hash;
            }
        }

        /// <summary>
        /// Return base 64 representation of the input string MD5
        /// </summary>
        public static string ToMd5Base64(this string input, Encoding encoding = null)
        {
            var bytes = input.ToMd5Bytes(encoding);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Return base 64 representation of the input byte array
        /// </summary>
        public static string ToMd5Base64(this byte[] input)
        {
            return Convert.ToBase64String(input);
        }

        public static string ComputeMd5Hash(this Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(stream).ToBase64();
            }
        }

    }
}
