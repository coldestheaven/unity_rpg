using UnityEngine;
using RPG.Items;

namespace RPG.Items
{
    public class Consumable : MonoBehaviour
    {
        public ItemType itemType;
        public int effectAmount = 20;
        public float cooldown = 1f;

        private bool canUse = true;

        public void Use(GameObject player)
        {
            if (!canUse) return;

            switch (itemType)
            {
                case ItemType.HealthPotion:
                    Player.PlayerHealth playerHealth = player.GetComponent<Player.PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.Heal(effectAmount);
                    }
                    break;

                case ItemType.ManaPotion:
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

        public int GetTotalAttackPower(int baseAttack)
        {
            return baseAttack + bonusAttackPower;
        }

        public int GetTotalDefense(int baseDefense)
        {
            return baseDefense + bonusDefense;
        }
    }
}
