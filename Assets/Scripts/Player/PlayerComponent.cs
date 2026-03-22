using UnityEngine;
using RPG.Core;

namespace RPG.Player
{
    /// <summary>
    /// 玩家组件基类 - 提供玩家系统的通用功能
    /// </summary>
    public abstract class PlayerComponent : MonoBehaviour, IDamageable
    {
        protected PlayerController Controller;
        protected PlayerHealth Health;
        protected PlayerState State;
        protected PlayerInput Input;

        protected virtual void Awake()
        {
            Controller = GetComponent<PlayerController>();
            Health = GetComponent<PlayerHealth>();
            State = GetComponent<PlayerState>();
            Input = GetComponent<PlayerInput>();
        }

        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void FixedUpdate() { }

        public virtual void TakeDamage(int damage, Vector2 attackerPosition)
        {
            Health?.TakeDamage(damage, attackerPosition);
        }
    }
}
