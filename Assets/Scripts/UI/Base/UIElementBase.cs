using UnityEngine;

namespace UI
{
    /// <summary>
    /// UI元素基类
    /// </summary>
    public abstract class UIElementBase : Framework.Base.MonoBehaviourBase
    {
        [SerializeField] protected CanvasGroup canvasGroup;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        protected virtual void Initialize()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        public virtual void Show()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public virtual void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public virtual void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public virtual void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }
    }
}
