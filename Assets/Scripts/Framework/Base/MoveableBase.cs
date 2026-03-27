using Framework.Interfaces;
using UnityEngine;

namespace Framework.Base
{
    public abstract class MoveableBase : MonoBehaviourBase, IMovable
    {
        [SerializeField] protected float moveSpeed = 5f;

        protected bool canMove = true;

        public virtual bool IsMoving { get; protected set; }
        public bool CanMove => canMove;
        public float MoveSpeed => moveSpeed;

        public abstract void Move(Vector3 direction);

        public abstract void Stop();

        public virtual void SetCanMove(bool value)
        {
            canMove = value;

            if (!canMove)
            {
                Stop();
            }
        }

        public virtual void SetMoveSpeed(float value)
        {
            moveSpeed = Mathf.Max(0f, value);
        }
    }
}
