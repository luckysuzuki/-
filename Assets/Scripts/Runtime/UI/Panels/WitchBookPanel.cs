using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WitchTrial.UI;

namespace WitchTrial.UI.Panels
{
    public enum WitchBookPanelType { Clue, Character, Map, Rule, Record }

    /// <summary>复用图鉴预制体模板，按目录展示已解锁条目。</summary>
    public class WitchBookPanel : UIPanel
    {
        public static event Action<WitchBookPanelType> onChangePanel;
        public event Action<WitchBookEntry> EntrySelected;
        public WitchBookCatalog catalog;
        public Sprite normalBackground;
        public Sprite ruleBackground;
        [SerializeField] private float tabHiddenX = 90f;
        [SerializeField] private float animationSpeed = 16f;
        public const int VisibleSlots = 10;
        public const float SlotStep = 180f;
        public WitchBookPanelType CurrentCategory { get; private set; }
        public int FirstVisibleIndex { get; private set; }
        public WitchBookEntry SelectedEntry { get; private set; }
        private readonly List<WitchBookEntry> entries = new List<WitchBookEntry>();
        private readonly Dictionary<string, bool> unlockOverrides = new Dictionary<string, bool>();
        private readonly string[] panelNames = { "cluePanel", "CharacterPanel", "mapPanel", "RulePanel", "RecordPanel" };
        private readonly string[] templateNames = { "clueslottemplate", "characterslottemplatge", "mapslottemplate B", "ruleslottemplate", "recordslottemplate" };
        private Toggle[] tabs;
        private UnityAction<bool>[] tabActions;
        private Transform selector;
        private RectTransform item, content;
        private RectTransform[] templates;
        private RectTransform undergroundTemplate;
        private Image background;
        private Button next, previous, close;
        private bool initialized;
        private readonly int[] offsets = new int[5];
        private readonly string[] selections = new string[5];

        private void Awake() { Initialize(); }
        private void OnEnable() { Initialize(); SelectCategory(CurrentCategory); SnapAnimations(); }
        private void OnDisable() { if (initialized) SnapAnimations(); }
        private void OnDestroy()
        {
            if (!initialized) return;
            for (int i = 0; i < tabs.Length; i++) tabs[i].onValueChanged.RemoveListener(tabActions[i]);
            next.onClick.RemoveListener(NextPage);
            previous.onClick.RemoveListener(LastPage);
            close.onClick.RemoveListener(CloseBook);
        }
        private void Initialize()
        {
            if (initialized) return;
            selector = transform.Find("PanelSelector");
            item = (RectTransform)transform.Find("Item");
            background = transform.Find("BackGround").GetComponent<Image>();
            tabs = transform.Find("Options").GetComponentsInChildren<Toggle>(true);
            templates = new RectTransform[5];
            for (int i = 0; i < 5; i++) templates[i] = (RectTransform)item.Find(templateNames[i]);
            undergroundTemplate = (RectTransform)item.Find("mapslottemplate");
            // 原始模板保留在预制体中，运行时只显示克隆条目。
            foreach (Transform child in item)
                if (child.name.Contains("template") || child.name.Contains("templatge") || child.name.StartsWith("slot ("))
                    child.gameObject.SetActive(false);
            var viewport = new GameObject("SlotViewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(item, false);
            viewport.anchorMin = viewport.anchorMax = new Vector2(0f, .5f);
            viewport.pivot = new Vector2(0f, .5f);
            viewport.anchoredPosition = new Vector2(60f, -19.08f);
            viewport.sizeDelta = new Vector2(VisibleSlots * SlotStep, 150f);
            content = new GameObject("SlotContent", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = content.anchorMax = new Vector2(0f, .5f);
            content.pivot = new Vector2(0f, .5f);
            content.sizeDelta = viewport.sizeDelta;
            next = item.Find("next_page(optional)").GetComponent<Button>();
            previous = item.Find("last_page(optional)").GetComponent<Button>();
            close = transform.Find("Button_Back").GetComponent<Button>();
            next.transform.SetAsLastSibling();
            previous.transform.SetAsLastSibling();
            next.onClick.AddListener(NextPage);
            previous.onClick.AddListener(LastPage);
            close.onClick.AddListener(CloseBook);
            tabActions = new UnityAction<bool>[tabs.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabActions[i] = value => { if (value) SelectCategory((WitchBookPanelType)index); };
                tabs[i].onValueChanged.AddListener(tabActions[i]);
                tabs[i].group = transform.Find("Options").GetComponent<ToggleGroup>();
            }
            transform.Find("Options").GetComponent<ToggleGroup>().allowSwitchOff = false;
            initialized = true;
        }
        protected override void OnOpening(object context)
        {
            Initialize();
            if (context is WitchBookCatalog book) catalog = book;
            SelectCategory(context is WitchBookPanelType type ? type : WitchBookPanelType.Clue);
        }
        protected override void OnRefreshed(object context)
        {
            if (context is WitchBookCatalog book) catalog = book;
            SelectCategory(context is WitchBookPanelType type ? type : CurrentCategory);
        }
        private void CloseBook() { if (IsOpen) RequestClose(); else gameObject.SetActive(false); }
        public void SetUnlocked(string id, bool value = true)
        {
            if (string.IsNullOrEmpty(id)) return;
            unlockOverrides[id] = value;
            SelectCategory(CurrentCategory);
        }
        public void SelectCategory(WitchBookPanelType category)
        {
            Initialize();
            if ((int)category < 0 || (int)category >= 5) return;
            CurrentCategory = category;
            for (int i = 0; i < 5; i++)
            {
                tabs[i].SetIsOnWithoutNotify(i == (int)category);
                string caption = new[] { "证物", "人物", "地图", "规定", "记录" }[i];
                tabs[i].GetComponentInChildren<TMP_Text>().text = i == (int)category
                    ? "<color=#FF6688><size=150%>" + caption[0] + "</size></color>" + caption.Substring(1)
                    : "<size=150%>" + caption[0] + "</size>" + caption.Substring(1);
                selector.Find(panelNames[i]).gameObject.SetActive(i == (int)category);
            }
            var recordTitle = selector.Find("Recordname");
            if (recordTitle != null) recordTitle.gameObject.SetActive(category == WitchBookPanelType.Record);
            background.sprite = category == WitchBookPanelType.Rule ? ruleBackground : normalBackground;
            entries.Clear();
            if (catalog != null)
                foreach (var entry in catalog.entries)
                    if (entry != null && entry.category == category && (entry.id != null && unlockOverrides.TryGetValue(entry.id, out bool value) ? value : entry.unlocked)) entries.Add(entry);
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var template = category == WitchBookPanelType.Map && entry.underground ? undergroundTemplate : templates[(int)category];
                var slot = Instantiate(template, content);
                slot.name = "Slot_" + entry.id;
                slot.anchorMin = slot.anchorMax = new Vector2(0f, .5f);
                slot.anchoredPosition = new Vector2(90f + i * SlotStep, 0f);
                slot.gameObject.SetActive(true);
                if (category == WitchBookPanelType.Clue || category == WitchBookPanelType.Character)
                    SetImage(slot.GetComponentInChildren<Image>(true), entry.thumbnail != null ? entry.thumbnail : entry.illustration);
                foreach (var label in slot.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.name == "order") label.text = entry.order;
                    if (label.name == "name") label.text = string.IsNullOrEmpty(entry.slotLabel) ? entry.title : entry.slotLabel;
                }
                var hit = slot.gameObject.GetComponent<Image>() ?? slot.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                hit.raycastTarget = true;
                var handler = slot.GetComponent<ItemSlot>() ?? slot.gameObject.AddComponent<ItemSlot>();
                handler.Bind(entry, SelectEntry);
            }
            FirstVisibleIndex = Mathf.Clamp(offsets[(int)category], 0, Mathf.Max(0, entries.Count - VisibleSlots));
            content.anchoredPosition = new Vector2(-FirstVisibleIndex * SlotStep, 0f);
            UpdateButtons();
            var selected = entries.Find(e => e.id == selections[(int)category]);
            SelectEntry(selected ?? (entries.Count > 0 ? entries[0] : null));
            onChangePanel?.Invoke(category);
        }
        public void NextPage() { MoveStrip(1); }
        public void LastPage() { MoveStrip(-1); }
        private void MoveStrip(int direction)
        {
            FirstVisibleIndex = Mathf.Clamp(FirstVisibleIndex + direction, 0, Mathf.Max(0, entries.Count - VisibleSlots));
            offsets[(int)CurrentCategory] = FirstVisibleIndex;
            UpdateButtons();
        }
        private void UpdateButtons()
        {
            bool overflow = entries.Count > VisibleSlots;
            next.gameObject.SetActive(overflow);
            previous.gameObject.SetActive(overflow);
            previous.interactable = FirstVisibleIndex > 0;
            next.interactable = FirstVisibleIndex < entries.Count - VisibleSlots;
        }
        public void SelectEntry(WitchBookEntry entry)
        {
            if (entry != null && !entries.Contains(entry)) return;
            SelectedEntry = entry;
            selections[(int)CurrentCategory] = entry?.id;
            var panel = selector.Find(panelNames[(int)CurrentCategory]);
            string title = entry?.title ?? "";
            string description = entry?.description ?? "";
            foreach (var label in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.name == "DescribeText") label.text = description;
                else if (label.name == "name-text" || label.name == "rulenametmp" || label.name == "recordnametmp") label.text = title;
                else if (label.name == "order") label.text = entry?.order ?? "";
                else if (CurrentCategory == WitchBookPanelType.Character && label.name == "Text (TMP)") label.text = title;
            }
            var detachedTitle = selector.Find("Recordname");
            if (CurrentCategory == WitchBookPanelType.Record && detachedTitle != null) detachedTitle.GetComponentInChildren<TMP_Text>().text = title;
            foreach (var img in panel.GetComponentsInChildren<Image>(true))
                if (img.name == "clueImage" || img.name == "Characterimg" || img.name == "map")
                    SetImage(img, entry?.illustration != null ? entry.illustration : entry?.thumbnail);
            foreach (var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
            {
                var text = scroll.content.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    float height = Mathf.Max(scroll.viewport.rect.height, text.GetPreferredValues(description, scroll.viewport.rect.width, 0).y + 24f);
                    scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                }
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
            }
            EntrySelected?.Invoke(entry);
        }
        private static void SetImage(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
        }
        private void Update()
        {
            float t = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);
            for (int i = 0; i < tabs.Length; i++)
            {
                var rect = (RectTransform)tabs[i].transform;
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, new Vector2(i == (int)CurrentCategory ? 0f : tabHiddenX, rect.anchoredPosition.y), t);
            }
            content.anchoredPosition = Vector2.Lerp(content.anchoredPosition, new Vector2(-FirstVisibleIndex * SlotStep, 0f), t);
            if (Mathf.Abs(content.anchoredPosition.x + FirstVisibleIndex * SlotStep) < .05f)
                content.anchoredPosition = new Vector2(-FirstVisibleIndex * SlotStep, 0f);
        }
        private void SnapAnimations()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                var rect = (RectTransform)tabs[i].transform;
                rect.anchoredPosition = new Vector2(i == (int)CurrentCategory ? 0f : tabHiddenX, rect.anchoredPosition.y);
            }
            content.anchoredPosition = new Vector2(-FirstVisibleIndex * SlotStep, 0f);
        }
    }
}
