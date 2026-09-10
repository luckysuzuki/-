using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace WitchTrial.Characters.Editor
{
    public sealed class CharacterTexturePostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(CharacterImporter.Root + "/") || !assetPath.Contains("/Sprites/")) return;
            var layoutPath = Path.GetDirectoryName(Path.GetDirectoryName(assetPath)) + "/layout.json";
            if (!File.Exists(layoutPath)) return;
            var name = Path.GetFileNameWithoutExtension(assetPath);
            var doc = JObject.Parse(File.ReadAllText(layoutPath));
            JToken sprite = null;
            foreach (var part in (JArray)doc["parts"])
            {
                var candidate = part["sprite"];
                if ((string)(candidate["fileName"] ?? candidate["name"]) == name) { sprite = candidate; break; }
            }
            if (sprite == null) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = (float)sprite["pixelsToUnits"];
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2((float)sprite["pivot"]["x"], (float)sprite["pivot"]["y"]);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 8192;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
        }
    }
}
