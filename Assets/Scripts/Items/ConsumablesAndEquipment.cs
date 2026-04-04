using UnityEngine;

namespace RPG.Items
{
    public enum ConsumableItemType
    {
        HealthPotion,
        ManaPotion
    }

    public class Consumable : MonoBehaviour
    {
        public ConsumableItemType itemType;
        public int effectAmount = 20;
        public float cooldown = 1f;

        private bool canUse = true;

        public void Use(GameObject player)
        {
            if (!canUse) return;

            switch (itemType)
            {
                case ConsumableItemType.HealthPotion:
                    var playerHealth = player.GetComponent<Gameplay.Player.PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.Heal(effectAmount);
                    }
                    break;

                case ConsumableItemType.ManaPotion:
                    Debug.Log("恢复魔法值: " + effectAmount);
                    break;
            }

            canUse = false;
            Invoke(nameof(ResetCooldown), cooldown);
        }

        private void ResetCooldown()
        {
            canUse = true;
        }
    }

    [System.Serializable]
    public class Weapon
    {
        public string weaponName;
        public int damage;
        public float attackSpeed;
        public Sprite icon;
        public GameObject weaponModel;
    }

    public class Equipment : MonoBehaviour
    {
        [Header("装备槽位")]
        public Weapon equippedWeapon;

        [Header("装备效果")]
        public int bonusAttackPower = 0;
        public int bonusDefense = 0;
        public float bonusMoveSpeed = 0f;

        public void EquipWeapon(Weapon weapon)
        {
            if (weapon == null) return;

            equippedWeapon = weapon;
            bonusAttackPower = weapon.damage;

            Debug.Log($"装备了武器: {weapon.weaponName}");
        }

        public void UnequipWeapon()
        {
            bonusAttackPower = 0;
            equippedWeapon = null;

            Debug.Log("卸下了武器");
        }

        public int GetTotalAttackPower(int baseAttack) => baseAttack + bonusAttackPower;
        public int GetTotalDefense(int baseDefense) => baseDefense + bonusDefense;
    }
}
