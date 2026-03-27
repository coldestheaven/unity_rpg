using System;
using System.Collections.Generic;
using Core.Stats;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerBuffController : Framework.Base.MonoBehaviourBase, IPlayerStatModifierSource
    {
        [Serializable]
        private class ActiveBuff
        {
            public string SourceName;
            public PlayerStatBlock Modifier;
            public float RemainingDuration;
        }

        private readonly List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

        public event Action ModifiersChanged;

        protected override void Update()
        {
            base.Update();

            bool changed = false;
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                activeBuffs[i].RemainingDuration -= Time.deltaTime;
                if (activeBuffs[i].RemainingDuration <= 0f)
                {
                    activeBuffs.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                ModifiersChanged?.Invoke();
            }
        }

        public void ApplyBuff(string sourceName, PlayerStatBlock modifier, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            activeBuffs.Add(new ActiveBuff
            {
                SourceName = sourceName,
                Modifier = modifier,
                RemainingDuration = duration
            });

            ModifiersChanged?.Invoke();
        }

        public void ClearBuffs()
        {
            if (activeBuffs.Count == 0)
            {
                return;
            }

            activeBuffs.Clear();
            ModifiersChanged?.Invoke();
        }

        public void ApplyModifiers(ref PlayerStatBlock stats)
        {
            foreach (var buff in activeBuffs)
            {
                stats.Add(
                    buff.Modifier.MaxHealth,
                    buff.Modifier.AttackDamage,
                    buff.Modifier.Defense,
                    buff.Modifier.MoveSpeed);
            }
        }
    }
}
