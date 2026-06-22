using System;
using System.IO;
using System.Text.Json;

namespace ThreeDBuilder.Core.Config
{
    /// <summary>
    /// Persistance de <see cref="GenerationConfig"/> en JSON (remplace le .ini plat — D5).
    /// Emplacement par défaut : Documents\3DBuilder_config.json.
    /// </summary>
    public sealed class ConfigStore
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public string Path { get; }

        public ConfigStore(string path = null)
        {
            Path = string.IsNullOrWhiteSpace(path) ? DefaultPath() : path;
        }

        public static string DefaultPath()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return System.IO.Path.Combine(docs, "3DBuilder_config.json");
        }

        /// <summary>Charge la config ; renvoie une config par défaut si le fichier est absent ou illisible.</summary>
        public GenerationConfig Load()
        {
            try
            {
                if (!File.Exists(Path)) return new GenerationConfig();
                var json = File.ReadAllText(Path);
                return JsonSerializer.Deserialize<GenerationConfig>(json, Options) ?? new GenerationConfig();
            }
            catch
            {
                return new GenerationConfig();
            }
        }

        public void Save(GenerationConfig config)
        {
            var json = JsonSerializer.Serialize(config ?? new GenerationConfig(), Options);
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(Path, json);
        }
    }
}
