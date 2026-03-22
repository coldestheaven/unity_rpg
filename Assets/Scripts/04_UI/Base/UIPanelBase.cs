using UnityEngine;
using System.Collections;

namespace UI
{
    /// <summary>
    /// UI面板基类 - 支持动画
    /// </summary>
    public abstract class UIPanelBase : UIElementBase
    {
        [Header("Animation")]
        [SerializeField] protected float fadeInDuration = 0.3f;
        [SerializeField] protected float fadeOutDuration = 0.3f;

        protected Coroutine currentAnimation;

        public override void Show()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            currentAnimation = StartCoroutine(FadeInCoroutine());
        }

        public override void Hide()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            currentAnimation = StartCoroutine(FadeOutCoroutine());
        }

        protected IEnumerator FadeInCoroutine()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeInDuration);
                SetAlpha(alpha);
                yield return null;
            }

            SetAlpha(1f);

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            currentAnimation = null;
            OnShowComplete();
        }

        protected IEnumerator FadeOutCoroutine()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                SetAlpha(alpha);
                yield return null;
            }

            SetAlpha(0f);
            currentAnimation = null;
            OnHideComplete();
        }

        protected virtual void OnShowComplete()
        {
            // Override in derived classes
        }

        protected virtual void OnHideComplete()
        {
            // Override in derived classes
        }
    }
}
