using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// 向后兼容别名 — 实现委托给 <see cref="Framework.Core.Patterns.Singleton{T}"/>。
    /// 新代码请直接继承 <see cref="Framework.Core.Patterns.Singleton{T}"/> 或
    /// <see cref="Framework.Base.SingletonMonoBehaviour{T}"/>。
    /// </summary>
    public abstract class Singleton<T> : Framework.Core.Patterns.Singleton<T>
        where T : MonoBehaviour
    {
    }
}
