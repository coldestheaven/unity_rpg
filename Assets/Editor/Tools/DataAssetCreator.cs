using System.Collections.Generic;
using System.IO;
using RPG.Buff;
using RPG.Data;
using RPG.Items;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 数据资产创建工具 — 一键生成玩家、装备、物品的 ScriptableObject 资产。
///
/// 菜单: RPG → 创建示例数据资产
///
/// 生成结构:
///   Assets/Data/
///     Player/
///       PlayerData_Default.asset
///     Items/
///       Consumables/
///         Potion_HP_Small.asset  … (5种)
///         Potion_MP_Small.asset  … (2种)
///         Potion_Strength.asset
///       Equipment/
///         Weapons/
///           Weapon_IronSword.asset
///           Weapon_Dagger.asset
///           Weapon_MagicStaff.asset
///           Weapon_FireStaff.asset
///         Armor/
///           Armor_LeatherChest.asset
///           Armor_IronChest.asset
///           Armor_LeatherHelmet.asset
///           Armor_IronHelmet.asset
///           Armor_ClothBoots.asset
///           Armor_IronBoots.asset
///     Databases/
///       ItemDatabase.asset
///       SkillDatabase.asset
///       EnemyDatabase.asset
///       BuffDatabase.asset
///       GameDataService.asset
/// </summary>
public static class DataAssetCreator
{
    private const string RootDir   = "Assets/Data";
    private const string PlayerDir = "Assets/Data/Player";
    private const string ConsumDir = "Assets/Data/Items/Consumables";
    private const string WeaponDir = "Assets/Data/Items/Equipment/Weapons";
    private const string ArmorDir  = "Assets/Data/Items/Equipment/Armor";
    private const string DbDir     = "Assets/Data/Databases";

    // ── Menu entries ──────────────────────────────────────────────────────────

    [MenuItem("RPG/创建示例数据资产")]
    public static void CreateAll()
    {
        EnsureDirectories();

        var items = new List<(string id, ItemData data)>();

        var player = CreatePlayerData();
        var consumables = CreateConsumables();
        var weapons     = CreateWeapons();
        var armors      = CreateArmors();

        items.AddRange(consumables);
        items.AddRange(weapons);
        items.AddRange(armors);

        // Wire starting inventory into player data
        var startingItems = new StartingItem[3];
        startingItems[0] = new StartingItem { item = consumables[0].data, quantity = 3 };
        startingItems[1] = new StartingItem { item = consumables[1].data, quantity = 1 };
        startingItems[2] = new StartingItem { item = consumables[5].data, quantity = 2 };

        using var playerSO = new SerializedObject(player);
        var startProp = playerSO.FindProperty("startingInventory");
        startProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            startProp.GetArrayElementAtIndex(i).FindPropertyRelative("item")
                     .objectReferenceValue = startingItems[i].item;
            startProp.GetArrayElementAtIndex(i).FindPropertyRelative("quantity")
                     .intValue = startingItems[i].quantity;
        }
        playerSO.FindProperty("startingWeapon").objectReferenceValue = weapons[0].data;
        playerSO.FindProperty("startingChest") .objectReferenceValue = armors[0].data;
        playerSO.FindProperty("startingHead")  .objectReferenceValue = armors[2].data;
        playerSO.ApplyModifiedProperties();

        // Item database
        var itemDb = CreateOrLoad<ItemDatabase>(DbDir, "ItemDatabase");
        PopulateItemDatabase(itemDb, items);

        // Remaining databases (empty stubs, user fills with their assets)
        CreateOrLoad<RPG.Data.SkillDatabase>(DbDir, "SkillDatabase");
        CreateOrLoad<RPG.Data.EnemyDatabase>(DbDir, "EnemyDatabase");
        CreateOrLoad<BuffDatabase>(DbDir, "BuffDatabase");

        // GameDataService
        var svc = CreateOrLoad<GameDataService>(DbDir, "GameDataService");
        using var svcSO = new SerializedObject(svc);
        svcSO.FindProperty("_playerData")      .objectReferenceValue = player;
        svcSO.FindProperty("_itemDatabase")    .objectReferenceValue = itemDb;
        svcSO.FindProperty("_skillDatabase")   .objectReferenceValue = AssetDatabase.LoadAssetAtPath<RPG.Data.SkillDatabase>($"{DbDir}/SkillDatabase.asset");
        svcSO.FindProperty("_enemyDatabase")   .objectReferenceValue = AssetDatabase.LoadAssetAtPath<RPG.Data.EnemyDatabase>($"{DbDir}/EnemyDatabase.asset");
        svcSO.FindProperty("_buffDatabase")    .objectReferenceValue = AssetDatabase.LoadAssetAtPath<BuffDatabase>($"{DbDir}/BuffDatabase.asset");
        svcSO.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "数据资产创建完成",
            $"已成功创建 {items.Count + 1} 个资产（含 PlayerData）。\n\n" +
            $"位置: {RootDir}\n\n" +
            "请打开 GameDataService 检查数据库绑定，" +
            "并在场景的 GameDataLoader 组件中赋值。",
            "好的");

        Selection.activeObject = svc;
        EditorGUIUtility.PingObject(svc);
    }

    // ── Player ────────────────────────────────────────────────────────────────

    private static PlayerData CreatePlayerData()
    {
        var pd = CreateOrLoad<PlayerData>(PlayerDir, "PlayerData_Default");
        using var so = new SerializedObject(pd);
        so.FindProperty("displayName")       .stringValue = "英雄";
        so.FindProperty("description")       .stringValue = "来自远方的冒险者。";
        so.FindProperty("baseMaxHealth")     .floatValue  = 100f;
        so.FindProperty("baseAttackDamage")  .floatValue  = 12f;
        so.FindProperty("baseDefense")       .floatValue  = 5f;
        so.FindProperty("baseMoveSpeed")     .floatValue  = 4f;
        so.FindProperty("baseMana")          .floatValue  = 80f;
        so.FindProperty("manaRegen")         .floatValue  = 3f;
        so.FindProperty("healthPerLevel")    .floatValue  = 18f;
        so.FindProperty("attackPerLevel")    .floatValue  = 2.5f;
        so.FindProperty("defensePerLevel")   .floatValue  = 1.2f;
        so.FindProperty("manaPerLevel")      .floatValue  = 8f;
        so.FindProperty("baseXpToLevel2")    .floatValue  = 100f;
        so.FindProperty("xpGrowthFactor")    .floatValue  = 1.45f;
        so.FindProperty("maxLevel")          .intValue    = 50;
        so.FindProperty("startingGold")      .intValue    = 50;
        so.FindProperty("startingLevel")     .intValue    = 1;
        so.ApplyModifiedProperties();
        return pd;
    }

    // ── Consumables ───────────────────────────────────────────────────────────

    private static List<(string id, ItemData data)> CreateConsumables()
    {
        var list = new List<(string, ItemData)>();

        list.Add(MakeConsumable("item_potion_hp_s",
            "小型血瓶", "恢复 30 点生命值。",
            ConsumableData.ConsumableType.HealthPotion,
            healAmount: 30, value: 20, maxStack: 10));

        list.Add(MakeConsumable("item_potion_hp_m",
            "中型血瓶", "恢复 80 点生命值。",
            ConsumableData.ConsumableType.HealthPotion,
            healAmount: 80, value: 60, maxStack: 5));

        list.Add(MakeConsumable("item_potion_hp_l",
            "大型血瓶", "恢复 200 点生命值。",
            ConsumableData.ConsumableType.HealthPotion,
            healAmount: 200, value: 150, maxStack: 3));

        list.Add(MakeConsumable("item_potion_hp_full",
            "生命圣水", "全量恢复生命值。",
            ConsumableData.ConsumableType.HealthPotion,
            healAmount: 0, healPct: 1f, value: 500, maxStack: 1));

        list.Add(MakeConsumable("item_potion_mp_s",
            "小型蓝瓶", "恢复 30 点法力值。",
            ConsumableData.ConsumableType.ManaPotion,
            manaRestore: 30, value: 25, maxStack: 10));

        list.Add(MakeConsumable("item_potion_mp_m",
            "中型蓝瓶", "恢复 80 点法力值。",
            ConsumableData.ConsumableType.ManaPotion,
            manaRestore: 80, value: 70, maxStack: 5));

        list.Add(MakeBuff("item_potion_str",
            "力量药水", "30 秒内攻击力 +15。",
            buffAtk: 15, buffDuration: 30f, value: 80, maxStack: 3));

        list.Add(MakeBuff("item_potion_iron",
            "铁甲药水", "30 秒内防御力 +10。",
            buffDef: 10, buffDuration: 30f, value: 80, maxStack: 3));

        return list;
    }

    private static (string, ItemData) MakeConsumable(
        string id, string name, string desc,
        ConsumableData.ConsumableType type,
        int healAmount = 0, float healPct = 0f,
        int manaRestore = 0, float manaPct = 0f,
        int value = 10, int maxStack = 10)
    {
        var c = CreateOrLoad<ConsumableData>(ConsumDir, id);
        using var so = new SerializedObject(c);
        so.FindProperty("itemName")             .stringValue  = name;
        so.FindProperty("description")          .stringValue  = desc;
        so.FindProperty("itemType")             .enumValueIndex = (int)ItemType.Consumable;
        so.FindProperty("consumableType")       .enumValueIndex = (int)type;
        so.FindProperty("healAmount")           .intValue     = healAmount;
        so.FindProperty("healPercentage")       .floatValue   = healPct;
        so.FindProperty("manaRestore")          .intValue     = manaRestore;
        so.FindProperty("manaRestorePercentage").floatValue   = manaPct;
        so.FindProperty("value")                .intValue     = value;
        so.FindProperty("maxStackSize")         .intValue     = maxStack;
        so.FindProperty("isSellable")           .boolValue    = true;
        so.FindProperty("isDroppable")          .boolValue    = true;
        so.ApplyModifiedProperties();
        return (id, c);
    }

    private static (string, ItemData) MakeBuff(
        string id, string name, string desc,
        int buffAtk = 0, int buffDef = 0, int buffHp = 0,
        float buffSpd = 0f, float buffDuration = 15f,
        int value = 50, int maxStack = 5)
    {
        var c = CreateOrLoad<ConsumableData>(ConsumDir, id);
        using var so = new SerializedObject(c);
        so.FindProperty("itemName")        .stringValue  = name;
        so.FindProperty("description")     .stringValue  = desc;
        so.FindProperty("itemType")        .enumValueIndex = (int)ItemType.Consumable;
        so.FindProperty("consumableType")  .enumValueIndex = (int)ConsumableData.ConsumableType.Buff;
        so.FindProperty("buffAttackPower") .intValue     = buffAtk;
        so.FindProperty("buffDefense")     .intValue     = buffDef;
        so.FindProperty("buffHealth")      .intValue     = buffHp;
        so.FindProperty("buffMoveSpeed")   .floatValue   = buffSpd;
        so.FindProperty("buffDuration")    .floatValue   = buffDuration;
        so.FindProperty("value")           .intValue     = value;
        so.FindProperty("maxStackSize")    .intValue     = maxStack;
        so.FindProperty("isSellable")      .boolValue    = true;
        so.FindProperty("isDroppable")     .boolValue    = true;
        so.ApplyModifiedProperties();
        return (id, c);
    }

    // ── Weapons ───────────────────────────────────────────────────────────────

    private static List<(string id, ItemData data)> CreateWeapons()
    {
        var list = new List<(string, ItemData)>();

        list.Add(MakeWeapon("item_weapon_iron_sword",
            "铁剑", "标准铁制单手剑，平衡的攻击与防御。",
            EquipmentSlot.MainHand, WeaponData.DamageType.Physical,
            baseDmg: 15, atkSpeed: 1f, atkRange: 1.5f,
            atkBonus: 5, hpBonus: 10, value: 120));

        list.Add(MakeWeapon("item_weapon_dagger",
            "短剑", "轻巧快速，适合偷袭和连击。",
            EquipmentSlot.MainHand, WeaponData.DamageType.Physical,
            baseDmg: 9, atkSpeed: 1.8f, atkRange: 1.2f,
            atkBonus: 8, value: 90));

        list.Add(MakeWeapon("item_weapon_magic_staff",
            "魔杖", "蕴含魔力的法杖，增强法术威力。",
            EquipmentSlot.MainHand, WeaponData.DamageType.Magic,
            baseDmg: 8, atkSpeed: 0.7f, atkRange: 6f,
            atkBonus: 12, value: 180));

        list.Add(MakeWeapon("item_weapon_fire_staff",
            "火焰法杖", "灼热的火焰法杖，释放炽烈火球。",
            EquipmentSlot.MainHand, WeaponData.DamageType.Fire,
            baseDmg: 14, atkSpeed: 0.6f, atkRange: 8f,
            atkBonus: 15, value: 350));

        list.Add(MakeWeapon("item_weapon_ice_wand",
            "寒冰魔杖", "散发冰霜气息，减缓敌人行动。",
            EquipmentSlot.MainHand, WeaponData.DamageType.Ice,
            baseDmg: 11, atkSpeed: 0.8f, atkRange: 7f,
            atkBonus: 13, value: 300));

        list.Add(MakeWeapon("item_weapon_great_sword",
            "大剑", "沉重的双手大剑，毁灭性的力量。",
            EquipmentSlot.MainHand, WeaponData.DamageType.Physical,
            baseDmg: 28, atkSpeed: 0.5f, atkRange: 2f,
            atkBonus: 18, hpBonus: 20, value: 500));

        list.Add(MakeWeapon("item_weapon_bow",
            "猎手弓", "精准的远程弓箭，穿透力强。",
            EquipmentSlot.MainHand, WeaponData.DamageType.Physical,
            baseDmg: 12, atkSpeed: 1.2f, atkRange: 12f,
            atkBonus: 10, value: 200));

        return list;
    }

    private static (string, ItemData) MakeWeapon(
        string id, string name, string desc,
        EquipmentSlot slot, WeaponData.DamageType dmgType,
        int baseDmg, float atkSpeed, float atkRange,
        int atkBonus = 0, int defBonus = 0, int hpBonus = 0,
        float spdBonus = 0f, int value = 100)
    {
        var w = CreateOrLoad<WeaponData>(WeaponDir, id);
        using var so = new SerializedObject(w);
        so.FindProperty("itemName")          .stringValue  = name;
        so.FindProperty("description")       .stringValue  = desc;
        so.FindProperty("itemType")          .enumValueIndex = (int)ItemType.Weapon;
        so.FindProperty("equipmentSlot")     .enumValueIndex = (int)slot;
        so.FindProperty("damageType")        .enumValueIndex = (int)dmgType;
        so.FindProperty("baseDamage")        .intValue     = baseDmg;
        so.FindProperty("attackSpeed")       .floatValue   = atkSpeed;
        so.FindProperty("attackRange")       .floatValue   = atkRange;
        so.FindProperty("attackPowerBonus")  .intValue     = atkBonus;
        so.FindProperty("defenseBonus")      .intValue     = defBonus;
        so.FindProperty("healthBonus")       .intValue     = hpBonus;
        so.FindProperty("moveSpeedBonus")    .floatValue   = spdBonus;
        so.FindProperty("value")             .intValue     = value;
        so.FindProperty("maxStackSize")      .intValue     = 1;
        so.FindProperty("isSellable")        .boolValue    = true;
        so.FindProperty("isDroppable")       .boolValue    = true;
        so.ApplyModifiedProperties();
        return (id, w);
    }

    // ── Armors ────────────────────────────────────────────────────────────────

    private static List<(string id, ItemData data)> CreateArmors()
    {
        var list = new List<(string, ItemData)>();

        list.Add(MakeArmor("item_armor_leather_chest",
            "皮甲", "轻便的皮革护甲，适合灵活的战士。",
            EquipmentSlot.Chest, baseDefense: 5, defBonus: 3, value: 80));

        list.Add(MakeArmor("item_armor_iron_chest",
            "铁甲", "坚固的铁制胸甲，提供可靠的防护。",
            EquipmentSlot.Chest, baseDefense: 12, defBonus: 6, hpBonus: 15, value: 250));

        list.Add(MakeArmor("item_armor_leather_helmet",
            "皮革头盔", "基础皮革头盔，保护头部。",
            EquipmentSlot.Head, baseDefense: 3, defBonus: 2, value: 50));

        list.Add(MakeArmor("item_armor_iron_helmet",
            "铁盔", "坚固的铁制头盔，增强防御。",
            EquipmentSlot.Head, baseDefense: 7, defBonus: 4, hpBonus: 10, value: 150));

        list.Add(MakeArmor("item_armor_cloth_boots",
            "布鞋", "轻便的布鞋，略微提升移动速度。",
            EquipmentSlot.Feet, baseDefense: 1, spdBonus: 0.3f, value: 30));

        list.Add(MakeArmor("item_armor_iron_boots",
            "铁靴", "沉重的铁靴，增强防御但略微影响速度。",
            EquipmentSlot.Feet, baseDefense: 5, defBonus: 2, spdBonus: -0.1f, value: 120));

        list.Add(MakeArmor("item_armor_leather_legs",
            "皮腿甲", "轻便的腿部防护。",
            EquipmentSlot.Legs, baseDefense: 4, defBonus: 2, value: 70));

        list.Add(MakeArmor("item_armor_iron_legs",
            "铁腿甲", "铁制腿部护甲，提供充足防御。",
            EquipmentSlot.Legs, baseDefense: 9, defBonus: 4, hpBonus: 8, value: 200));

        list.Add(MakeAccessory("item_ring_power",
            "力量戒指", "镶嵌红宝石，提升攻击力。",
            EquipmentSlot.Ring, atkBonus: 8, value: 200));

        list.Add(MakeAccessory("item_ring_defense",
            "守护戒指", "镶嵌蓝宝石，提升防御力。",
            EquipmentSlot.Ring, defBonus: 6, hpBonus: 20, value: 200));

        list.Add(MakeAccessory("item_amulet_wisdom",
            "智慧项链", "古老的魔法项链，增强法术威力。",
            EquipmentSlot.Amulet, atkBonus: 10, spdBonus: 0.2f, value: 350));

        return list;
    }

    private static (string, ItemData) MakeArmor(
        string id, string name, string desc,
        EquipmentSlot slot, int baseDefense,
        int defBonus = 0, int hpBonus = 0, float spdBonus = 0f, int value = 100)
    {
        var a = CreateOrLoad<ArmorData>(ArmorDir, id);
        using var so = new SerializedObject(a);
        so.FindProperty("itemName")         .stringValue  = name;
        so.FindProperty("description")      .stringValue  = desc;
        so.FindProperty("itemType")         .enumValueIndex = (int)ItemType.Armor;
        so.FindProperty("equipmentSlot")    .enumValueIndex = (int)slot;
        so.FindProperty("baseDefense")      .intValue     = baseDefense;
        so.FindProperty("defenseBonus")     .intValue     = defBonus;
        so.FindProperty("healthBonus")      .intValue     = hpBonus;
        so.FindProperty("moveSpeedBonus")   .floatValue   = spdBonus;
        so.FindProperty("value")            .intValue     = value;
        so.FindProperty("maxStackSize")     .intValue     = 1;
        so.FindProperty("isSellable")       .boolValue    = true;
        so.FindProperty("isDroppable")      .boolValue    = true;
        so.ApplyModifiedProperties();
        return (id, a);
    }

    private static (string, ItemData) MakeAccessory(
        string id, string name, string desc,
        EquipmentSlot slot,
        int atkBonus = 0, int defBonus = 0, int hpBonus = 0, float spdBonus = 0f,
        int value = 150)
    {
        var e = CreateOrLoad<EquipmentData>(ArmorDir, id);
        using var so = new SerializedObject(e);
        so.FindProperty("itemName")         .stringValue  = name;
        so.FindProperty("description")      .stringValue  = desc;
        so.FindProperty("itemType")         .enumValueIndex = (int)ItemType.Accessory;
        so.FindProperty("equipmentSlot")    .enumValueIndex = (int)slot;
        so.FindProperty("attackPowerBonus") .intValue     = atkBonus;
        so.FindProperty("defenseBonus")     .intValue     = defBonus;
        so.FindProperty("healthBonus")      .intValue     = hpBonus;
        so.FindProperty("moveSpeedBonus")   .floatValue   = spdBonus;
        so.FindProperty("value")            .intValue     = value;
        so.FindProperty("maxStackSize")     .intValue     = 1;
        so.FindProperty("isSellable")       .boolValue    = true;
        so.FindProperty("isDroppable")      .boolValue    = true;
        so.ApplyModifiedProperties();
        return (id, e);
    }

    // ── ItemDatabase population ───────────────────────────────────────────────

    private static void PopulateItemDatabase(ItemDatabase db, List<(string id, ItemData data)> items)
    {
        using var so = new SerializedObject(db);
        var arr = so.FindProperty("items");
        arr.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            var elem = arr.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("itemId")  .stringValue         = items[i].id;
            elem.FindPropertyRelative("itemData").objectReferenceValue = items[i].data;
        }
        so.ApplyModifiedProperties();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static T CreateOrLoad<T>(string dir, string assetName) where T : ScriptableObject
    {
        string path = $"{dir}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        var obj = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(obj, path);
        return obj;
    }

    private static void EnsureDirectories()
    {
        foreach (var dir in new[]
        {
            RootDir, PlayerDir,
            "Assets/Data/Items",
            "Assets/Data/Items/Consumables",
            "Assets/Data/Items/Equipment",
            WeaponDir, ArmorDir, DbDir
        })
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        AssetDatabase.Refresh();
    }
}
