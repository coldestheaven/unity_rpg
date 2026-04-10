using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Framework.Events;
using Framework.Interfaces;
using RPG.Data;
using RPG.Items;
using RPG.Building;
using Gameplay.Player;

namespace RPG.Core
{
    // ──────────────────────────────────────────────────────────────────────────
    // SaveSystem
    //
    // Responsibilities:
    //   • Own the ISaveDAO instance (currently JsonSaveDAO).
    //   • Orchestrate what to save and load (assemble / apply DTOs).
    //   • Fire EventBus events after each operation.
    //
    // What SaveSystem does NOT do:
    //   • File I/O — delegated to ISaveDAO.
    //   • Data serialisation format — delegated to ISaveDAO implementation.
    //   • Deciding which data domains exist — each domain has its own DTO.
    //
    // To swap the storage backend (e.g. PlayerPrefs, cloud):
    //   Replace the JsonSaveDAO constructor call in Awake with a different ISaveDAO.
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Orchestrates save / load operations through the <see cref="ISaveDAO"/> contract.
    ///
    /// Business logic only — no file I/O directly.  The DAO implementation
    /// (<see cref="JsonSaveDAO"/>) handles all serialisation and file-system access.
    /// </summary>
    public class SaveSystem : Singleton<SaveSystem>
    {
        // ── Well-known slot names ─────────────────────────────────────────────
        public const string QuickSaveSlot = "QuickSave";
        public const string AutoSaveSlot  = "AutoSave";

        // ── DAO (injected in Awake; replaceable for testing) ──────────────────
        public ISaveDAO DAO { get; private set; }

        // ── Backward-compat: list of slot name strings (derived from DAO) ─────
        public IReadOnlyList<string> SaveSlotNames
        {
            get
            {
                var slots = DAO?.GetAllSlots();
                if (slots == null) return Array.Empty<string>();
                var names = new string[slots.Count];
                for (int i = 0; i < slots.Count; i++) names[i] = slots[i].slotName;
                return names;
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            string saveDir = Path.Combine(Application.persistentDataPath, "Saves");
            DAO = new JsonSaveDAO(saveDir);
            Debug.Log($"[SaveSystem] Save directory: {saveDir}");
        }

        // ── Save ──────────────────────────────────────────────────────────────

        /// <summary>Saves the current game state to a named slot.</summary>
        public void SaveGame(string slotName = null)
        {
            slotName = NormaliseSlotName(slotName);

            try
            {
                WriteSlot(slotName);
                EventBus.Publish(new GameSavedEvent(
                    0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                Debug.Log($"[SaveSystem] Saved to slot '{slotName}'.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed ({slotName}): {e}");
            }
        }

        public void QuickSave() => SaveGame(QuickSaveSlot);
        public void AutoSave()  => SaveGame(AutoSaveSlot);

        // ── Load ──────────────────────────────────────────────────────────────

        /// <summary>Loads a previously saved slot and applies data to live systems.</summary>
        public void LoadGame(string slotName = null)
        {
            slotName = NormaliseSlotName(slotName, QuickSaveSlot);
            if (!DAO.SlotExists(slotName))
            {
                Debug.LogWarning($"[SaveSystem] Slot not found: '{slotName}'.");
                return;
            }

            try
            {
                ApplySlot(slotName);
                EventBus.Publish(new GameLoadedEvent(0));
                Debug.Log($"[SaveSystem] Loaded slot '{slotName}'.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed ({slotName}): {e}");
            }
        }

        public void QuickLoad() => LoadGame(QuickSaveSlot);

        // ── Delete ────────────────────────────────────────────────────────────

        public void DeleteSave(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName)) return;
            DAO.DeleteSlot(slotName);
            EventBus.Publish(new SaveDeletedEvent(0));
            Debug.Log($"[SaveSystem] Deleted slot '{slotName}'.");
        }

        // ── Query ─────────────────────────────────────────────────────────────

        public bool                      SaveExists(string slotName) => DAO.SlotExists(slotName);
        public IReadOnlyList<SaveSlotInfo> GetAllSlots()             => DAO.GetAllSlots();

        // ── Internal: assemble DTOs and write ────────────────────────────────

        private void WriteSlot(string slotName)
        {
            // Meta — written first so slot list is always valid even if later writes fail.
            var pm = PlayerProgressManager.Instance;
            DAO.Write(slotName, SaveKeys.Meta, new SaveSlotInfo
            {
                slotName         = slotName,
                saveTimestampUtc = DateTime.UtcNow.Ticks,
                playerLevel      = pm?.GetLevel() ?? 1,
                sceneName        = SceneManager.GetActiveScene().name,
                gameVersion      = Application.version
            });

            // Progress
            if (pm != null)
            {
                DAO.Write(slotName, SaveKeys.Progress, new PlayerProgressDTO
                {
                    level                 = pm.GetLevel(),
                    experience            = pm.GetExperience(),
                    experienceToNextLevel = pm.GetExperienceToNextLevel(),
                    gold                  = pm.GetGold()
                });
            }

            // Stats
            var pc = PlayerController.Instance;
            if (pc != null)
            {
                DAO.Write(slotName, SaveKeys.Stats, new PlayerStatsDTO
                {
                    maxHealth     = pc.Health.MaxHealth,
                    currentHealth = pc.Health.CurrentHealth,
                    attackPower   = pc.AttackDamage,
                    defense       = pc.Defense,
                    moveSpeed     = pc.MoveSpeed
                });

                DAO.Write(slotName, SaveKeys.Position, new PlayerPositionDTO
                {
                    sceneName = SceneManager.GetActiveScene().name,
                    posX      = pc.transform.position.x,
                    posY      = pc.transform.position.y,
                    posZ      = pc.transform.position.z
                });
            }

            // Inventory
            WriteInventorySlot(slotName);

            // Equipment
            WriteEquipmentSlot(slotName);

            // Buildings
            WriteBuildingsSlot(slotName);
        }

        private void WriteInventorySlot(string slotName)
        {
            var itemSys = ItemSystem.Instance;
            if (itemSys?.inventory == null) return;

            var inv    = itemSys.inventory;
            var raw    = inv.GetAllSlots();
            var dtos   = new InventorySlotDTO[raw.Count];
            for (int i = 0; i < raw.Count; i++)
            {
                var s = raw[i];
                dtos[i] = new InventorySlotDTO
                {
                    itemId   = s.IsEmpty ? "" : ResolveItemId(s.itemData),
                    quantity = s.IsEmpty ? 0  : s.quantity,
                };
            }
            DAO.Write(slotName, SaveKeys.Inventory, new InventoryDTO
            {
                gold  = (int)inv.Gold,
                slots = dtos,
            });
        }

        private void WriteEquipmentSlot(string slotName)
        {
            var itemSys = ItemSystem.Instance;
            if (itemSys?.equipment == null) return;

            var equipped = itemSys.equipment.GetAllEquippedItems();
            var dtos     = new List<EquipmentSlotDTO>(equipped.Count);
            foreach (var kvp in equipped)
            {
                if (kvp.Value == null) continue;
                dtos.Add(new EquipmentSlotDTO
                {
                    slotName = kvp.Key.ToString(),
                    itemId   = ResolveItemId(kvp.Value),
                });
            }
            DAO.Write(slotName, SaveKeys.Equipment, new EquipmentDTO { slots = dtos.ToArray() });
        }

        private void WriteBuildingsSlot(string slotName)
        {
            var bs = BuildingSystem.Instance;
            if (bs == null) return;

            var placed = bs.GetAllBuildings();
            var dtos   = new PlacedBuildingDTO[placed.Length];
            for (int i = 0; i < placed.Length; i++)
                dtos[i] = placed[i].ToDTO();

            DAO.Write(slotName, SaveKeys.Buildings, new BuildingsSaveDTO { buildings = dtos });
        }

        private static string ResolveItemId(Items.ItemData item)
        {
            if (item == null) return "";
            return !string.IsNullOrEmpty(item.itemId) ? item.itemId : item.name;
        }

        // ── Internal: read DTOs and apply to live systems ─────────────────────

        private void ApplySlot(string slotName)
        {
            // Progress
            if (DAO.TryRead<PlayerProgressDTO>(slotName, SaveKeys.Progress, out var prog))
            {
                var pm = PlayerProgressManager.Instance;
                if (pm?.Progress != null)
                {
                    pm.Progress.level                 = prog.level;
                    pm.Progress.experience            = prog.experience;
                    pm.Progress.experienceToNextLevel = prog.experienceToNextLevel;
                    pm.Progress.gold                  = prog.gold;
                    pm.NotifyProgressChanged();

                    // Sync authoritative state to the logic-thread simulation.
                    RPG.Simulation.GameSimulation.Instance?.Progress.RestoreState(
                        prog.level, prog.experience, prog.experienceToNextLevel, prog.gold);
                }
            }

            // Stats
            var pc = PlayerController.Instance;
            if (pc != null && DAO.TryRead<PlayerStatsDTO>(slotName, SaveKeys.Stats, out var stats))
            {
                pc.SetMaxHealth(stats.maxHealth);
                pc.Health.Revive(stats.currentHealth);
                pc.SetAttackDamage(stats.attackPower);
                pc.SetDefense(stats.defense);
                pc.SetMoveSpeed(stats.moveSpeed);
            }

            // Inventory
            ApplyInventorySlot(slotName);

            // Equipment (must run after inventory to avoid double-removing items)
            ApplyEquipmentSlot(slotName);

            // Buildings
            ApplyBuildingsSlot(slotName);

            // Scene + position
            // 修复：LoadScene 为异步操作，旧代码在 LoadScene 调用后立即设置坐标，
            // 此时新场景尚未完成，玩家对象不存在或位于旧场景，坐标会被丢弃。
            // 现在改为通过协程等待场景加载完毕后再还原坐标。
            if (DAO.TryRead<PlayerPositionDTO>(slotName, SaveKeys.Position, out var pos))
            {
                bool needsSceneChange = !string.IsNullOrEmpty(pos.sceneName) &&
                                        pos.sceneName != SceneManager.GetActiveScene().name;
                var targetPos = new Vector3(pos.posX, pos.posY, pos.posZ);

                if (needsSceneChange)
                    Instance.StartCoroutine(LoadSceneAndRestorePosition(pos.sceneName, targetPos));
                else
                    ApplyPlayerPosition(targetPos);
            }
        }

        // ── Apply helpers ────────────────────────────────────────────────────

        private void ApplyInventorySlot(string slotName)
        {
            var itemSys = ItemSystem.Instance;
            if (itemSys?.inventory == null) return;
            if (!DAO.TryRead<InventoryDTO>(slotName, SaveKeys.Inventory, out var dto)) return;

            itemSys.inventory.ClearInventory();
            if (dto.gold > 0) itemSys.inventory.AddGold(dto.gold);

            var db = itemSys.itemDatabase;
            if (dto.slots == null || db == null) return;

            foreach (var s in dto.slots)
            {
                if (string.IsNullOrEmpty(s.itemId) || s.quantity <= 0) continue;
                var item = db.GetItem(s.itemId);
                if (item != null)
                    itemSys.inventory.AddItem(item, s.quantity);
                else
                    Debug.LogWarning($"[SaveSystem] 背包恢复：找不到 itemId='{s.itemId}'，跳过。");
            }
        }

        private void ApplyEquipmentSlot(string slotName)
        {
            var itemSys = ItemSystem.Instance;
            if (itemSys?.equipment == null) return;
            if (!DAO.TryRead<EquipmentDTO>(slotName, SaveKeys.Equipment, out var dto)) return;

            itemSys.equipment.ClearAllEquipment();

            var db = itemSys.itemDatabase;
            if (dto.slots == null || db == null) return;

            foreach (var s in dto.slots)
            {
                if (string.IsNullOrEmpty(s.itemId)) continue;
                var item = db.GetItem(s.itemId) as Items.EquipmentData;
                if (item != null)
                    itemSys.equipment.EquipItem(item);
                else
                    Debug.LogWarning($"[SaveSystem] 装备恢复：找不到 itemId='{s.itemId}'，跳过。");
            }
        }

        private void ApplyBuildingsSlot(string slotName)
        {
            var bs = BuildingSystem.Instance;
            if (bs == null) return;
            if (!DAO.TryRead<BuildingsSaveDTO>(slotName, SaveKeys.Buildings, out var dto)) return;

            bs.ClearAllBuildings();
            bs.RestoreFromDTOs(dto.buildings ?? Array.Empty<PlacedBuildingDTO>());
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// 异步加载目标场景，等待激活完毕后再还原玩家坐标。
        /// 必须在 MonoBehaviour 上通过 StartCoroutine 调用。
        /// </summary>
        private static IEnumerator LoadSceneAndRestorePosition(string sceneName, Vector3 position)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = true;
            yield return op;   // 等待场景完全加载并激活
            ApplyPlayerPosition(position);
        }

        private static void ApplyPlayerPosition(Vector3 position)
        {
            var pc = PlayerController.Instance;
            if (pc != null)
                pc.transform.position = position;
            else
                Debug.LogWarning("[SaveSystem] 场景加载完毕但 PlayerController 未找到，无法还原坐标。");
        }

        private static string NormaliseSlotName(string name, string fallback = null)
        {
            if (!string.IsNullOrWhiteSpace(name)) return name;
            if (!string.IsNullOrWhiteSpace(fallback)) return fallback;
            return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }
    }
}
