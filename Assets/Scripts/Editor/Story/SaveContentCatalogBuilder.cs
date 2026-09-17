using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using WitchTrial.Story;

namespace WitchTrial.Story.Editor
{
    /// <summary>
    /// 收集剧情存档需要引用的资源，并在运行或构建前生成稳定资源目录。
    /// </summary>
    [InitializeOnLoad]
    public sealed class SaveContentCatalogBuilder : IPreprocessBuildWithReport
    {
        static SaveContentCatalogBuilder()
        {
            EditorApplication.playModeStateChanged += state => {
                if(state==PlayModeStateChange.ExitingEditMode)Rebuild();
            };
        }
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) { Rebuild(); }
        [MenuItem("Witch Trial/Story/Rebuild Save Catalog")]
        public static void Rebuild()
        {
            if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
            const string path="Assets/Resources/SaveContentCatalog.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<SaveContentCatalog>(path);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<SaveContentCatalog>();AssetDatabase.CreateAsset(catalog,path);}
            var assets=new HashSet<Object>();
            foreach(var type in new[]{"StoryChapter","StoryGraph","StoryNode"})
                foreach(var guid in AssetDatabase.FindAssets("t:"+type,new[]{"Assets"}))
                    foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                        if(asset is StoryChapter || asset is StoryGraph || asset is StoryNode)assets.Add(asset);
            foreach(var node in assets.OfType<DialogueNode>().ToArray())foreach(var line in node.lines) {
                if(line==null)continue;
                if(line.background!=null)assets.Add(line.background);
                if(line.appearance!=null)assets.Add(line.appearance);
                foreach(var actor in line.characters ?? new StageCharacter[0])if(actor?.appearance!=null)assets.Add(actor.appearance);
            }
            catalog.entries.Clear();
            foreach(var asset in assets) {
                if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long local))continue;
                catalog.entries.Add(new SaveContentCatalog.Entry {id=guid+":"+local,asset=asset});
            }
            catalog.entries.Sort((a,b)=>string.CompareOrdinal(a.id,b.id));
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Debug.Log("Save catalog: "+catalog.entries.Count+" persistent asset references.");
        }
    }
}
