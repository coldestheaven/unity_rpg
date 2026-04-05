using UnityEngine;
using Framework.Events;

namespace RPG.Items
{
    /// <summary>
    /// 物品拾取控制器 - 重构版
    /// </summary>
    public class ItemPickup : MonoBehaviour
    {
        [Header("物品信息")]
        public ItemData itemData;
        public int quantity = 1;

        [Header("拾取设置")]
        public float pickupRange = 2f;
        public bool autoPickup = false;
        public float autoPickupDelay = 0.5f;
        public LayerMask playerLayer;

        [Header("动画设置")]
        public float bobSpeed = 2f;
        public float bobHeight = 0.2f;
        public float rotateSpeed = 90f;

        [Header("飞向玩家效果")]
        public bool flyToPlayer = false;
        public float flySpeed = 5f;
        public float flySmoothTime = 0.2f;

        private Vector3 startPosition;
        private float bobTimer;
        private bool isBeingCollected;
        private Transform playerTransform;
        private Vector3 velocity;
        private bool playerInRange;

        public ItemData ItemData => itemData;
        public int Quantity => quantity;

        private void Start()
        {
            startPosition = transform.position;
            CreateVisuals();
        }

        private void CreateVisuals()
        {
            if (itemData != null && itemData.icon != null)
            {
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
                spriteRenderer.sprite = itemData.icon;
            }
        }

        private void Update()
        {
            if (!isBeingCollected)
            {
                AnimateItem();
                CheckPlayerInRange();
                CheckAutoPickup();
            }
            else
            {
                FlyToPlayer();
            }
        }

        private void AnimateItem()
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float newY = startPosition.y + Mathf.Sin(bobTimer) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }

        private void CheckPlayerInRange()
        {
            Collider2D player = Physics2D.OverlapCircle(transform.position, pickupRange, playerLayer);
            playerInRange = player != null;
        }

        private void CheckAutoPickup()
        {
            if (autoPickup && playerInRange)
            {
                if (flyToPlayer)
                {
                    StartFlyToPlayer();
                }
                else
                {
                    Invoke(nameof(Pickup), autoPickupDelay);
                    autoPickup = false;
                }
            }
        }

        public void Pickup()
        {
            if (isBeingCollected) return;

            Collider2D player = Physics2D.OverlapCircle(transform.position, pickupRange, playerLayer);

            if (player != null)
            {
                InventorySystem inventory = player.GetComponent<InventorySystem>();

                if (inventory != null)
                {
                    InventoryOperationResult result = inventory.AddItem(itemData, quantity);

                    if (result == InventoryOperationResult.Success)
                    {
                        PlayPickupEffects(player.gameObject);
                        OnPickupSuccess(player.gameObject);
                    }
                    else
                    {
                        OnPickupFailed(result);
                    }
                }
            }
        }

        public void ManualPickup(GameObject player)
        {
            if (isBeingCollected) return;

            InventorySystem inventory = player.GetComponent<InventorySystem>();

            if (inventory != null)
            {
                InventoryOperationResult result = inventory.AddItem(itemData, quantity);

                if (result == InventoryOperationResult.Success)
                {
                    PlayPickupEffects(player.gameObject);
                    OnPickupSuccess(player.gameObject);
                }
                else
                {
                    OnPickupFailed(result);
                }
            }
        }

        private void StartFlyToPlayer()
        {
            Collider2D player = Physics2D.OverlapCircle(transform.position, pickupRange, playerLayer);

            if (player != null)
            {
                playerTransform = player.transform;
                isBeingCollected = true;

                // 停止动画,开始飞向玩家
                transform.rotation = Quaternion.identity;
            }
        }

        private void FlyToPlayer()
        {
            if (playerTransform == null) return;

            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance < 0.5f)
            {
                ManualPickup(playerTransform.gameObject);
            }
            else
            {
                Vector3 targetPosition = playerTransform.position;
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, flySmoothTime, flySpeed);
            }
        }

        private void PlayPickupEffects(GameObject player)
        {
            itemData?.OnPickup(player);

            if (itemData?.pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(itemData.pickupSound, transform.position);
            }
        }

        private void OnPickupSuccess(GameObject player)
        {
            Framework.Events.EventBus.Publish(new Framework.Events.ItemPickedUpEvent(
                itemData?.name, itemData?.itemName, quantity, transform.position));

            Destroy(gameObject);
        }

        private void OnPickupFailed(InventoryOperationResult result)
        {
            Debug.LogWarning($"Failed to pickup {itemData?.itemName}: {result}");

            Framework.Events.EventBus.Publish(new Framework.Events.ItemPickupFailedEvent(
                itemData?.name, itemData?.itemName, result.ToString()));
        }

        public void SetItem(ItemData item, int qty = 1)
        {
            itemData = item;
            quantity = qty;
            CreateVisuals();
        }

        public void SetAutoPickup(bool enabled)
        {
            autoPickup = enabled;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }

        private void OnMouseDown()
        {
            if (playerInRange)
            {
                Pickup();
            }
        }
    }

}
