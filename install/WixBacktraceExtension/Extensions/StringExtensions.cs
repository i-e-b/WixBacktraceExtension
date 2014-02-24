namespace WixBacktraceExtension.Extensions
{
    using System.IO;
    using System.Linq;
    using System.Text;

    public static class StringExtensions
    {
        public static string FilterJunk(this string src)
        {
            var dst = new StringBuilder();
            var filter = Path.GetInvalidFileNameChars().Concat(new[] { ' ', '-', '.'}).ToArray();
            foreach (var ch in src)
            {
                dst.Append(filter.Contains(ch) ? '_' : ch);
            }
            return dst.ToString();
        }
    }
}