using UnityEngine;
using Core.Stats;
using Framework.Events;

namespace RPG.Items
{
    /// <summary>
    /// 物品类型枚举
    /// </summary>
    public enum ItemType
    {
        Consumable,      // 消耗品
        Weapon,          // 武器
        Armor,           // 护甲
        Accessory,       // 饰品
        QuestItem,       // 任务物品
        Material,        // 材料
        Currency         // 货币
    }

    /// <summary>
    /// 装备类型
    /// </summary>
    public enum EquipmentSlot
    {
        MainHand,
        OffHand,
        Head,
        Chest,
        Legs,
        Feet,
        Ring,
        Amulet
    }

    /// <summary>
    /// 物品基类数据 - 使用ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "RPG/Items/Basic Item")]
    public class ItemData : ScriptableObject
    {
        [Header("基本信息")]
        public string itemName;
        [TextArea]
        public string description;
        public Sprite icon;
        public ItemType itemType;

        [Header("属性")]
        public int value = 1;
        public int maxStackSize = 99;
        public bool isSellable = true;
        public bool isDroppable = true;

        [Header("视觉效果")]
        public GameObject pickupEffect;
        public GameObject dropEffect;
        public AudioClip pickupSound;
        public AudioClip dropSound;

        [Header("标签")]
        public string[] tags;

        public virtual void Use(GameObject user)
        {
            Debug.Log($"Used item: {itemName}");
        }

        public virtual bool CanStack()
        {
            return maxStackSize > 1;
        }

        public virtual void OnPickup(GameObject player)
        {
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, player.transform.position, Quaternion.identity);
            }
        }

        public virtual void OnDrop(Vector3 position)
        {
            if (dropEffect != null)
            {
                Instantiate(dropEffect, position, Quaternion.identity);
            }
        }

        public bool HasTag(string tag)
        {
            if (tags == null) return false;
            return System.Array.Exists(tags, t => t.Equals(tag, System.StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// 消耗品数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Consumable", menuName = "RPG/Items/Consumable")]
    public class ConsumableData : ItemData
    {
        [Header("消耗品属性")]
        public ConsumableType consumableType;
        public int healAmount;
        public float healPercentage;
        public int manaRestore;
        public float manaRestorePercentage;

        [Header("冷却")]
        public float cooldown = 0f;

        [Header("Buff 属性")]
        public float buffDuration = 5f;
        public int buffAttackPower;
        public int buffDefense;
        public int buffHealth;
        public float buffMoveSpeed;

        public enum ConsumableType
        {
            HealthPotion,
            ManaPotion,
            StaminaPotion,
            Buff,
            Food
        }

        public override void Use(GameObject user)
        {
            switch (consumableType)
            {
                case ConsumableType.HealthPotion:
                    HealPlayer(user);
                    break;
                case ConsumableType.ManaPotion:
                    RestoreMana(user);
                    break;
                case ConsumableType.Buff:
                    ApplyBuff(user);
                    break;
                default:
                    Debug.Log($"Consumed: {itemName}");
                    break;
            }
        }

        private void HealPlayer(GameObject user)
        {
            var health = user.GetComponent<Gameplay.Player.PlayerHealth>();
            if (health != null)
            {
                float totalHeal = healAmount + (health.MaxHealth * healPercentage);
                health.Heal(totalHeal);

                EventManager.Instance?.TriggerEvent("ItemUsed", new ItemUsedEventArgs
                {
                    itemName = itemName,
                    itemType = itemType,
                    value = Mathf.RoundToInt(totalHeal)
                });
            }
        }

        private void RestoreMana(GameObject user)
        {
            // TODO: 实现法力系统
            Debug.Log($"Restored {manaRestore} mana");
        }

        private void ApplyBuff(GameObject user)
        {
            var buffController = user.GetComponent<Gameplay.Player.PlayerBuffController>();
            if (buffController == null)
            {
                return;
            }

            buffController.ApplyBuff(
                itemName,
                new PlayerStatBlock(
                    buffHealth,
                    buffAttackPower,
                    buffDefense,
                    buffMoveSpeed),
                buffDuration);

            EventManager.Instance?.TriggerEvent("ItemUsed", new ItemUsedEventArgs
            {
                itemName = itemName,
                itemType = itemType,
                value = Mathf.RoundToInt(buffDuration)
            });
        }
    }

    /// <summary>
    /// 装备数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment", menuName = "RPG/Items/Equipment")]
    public class EquipmentData : ItemData
    {
        [Header("装备属性")]
        public EquipmentSlot equipmentSlot;
        public int attackPowerBonus;
        public int defenseBonus;
        public int healthBonus;
        public int manaBonus;
        public float moveSpeedBonus;

        [Header("特殊属性")]
        public EquipmentEffect[] effects;

        [System.Serializable]
        public class EquipmentEffect
        {
            public string effectName;
            public float effectValue;
            public string description;
        }

        public override void Use(GameObject user)
        {
            var equipmentSystem = user.GetComponent<EquipmentSystem>();
            if (equipmentSystem != null)
            {
                equipmentSystem.EquipItem(this);
            }
        }
    }

    /// <summary>
    /// 武器数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "RPG/Items/Weapon")]
    public class WeaponData : EquipmentData
    {
        [Header("武器属性")]
        public int baseDamage;
        public float attackSpeed;
        public float attackRange;
        public DamageType damageType;

        public enum DamageType
        {
            Physical,
            Magic,
            Fire,
            Ice,
            Lightning,
            Poison
        }

        public int GetDamage()
        {
            return baseDamage + attackPowerBonus;
        }
    }

    /// <summary>
    /// 护甲数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Armor", menuName = "RPG/Items/Armor")]
    public class ArmorData : EquipmentData
    {
        [Header("护甲属性")]
        public int baseDefense;
        public float damageReductionPercentage;

        public int GetDefense()
        {
            return baseDefense + defenseBonus;
        }

        public float GetDamageReduction()
        {
            return damageReductionPercentage;
        }
    }

    /// <summary>
    /// 任务物品数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Quest Item", menuName = "RPG/Items/Quest Item")]
    public class QuestItemData : ItemData
    {
        [Header("任务物品")]
        public string questId;
        public bool isObjective;
        public bool autoCollect = true;

        public override bool CanStack()
        {
            return false;
        }

        public override void OnPickup(GameObject player)
        {
            base.OnPickup(player);
            EventManager.Instance?.TriggerEvent("QuestItemCollected", new QuestItemEventArgs
            {
                itemName = itemName,
                questId = questId
            });
        }
    }
}
