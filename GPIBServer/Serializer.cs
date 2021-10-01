using System;
using System.IO;
using System.Text.Json;

namespace GPIBServer
{
    public static class Serializer
    {
        public static event EventHandler<ExceptionEventArgs> ErrorOccured;

        public static JsonSerializerOptions Options { get; set; } =
            new JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

        public static void Serialize<T>(T obj, string path)
        {
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(obj, typeof(T), Options));
            }
            catch (Exception ex)
            {
                RaiseError(ex, path);
            }
        }

        public static T Deserialize<T>(T def, string path)
        {
            try
            {
                return (T)JsonSerializer.Deserialize(File.ReadAllText(path), typeof(T), Options);
            }
            catch (Exception ex)
            {
                RaiseError(ex, path);
                return def;
            }
        }

        private static void RaiseError(Exception ex, object data = null)
        {
            ErrorOccured?.Invoke("Serializer", new ExceptionEventArgs(ex, data));
        }
    }
}
