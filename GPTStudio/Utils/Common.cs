using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GPTStudio.Utils
{
    internal static partial class Common
    {
        public static string GenerateRandomHash(string str)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(str);
            byte[] hashBytes = MD5.HashData(inputBytes);
            byte[] buff = RandomNumberGenerator.GetBytes(8);
            // Return a Base64 string representation of the random number.
            return Regexes.WindowsFileName().Replace($"{Convert.ToHexString(hashBytes) + "/"}{Convert.ToBase64String(buff)}", "$1");
        }

        public static void BinarySerialize(object obj, string fullPath)
        {
            using var file = new FileStream(fullPath, FileMode.Create);
            WriteObjectToStream(file, obj);
        }

        public static T BinaryDeserialize<T>(string fullPath)
        {
            if (!File.Exists(fullPath))
                return default;

            using var file = new FileStream(fullPath, FileMode.Open);
            return (T)ReadObjectFromStream(file);
        }

#pragma warning disable SYSLIB0011
        public static void WriteObjectToStream(Stream outputStream, object obj) => new BinaryFormatter().Serialize(outputStream, obj);
        public static object ReadObjectFromStream(Stream inputStream)           => new BinaryFormatter().Deserialize(inputStream);
#pragma warning restore SYSLIB0011

        public static int IndexOf(this StringBuilder builder,char findChar,bool fromEnd = false)
        {
            for (int i = 0; i < builder.Length; i++)
            {
                if (builder[fromEnd ? ^(i+1) : i] == findChar)
                    return fromEnd ? builder.Length-(i+1) : i;
            }

            return -1;
        }
    }
}
