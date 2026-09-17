using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitchTrial.UI.Panels
{
    [Serializable]
    public sealed class WitchBookEntry
    {
        public string id;
        public WitchBookPanelType category;
        public bool unlocked = true;
        public string title;
        public string slotLabel;
        public string order;
        public bool underground;
        public Sprite thumbnail;
        public Sprite illustration;
        [TextArea(4, 20)] public string description;
    }

    [CreateAssetMenu(fileName = "WitchBookCatalog", menuName = "Witch Trial/UI/Witch Book Catalog")]
    public sealed class WitchBookCatalog : ScriptableObject
    {
        public List<WitchBookEntry> entries = new List<WitchBookEntry>();
    }
}
