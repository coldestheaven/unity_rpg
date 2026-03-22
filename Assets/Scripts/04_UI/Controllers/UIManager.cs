using UnityEngine;
using System.Collections.Generic;

namespace UI.Controllers
{
    /// <summary>
    /// UI管理器 - 统一管理所有UI面板
    /// </summary>
    public class UIManager : Framework.Base.SingletonMonoBehaviour<UIManager>
    {
        [System.Serializable]
        public class UIPanelReference
        {
            public string panelName;
            public UIPanelBase panel;
        }

        [SerializeField] private UIPanelReference[] panels;

        private Dictionary<string, UIPanelBase> panelDictionary;

        protected override void Awake()
        {
            base.Awake();
            InitializePanels();
        }

        private void InitializePanels()
        {
            panelDictionary = new Dictionary<string, UIPanelBase>();

            foreach (var panelRef in panels)
            {
                if (panelRef.panel != null)
                {
                    panelDictionary[panelRef.panelName] = panelRef.panel;
                }
            }

            // Auto-find panels if not assigned
            UIPanelBase[] allPanels = FindObjectsOfType<UIPanelBase>();
            foreach (var panel in allPanels)
            {
                string panelName = panel.GetType().Name;
                if (!panelDictionary.ContainsKey(panelName))
                {
                    panelDictionary[panelName] = panel;
                }
            }
        }

        public void ShowPanel(string panelName)
        {
            if (panelDictionary.TryGetValue(panelName, out UIPanelBase panel))
            {
                panel.Show();
            }
            else
            {
                Debug.LogWarning($"Panel {panelName} not found");
            }
        }

        public void HidePanel(string panelName)
        {
            if (panelDictionary.TryGetValue(panelName, out UIPanelBase panel))
            {
                panel.Hide();
            }
        }

        public void TogglePanel(string panelName)
        {
            if (panelDictionary.TryGetValue(panelName, out UIPanelBase panel))
            {
                panel.Toggle();
            }
        }

        public bool IsPanelVisible(string panelName)
        {
            if (panelDictionary.TryGetValue(panelName, out UIPanelBase panel))
            {
                return panel.IsVisible;
            }
            return false;
        }

        public void HideAllPanels()
        {
            foreach (var panel in panelDictionary.Values)
            {
                panel.Hide();
            }
        }

        public T GetPanel<T>(string panelName) where T : UIPanelBase
        {
            if (panelDictionary.TryGetValue(panelName, out UIPanelBase panel))
            {
                return panel as T;
            }
            return null;
        }
    }
}
