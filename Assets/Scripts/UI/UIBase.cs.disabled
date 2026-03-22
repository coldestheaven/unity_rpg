using UnityEngine;
using RPG.Core;

namespace RPG.UI
{
    /// <summary>
    /// UI面板基类
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        [Header("面板设置")]
        public bool isAnimated = true;
        public float fadeDuration = 0.3f;
        public CanvasGroup canvasGroup;

        protected bool isVisible;

        public bool IsVisible => isVisible;

        public event System.Action OnPanelShown;
        public event System.Action OnPanelHidden;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        protected virtual void Start()
        {
            HidePanel(false);
        }

        /// <summary>
        /// 显示面板
        /// </summary>
        public virtual void ShowPanel(bool animate = true)
        {
            if (isVisible) return;

            gameObject.SetActive(true);
            isVisible = true;

            if (animate && isAnimated)
            {
                StartCoroutine(AnimateFadeIn());
            }
            else
            {
                SetPanelAlpha(1f);
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            OnPanelShown?.Invoke();
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public virtual void HidePanel(bool animate = true)
        {
            if (!isVisible) return;

            if (animate && isAnimated)
            {
                StartCoroutine(AnimateFadeOut());
            }
            else
            {
                SetPanelAlpha(0f);
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
                isVisible = false;
            }

            OnPanelHidden?.Invoke();
        }

        /// <summary>
        /// 切换面板显示状态
        /// </summary>
        public void TogglePanel()
        {
            if (isVisible)
            {
                HidePanel();
            }
            else
            {
                ShowPanel();
            }
        }

        private System.Collections.IEnumerator AnimateFadeIn()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                SetPanelAlpha(alpha);
                yield return null;
            }

            SetPanelAlpha(1f);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private System.Collections.IEnumerator AnimateFadeOut()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                SetPanelAlpha(alpha);
                yield return null;
            }

            SetPanelAlpha(0f);
            gameObject.SetActive(false);
            isVisible = false;
        }

        private void SetPanelAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }

        protected virtual void OnDestroy()
        {
            StopAllCoroutines();
        }
    }

    /// <summary>
    /// UI元素基类
    /// </summary>
    public abstract class UIElement : MonoBehaviour
    {
        protected virtual void Awake() { }
        protected virtual void Start() { }

        public virtual void Initialize() { }
        public virtual void UpdateUI() { }

        protected virtual void OnDestroy() { }
    }
}
