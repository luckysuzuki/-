using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WitchTrial.Story;

namespace WitchTrial.Characters.Editor
{
    public static class CharacterImporter
    {
        public const string Root = "Assets/Characters/Imported";
        private static Vector3 V(JToken t) => new Vector3((float)t["x"], (float)t["y"], (float)t["z"]);
        private static Quaternion Q(JToken t) => new Quaternion((float)t["x"], (float)t["y"], (float)t["z"], (float)t["w"]);

        [MenuItem("Witch Trial/Characters/Rebuild Imported Prefabs")]
        public static void RebuildAll()
        {
            foreach (var dir in Directory.GetDirectories(Root).OrderBy(x => x))
                if (File.Exists(dir + "/layout.json") && !File.Exists(dir + "/" + Path.GetFileName(dir) + ".prefab")) Build(dir.Replace('\\','/'));
            AssetDatabase.SaveAssets();
        }

        public static CharacterActor Build(string folder)
        {
            var layout = JObject.Parse(File.ReadAllText(folder + "/layout.json"));
            var nodes = (JArray)layout["nodes"];
            var parts = (JArray)layout["parts"];
            var name = (string)layout["character"];
            string prefabPath = folder + "/" + name + ".prefab";
            if (File.Exists(prefabPath)) throw new InvalidOperationException("Prefab already exists; refusing to overwrite user edits: " + prefabPath);
            foreach (var part in parts)
            {
                string imagePath = folder + "/Sprites/" + (string)(part["sprite"]["fileName"] ?? part["sprite"]["name"]) + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
                if (importer == null) throw new FileNotFoundException(imagePath);
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                    && Mathf.Approximately(importer.spritePixelsPerUnit, (float)part["sprite"]["pixelsToUnits"])
                    && importer.maxTextureSize == 8192 && importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = (float)part["sprite"]["pixelsToUnits"];
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2((float)part["sprite"]["pivot"]["x"], (float)part["sprite"]["pivot"]["y"]);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 8192;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            var map = new Dictionary<string, Transform>();
            GameObject root = null;
            try
            {
                foreach (var node in nodes)
                {
                    var go = new GameObject((string)node["name"]);
                    map.Add((string)node["id"], go.transform);
                    if ((string)node["parentId"] == "0" && root == null) root = go;
                }
                foreach (var node in nodes)
                {
                    var tr = map[(string)node["id"]];
                    string parent = (string)node["parentId"];
                    if (parent != "0") tr.SetParent(map[parent], false);
                    tr.localPosition = V(node["localPosition"]);
                    tr.localRotation = Q(node["localRotation"]);
                    tr.localScale = V(node["localScale"]);
                }
                foreach (var node in nodes)
                {
                    int index = 0;
                    foreach (var child in (JArray)node["childrenIds"]) map[(string)child].SetSiblingIndex(index++);
                }
                // Root translation is stage placement, not internal character assembly.
                root.transform.localPosition = Vector3.zero;
                var actor = root.AddComponent<CharacterActor>();
                actor.characterId = name;
                actor.branches = nodes.Where(n => Regex.IsMatch((string)n["name"], @"^Head\d+$") && ((JArray)n["childrenIds"]).Count > 0)
                    .OrderBy(n => (string)n["name"]).Select(n => new CharacterBranch { id=(string)n["name"],group="Head",root=map[(string)n["id"]].gameObject }).ToArray();
                if (actor.branches.Length > 0) actor.branches[0].defaultVisible = true;
                var definitions = new List<CharacterPart>();
                var materials = new Dictionary<string, Material>();
                Directory.CreateDirectory(folder + "/Materials");
                foreach (var part in parts)
                {
                    var tr = map[(string)part["transformId"]];
                    var sr = tr.gameObject.AddComponent<SpriteRenderer>();
                    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "/Sprites/" + (string)(part["sprite"]["fileName"] ?? part["sprite"]["name"]) + ".png");
                    sr.sortingOrder = (int)part["sortingOrder"];
                    sr.sortingLayerName = "Default";
                    sr.flipX = (bool?)part["flipX"] ?? false;
                    sr.flipY = (bool?)part["flipY"] ?? false;
                    var c = part["color"];
                    sr.color = new Color((float)c["r"], (float)c["g"], (float)c["b"], (float)c["a"]);
                    var materialName = (string)part["materials"][0]["name"];
                    Material material;
                    if (!materials.TryGetValue(materialName, out material))
                    {
                        material = CreateMaterial(materialName);
                        AssetDatabase.CreateAsset(material, folder + "/Materials/" + materialName.Replace('#','_') + ".mat");
                        materials.Add(materialName, material);
                    }
                    sr.sharedMaterial = material;
                    string id = (string)part["name"];
                    string group = tr.parent.name;
                    bool optional = group.StartsWith("Angle") || id.Contains("ClippingMask") || id.StartsWith("RootBlending")||id.StartsWith("FacialLineDrawing")
                        || group.Contains("Effect") || group.StartsWith("Option") || group.StartsWith("Shadow") || group == "Mask";
                    if (id == "Body") { group = "Body"; optional = false; }
                    definitions.Add(new CharacterPart { id=id, group=group, renderer=sr, optional=optional });
                }
                actor.parts = definitions.ToArray();
                foreach (var part in actor.parts) part.defaultVisible = part.id == "Body" || part.id.Contains("ClippingMask");
                foreach (var group in actor.Groups)
                {
                    if (group.StartsWith("Pale") || group.StartsWith("Sweat") || group.StartsWith("Cheeks") || group == "Arms") continue;
                    var options = actor.parts.Where(p => p.group == group && !p.optional).OrderBy(p => p.id).ToArray();
                    var chosen = options.FirstOrDefault(p => Regex.IsMatch(p.id, @"^Eyes\d*_Normal_Open0?1$") || Regex.IsMatch(p.id, @"^Mouth\d*_Normal_Closed$")) ?? options.FirstOrDefault();
                    if (chosen != null) chosen.defaultVisible = true;
                }
                if (!actor.parts.Any(p => p.group == "Body" && p.defaultVisible))
                {
                    var body = actor.parts.FirstOrDefault(p => p.group == "Body");
                    if (body != null) body.defaultVisible = true;
                }
                actor.ResetAppearance();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath).GetComponent<CharacterActor>();
                CreatePose(prefab, name + "_Default", actor.VisibleParts, folder);
                if (name == "sherry")
                {
                    actor.Select("Eyes", "Eyes_Smile_Closed01"); actor.Select("Mouth", "Mouth_Smile_Closed");
                    CreatePose(prefab, "sherry_Smile", actor.VisibleParts, folder);
                    actor.Select("Eyes", "Eyes_Angry_Open01"); actor.Select("Mouth", "Mouth_Angry_Closed");
                    actor.Select("ArmL", "ArmL02"); actor.Select("ArmR", "ArmR02");
                    CreatePose(prefab, "sherry_Angry", actor.VisibleParts, folder);
                }
                Debug.Log("Character imported: " + name + ", " + actor.parts.Length + " parts");
                return prefab;
            }
            finally { foreach (var tr in map.Values.Where(t => t != null && t.parent == null).ToArray()) UnityEngine.Object.DestroyImmediate(tr.gameObject); }
        }

        private static Material CreateMaterial(string name)
        {
            var mat = new Material(Shader.Find("WitchTrial/CharacterSprite")) { name = name };
            var match = Regex.Match(name, @"#(Mask|Masked)_Ref(\d+)");
            if (match.Success)
            {
                int reference = int.Parse(match.Groups[2].Value);
                mat.SetFloat("_StencilRef", reference);
                if (match.Groups[1].Value == "Mask") mat.SetFloat("_StencilOp", (int)StencilOp.Replace);
                else { mat.SetFloat("_StencilComp", (int)CompareFunction.Equal); mat.SetFloat("_StencilWriteMask", 0); }
            }
            if (name.Contains("Multiply"))
            {
                mat.SetFloat("_Multiply", 1);
                mat.SetFloat("_SrcBlend", (int)BlendMode.DstColor);
                mat.SetFloat("_DstBlend", (int)BlendMode.Zero);
            }
            // Overlay and Softlight are imported for metadata, but remain disabled in the default pose.
            return mat;
        }

        private static CharacterPose CreatePose(CharacterActor prefab, string name, string[] parts, string folder)
        {
            var pose = ScriptableObject.CreateInstance<CharacterPose>();
            pose.characterPrefab = prefab; pose.visibleParts = parts;
            Directory.CreateDirectory(folder + "/Poses");
            AssetDatabase.CreateAsset(pose, folder + "/Poses/" + name + ".asset");
            return pose;
        }

        [MenuItem("Witch Trial/Characters/Open Sherry Preview Scene")]
        public static void OpenPreview()
        {
            const string path = "Assets/Characters/SherryPreview.unity";
            if (File.Exists(path)) { EditorSceneManager.OpenScene(path); return; }
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before creating preview.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Character Preview Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -50);
            camera.orthographic = true; camera.orthographicSize = 13;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.065f,.085f,.12f);
            camera.tag = "MainCamera";
            var prefab = AssetDatabase.LoadAssetAtPath<CharacterActor>(Root + "/sherry/sherry.prefab");
            var instance = ((GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject)).GetComponent<CharacterActor>();
            instance.transform.position = new Vector3(5,0,0);
            var controls = new GameObject("Character Preview Controls").AddComponent<CharacterPreviewControls>();
            controls.actor = instance;
            controls.presets = AssetDatabase.FindAssets("t:CharacterPose", new[]{Root + "/sherry/Poses"})
                .Select(g => AssetDatabase.LoadAssetAtPath<CharacterPose>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            EditorSceneManager.SaveScene(scene, path);
            Selection.activeGameObject = instance.gameObject;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.LookAt(new Vector3(5,0,0), Quaternion.identity, 15, true, true);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
