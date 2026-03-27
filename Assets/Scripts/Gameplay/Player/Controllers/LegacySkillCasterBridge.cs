using UnityEngine;
using RPG.Skills;

namespace Gameplay.Player
{
    [RequireComponent(typeof(SkillController))]
    public class LegacySkillCasterBridge : Framework.Base.MonoBehaviourBase, IPlayerSkillCaster
    {
        [SerializeField] private SkillController skillController;
        [SerializeField] private float castLockDuration = 0.25f;

        private float castLockedUntil;

        public bool IsCastingSkill => Time.time < castLockedUntil;

        protected override void Awake()
        {
            base.Awake();
            skillController = GetComponent<SkillController>();
        }

        public bool CanUseSkill(int slotIndex)
        {
            return skillController != null && slotIndex >= 0 && slotIndex < skillController.SkillSlotCount;
        }

        public bool TryUseSkill(int slotIndex)
        {
            if (!CanUseSkill(slotIndex))
            {
                return false;
            }

            bool used = skillController.TryUseSkill(slotIndex);
            if (used)
            {
                castLockedUntil = Time.time + castLockDuration;
            }

            return used;
        }
    }
}
