using System.Text;

namespace GPTStudio.OpenAI
{
    internal static class StringExtensions
    {
        public static StringContent ToJsonStringContent(this string json) => new(json, Encoding.UTF8, "application/json");
    }
}
