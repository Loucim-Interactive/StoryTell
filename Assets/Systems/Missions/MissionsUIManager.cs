using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.Missions {
    [DefaultExecutionOrder(-100)]
    public class MissionsUIManager : MonoBehaviour
    {
        private class Mission {
            public string name;
            public readonly List<string> items = new();
            public readonly List<RectTransform> rows = new();
            public RectTransform titleView;
            public RectTransform itemsView;
        }

        [SerializeField] private GameObject uiObject;
        [SerializeField] private RectTransform titleTemplate;
        [SerializeField] private RectTransform itemsTemplate;
        [SerializeField] private RectTransform itemTemplate;
        [SerializeField] private TMP_Text itemLabel;
        [SerializeField] private GameObject itemCompletedMark;
        [SerializeField, Min(0f)] private float missionSpacing = 20f;

        public static MissionsUIManager Instance { get; private set; }

        private readonly List<Mission> _missions = new();
        private bool _initialized, overlaySuppressed, requestedVisible;
        public event System.Action<string> MissionAdded;
        public bool IsVisible => uiObject && uiObject.activeInHierarchy;
        public void ShowOnlyMission(string title) {
            bool changed = false;
            foreach (var mission in _missions) {
                bool visible = mission.name == title;
                if (mission.titleView.gameObject.activeSelf != visible) { mission.titleView.gameObject.SetActive(visible); changed = true; }
                if (mission.itemsView.gameObject.activeSelf != visible) mission.itemsView.gameObject.SetActive(visible);
            }
            if(changed) RefreshLayout();
        }
        public void SetOverlaySuppressed(bool suppressed) { overlaySuppressed = suppressed; if (uiObject) uiObject.SetActive(requestedVisible && !suppressed); }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(this);
                return;
            }

            Instance = this;
            requestedVisible = uiObject && uiObject.activeSelf;
            InitializeUI();
        }

        private void OnDestroy() {
            if (Instance == this) Instance = null;
        }

        private bool InitializeUI() {
            if (_initialized) return true;
            if (!uiObject || !titleTemplate || !itemsTemplate || !itemTemplate || !itemLabel ||
                !titleTemplate.GetComponentInChildren<TMP_Text>(true) || itemLabel.transform.parent != itemTemplate) {
                Debug.LogError("Assign the missions UI and its title, items, item and label templates.", this);
                return false;
            }

            // Keep the authored objects as styling templates; only clones display runtime data.
            titleTemplate.gameObject.SetActive(false);
            itemsTemplate.gameObject.SetActive(false);
            _initialized = true;
            return true;
        }

        public void ShowMissionsUI() {
            if (!InitializeUI()) return;
            requestedVisible = true; uiObject.SetActive(!overlaySuppressed);
            RefreshLayout();
        }

        public void HideMissionsUI() {
            requestedVisible = false; if (uiObject) uiObject.SetActive(false);
        }

        /// <summary>Adds a mission once. Existing titles retain all their items.</summary>
        public void AddMission(string title) {
            if (string.IsNullOrWhiteSpace(title) || FindMission(title) != null || !InitializeUI()) return;

            Mission mission = new Mission { name = title };
            mission.titleView = Instantiate(titleTemplate, titleTemplate.parent);
            mission.titleView.GetComponentInChildren<TMP_Text>(true).text = title;
            mission.titleView.gameObject.SetActive(true);
            mission.itemsView = Instantiate(itemsTemplate, itemsTemplate.parent);
            // The sample rows are placeholders, not objectives belonging to the new mission.
            foreach (Transform child in mission.itemsView) child.gameObject.SetActive(false);
            mission.itemsView.gameObject.SetActive(true);
            _missions.Add(mission);
            MissionAdded?.Invoke(title);
            RefreshLayout();
        }

        /// <summary>Appends a unique item, creating its mission if needed.</summary>
        public void AddItemToMission(string missionTitle, string itemName) {
            if (string.IsNullOrWhiteSpace(missionTitle) || string.IsNullOrWhiteSpace(itemName)) return;
            AddMission(missionTitle);
            Mission mission = FindMission(missionTitle);
            if (mission == null || mission.items.Contains(itemName)) return;

            RectTransform row = Instantiate(itemTemplate, mission.itemsView);
            row.GetChild(itemLabel.transform.GetSiblingIndex()).GetComponent<TMP_Text>().text = itemName;
            if (itemCompletedMark && itemCompletedMark.transform.parent == itemTemplate)
                row.GetChild(itemCompletedMark.transform.GetSiblingIndex()).gameObject.SetActive(false);
            row.gameObject.SetActive(true);
            mission.items.Add(itemName);
            mission.rows.Add(row);
            RefreshLayout();
        }

        public void SetItemCompleted(string missionTitle, string itemName, bool completed = true) {
            Mission mission = FindMission(missionTitle);
            if (mission == null) return;
            int index = mission.items.IndexOf(itemName);
            if (index < 0) return;
            RectTransform row = mission.rows[index];
            if (itemCompletedMark && itemCompletedMark.transform.parent == itemTemplate) {
                row.GetChild(itemCompletedMark.transform.GetSiblingIndex()).gameObject.SetActive(completed);
            } else {
                TMP_Text label = row.GetChild(itemLabel.transform.GetSiblingIndex()).GetComponent<TMP_Text>();
                if (completed) label.fontStyle |= FontStyles.Strikethrough;
                else label.fontStyle &= ~FontStyles.Strikethrough;
            }
        }

        private Mission FindMission(string title) {
            foreach (Mission mission in _missions) {
                if (!mission.titleView.gameObject.activeSelf) continue;
                if (mission.name == title) return mission;
            }
            return null;
        }

        private void RefreshLayout() {
            float offset = 0f;
            float titleHeight = titleTemplate.rect.height;
            float itemsTop = titleTemplate.anchoredPosition.y - itemsTemplate.anchoredPosition.y;
            foreach (Mission mission in _missions) {
                mission.titleView.anchoredPosition = titleTemplate.anchoredPosition + Vector2.down * offset;
                mission.itemsView.anchoredPosition = itemsTemplate.anchoredPosition + Vector2.down * offset;
                LayoutRebuilder.ForceRebuildLayoutImmediate(mission.itemsView);
                // Calculate from the fixed-height authored rows even when the panel is hidden.
                VerticalLayoutGroup layout = mission.itemsView.GetComponent<VerticalLayoutGroup>();
                float itemsHeight = mission.items.Count * itemTemplate.rect.height;
                if (layout) itemsHeight += layout.padding.vertical +
                    Mathf.Max(0, mission.items.Count - 1) * layout.spacing;
                offset += Mathf.Max(titleHeight, titleHeight * 0.5f + itemsTop + itemsHeight) + missionSpacing;
            }
        }
    }
}
