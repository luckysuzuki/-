using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace WitchTrial.Story
{
    /// <summary>
    /// 表示一个存档槽的版本、时间、标题、缩略图与剧情进度。
    /// </summary>
    [Serializable] public sealed class StorySaveRecord
    {
        public int version=1, slot;
        public string savedAtUtc, title, screenshotBase64;
        public StorySaveData progress;
    }
    /// <summary>
    /// 在持久化目录中按槽位原子写入、读取、检查和删除剧情存档。
    /// </summary>
    public sealed class StorySaveStore
    {
        public const int MaxPages=10, SlotsPerPage=12, Capacity=MaxPages*SlotsPerPage;
        public string Root { get; }
        public StorySaveStore(string root=null) { Root=root ?? Path.Combine(Application.persistentDataPath,"StorySaves"); }
        private string PathFor(int slot) {
            if(slot<0 || slot>=Capacity)throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(Root,"slot-"+(slot+1).ToString("D3")+".json");
        }
        public bool Exists(int slot) => File.Exists(PathFor(slot));
        public StorySaveRecord Read(int slot)
        {
            var path=PathFor(slot); if(!File.Exists(path))return null;
            var record=JsonUtility.FromJson<StorySaveRecord>(File.ReadAllText(path,Encoding.UTF8));
            if(record==null || record.version!=1 || record.slot!=slot || record.progress==null ||
                !DateTimeOffset.TryParse(record.savedAtUtc,out _)) throw new InvalidDataException("存档损坏或版本不兼容。");
            return record;
        }
        public void Write(int slot, StorySaveData data, byte[] screenshot, string title)
        {
            var path=PathFor(slot);
            if(data==null || screenshot==null || screenshot.Length==0)throw new InvalidDataException("存档进度或截图为空。");
            Directory.CreateDirectory(Root);
            var record=new StorySaveRecord {slot=slot, savedAtUtc=DateTimeOffset.UtcNow.ToString("O"),
                title=title, progress=data, screenshotBase64=Convert.ToBase64String(screenshot)};
            var bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(record));
            var temp=path+".tmp";
            try {
                using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)) {
                    stream.Write(bytes,0,bytes.Length); stream.Flush(true);
                }
                // Screenshot and progress are committed together; failed writes preserve the old slot.
                if(File.Exists(path))File.Replace(temp,path,null); else File.Move(temp,path);
            } finally { if(File.Exists(temp))File.Delete(temp); }
        }
        public void Delete(int slot) { var path=PathFor(slot); if(File.Exists(path))File.Delete(path); }
    }
}
