using UnityEngine;

namespace Framework.Interfaces
{
    public interface IDamageable
    {
        void TakeDamage(float damage);
        void Heal(float amount);
        float CurrentHealth { get; }
        float MaxHealth { get; }
    }

    public interface IInteractable
    {
        void Interact(GameObject interactor);
        string GetInteractionText();
        bool CanInteract(GameObject interactor);
    }

    public interface ISavable
    {
        string GetSaveData();
        void LoadSaveData(string data);
        string GetSaveKey();
    }

    public interface IKillable
    {
        void Die();
        bool IsDead { get; }
    }

    public interface IMovable
    {
        void Move(Vector3 direction);
        void Stop();
        bool IsMoving { get; }
    }
}
