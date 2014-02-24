namespace WixBacktraceExtension.Backtrace
{
    using System;
    using System.Collections.Generic;

    public class QuotedArgsSplitter
    {
        public QuotedArgsSplitter(string args)
        {
            NamedArguments = new Dictionary<string, string>();
            var clean = args.Trim();
            if (clean.IndexOf('"') < 0)
            {
                Primary = clean;
                return;
            }

            var bits = clean.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
            if (bits.Length < 1) return;

            var idx = (clean.StartsWith("\"")) ? (1) : (0);
            if (idx == 1)
            {
                Primary = bits[0];
            }

            for (var i = idx; i < bits.Length; i+=2)
            {
                if (i + 1 >= bits.Length)  break; // must come in pairs
                NamedArguments.Add(bits[i].Trim(), bits[i + 1]);
            }
        }

        /// <summary>
        /// The first argument, if no name is given at the beginning
        /// </summary>
        public string Primary { get; protected set; }

        /// <summary>
        /// Named arguments
        /// </summary>
        public Dictionary<string, string> NamedArguments { get; protected set; }

        /// <summary>
        /// Look up a named argument, with a default value if argument not provided
        /// </summary>
        public string WithDefault(string key, string defaultValue)
        {
            if (NamedArguments.ContainsKey(key)) return NamedArguments[key];
            return defaultValue;
        }

        /// <summary>
        /// Look up a named argument, with an exception thrown if argument not provided
        /// </summary>
        public string Required(string key)
        {
            if (NamedArguments.ContainsKey(key)) return NamedArguments[key];
            throw new Exception("Expected argument named \"" + key + "\", but it was missing");
        }

        /// <summary>
        /// Return the primary argument, with an exception thrown if argument not provided
        /// </summary>
        public string PrimaryRequired()
        {
            if (string.IsNullOrWhiteSpace(Primary)) throw new Exception("Expected primary argument, but it was missing");
            return Primary;
        }
    }
}