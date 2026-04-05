using Framework.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerInteractionBridge : Framework.Base.MonoBehaviourBase, IPlayerInteractor
    {
        [Header("Interaction")]
        [SerializeField] private Transform interactionOrigin;
        [SerializeField] private float interactionRadius = 1.25f;
        [SerializeField] private LayerMask interactionLayers = ~0;
        [SerializeField] private float interactionLockDuration = 0.2f;

        private float interactingUntil;

        public bool IsInteracting => Time.time < interactingUntil;

        public bool CanInteract()
        {
            return FindBestInteractable() != null
                && RPG.Core.GameStateManager.Instance != null
                && RPG.Core.GameStateManager.Instance.CanInteract();
        }

        public bool TryInteract()
        {
            IInteractable interactable = FindBestInteractable();
            if (interactable == null)
            {
                return false;
            }

            interactable.Interact(gameObject);
            interactingUntil = Time.time + interactionLockDuration;
            return true;
        }

        private IInteractable FindBestInteractable()
        {
            Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, interactionRadius, interactionLayers);

            IInteractable bestInteractable = null;
            float bestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                MonoBehaviour[] behaviours = hit.GetComponents<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is IInteractable interactable && interactable.CanInteract(gameObject))
                    {
                        float distance = Vector2.Distance(origin, hit.transform.position);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestInteractable = interactable;
                        }
                    }
                }
            }

            return bestInteractable;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, interactionRadius);
        }
    }
}
