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

        public static string LastPathElement(this string path)
        {
            var try1 = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(try1)) return try1;

            return Path.GetFileNameWithoutExtension(Path.GetDirectoryName(path));
        }

        public static string LimitRight(int numChars, string src)
        {
            return src.Length <= numChars ? src : src.Substring(src.Length - numChars);
        }
    }
}