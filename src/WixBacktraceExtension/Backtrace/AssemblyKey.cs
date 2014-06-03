namespace WixBacktraceExtension.Backtrace
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json;

    /// <summary>
    /// "name, namespace, version|filepath" for an Assembly
    /// </summary>
    [JsonConverter(typeof(AssemblyKeyConverter))]
    public class AssemblyKey : IEquatable<AssemblyKey>
    {
        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return (_key != null ? ComponentId(_key).GetHashCode() : 0);
        }

        readonly string _key;

        /// <summary>
        /// Read from assembly
        /// </summary>
        public AssemblyKey(Assembly assm) : this(assm.Location, assm.FullName) { }

        /// <summary>
        /// Read from file
        /// </summary>
        public AssemblyKey(string filePath, string assemblyFullName)
        {
            var bits = assemblyFullName.Split(',');
            _key = string.Join(",", bits.Take(2)) + "|" + filePath;
            Version = double.Parse(string.Join(".", (bits[1].Split('=')[1]).Split('.').Take(2)));
        }

        /// <summary>
        /// Read from string
        /// </summary>
        public AssemblyKey(string key)
        {
            _key = key;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(AssemblyKey other)
        {
            return ComponentId(_key) == ComponentId(other._key);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            return obj is AssemblyKey && Equals((AssemblyKey)obj);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return _key;
        }

        /// <summary>
        /// turn an assembly key string into a unique component key name
        /// </summary>
        public static string ComponentId(string keystring)
        {
            var bits = keystring.Split('|');
            return "cmp_" + bits[0].Replace(", Version=", "_").Replace(".", "_");
        }

        /// <summary>
        /// Get the source file path for an assembly key
        /// </summary>
        public static string FilePath(string keystring)
        {
            return keystring.Split('|')[1];
        }

        /// <summary>
        /// turn an assembly key string into a unique file key name
        /// </summary>
        public static string FileId(string keystring)
        {
            var bits = keystring.Split('|');
            return "file_" + bits[0].Replace(", Version=", "_").Replace(".", "_");
        }

        /// <summary>
        /// Major.Minor version of assembly
        /// </summary>
        public double Version { get; private set; }

        /// <summary>
        /// Name of file
        /// </summary>
        public string FileName { get { return Path.GetFileName(FilePath(_key)); } }

        /// <summary>
        /// Target path of file
        /// </summary>
        public string TargetFilePath { get { return FilePath(_key); } }
    }

    /// <summary>
    /// Serialiser for AssemblyKeys
    /// </summary>
    public class AssemblyKeyConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter"/> can read JSON.
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new AssemblyKey(reader.Value.ToString());
        }
    }
}