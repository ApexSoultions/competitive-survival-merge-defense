#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PrototypeGameplayValidator
{
    private sealed class HeroSpec
    {
        public readonly string displayName;
        public readonly string dataPath;
        public readonly string prefabFolder;
        public readonly Type abilityType;
        public readonly string[] spriteNames;

        public HeroSpec(string name, string data, string folder, Type ability, params string[] sprites)
        {
            displayName = name;
            dataPath = data;
            prefabFolder = folder;
            abilityType = ability;
            spriteNames = sprites;
        }
    }

    private static readonly HeroSpec[] Heroes =
    {
        new HeroSpec("Enchantress", "Assets/Script/Unit/UnitData/Enchantress_Data.asset", "Assets/_Prefabs/Units/Enchantress", typeof(NatureBlessingBuffAbility), "char level 1_0", "char level 2_0", "char level 3_0", "char level 4_0", "char level 5_0", "char level 6_0"),
        new HeroSpec("Fire Mage", "Assets/Script/Unit/UnitData/FireMage_Data.asset", "Assets/_Prefabs/Units/Fire Mage", typeof(FireMageAoEAbility), "char level 1_1", "char level 2_1", "char level 3_1", "char level 4_2", "char level 5_1", "char level 6_1"),
        new HeroSpec("Frost Witch", "Assets/Script/Unit/UnitData/Frost Witch_Data.asset", "Assets/_Prefabs/Units/Frost Witch", typeof(FrostWitchSlowAbility), "char level 1_2", "char level 2_2", "char level 3_2", "char level 4_3", "char level 5_5", "char level 6_3"),
        new HeroSpec("Golden Spirit", "Assets/Script/Unit/UnitData/Golden Spirit_Data.asset", "Assets/_Prefabs/Units/Golden Spirit", typeof(GoldSpiritAbility), "char level 1_3", "char level 2_3", "char level 3_3", "char level 4_4", "char level 5_2", "char level 6_2"),
        new HeroSpec("Magic Archer", "Assets/Script/Unit/UnitData/Magic Archer_Data.asset", "Assets/_Prefabs/Units/Magic Archer", null, "char level 1_4", "char level 2_7", "char level 3_6", "char level 4_5", "char level 5_7", "char level 6_4"),
        new HeroSpec("Plague Doctor", "Assets/Script/Unit/UnitData/Poison Druid_Data.asset", "Assets/_Prefabs/Units/Poison Druid", typeof(PlagueDoctorPoisonAbility), "char level 1_5", "char level 2_12", "char level 3_10", "char level 4_1", "char level 5_9", "char level 6_7"),
        new HeroSpec("Shapeshifter", "Assets/Script/Unit/UnitData/Shapeshifte_Data.asset", "Assets/_Prefabs/Units/Shapeshifter", typeof(ShapeshifterAbility), "char level 1_6", "char level 2_14", "char level 3_11", "char level 4_6", "char level 5_19", "char level 6_8"),
        new HeroSpec("Princess", "Assets/Script/Unit/UnitData/Princess_Data.asset", "Assets/_Prefabs/Units/Princess", null, "char level 1_7", "char level 2_15", "char level 3_13", "char level 4_7", "char level 5_22", "char level 6_9"),
        new HeroSpec("Stone Guardian", "Assets/Script/Unit/UnitData/Stone Guardian_Data.asset", "Assets/_Prefabs/Units/Stone Guardian", typeof(StoneGolemStunAbility), "char level 1_8", "char level 2_16", "char level 3_15", "char level 4_8", "char level 5_3", "char level 6_10"),
        new HeroSpec("Zeus", "Assets/Script/Unit/UnitData/Zeus_Data.asset", "Assets/_Prefabs/Units/Zeus", typeof(ChainLightningAbility), "char level 1_9", "char level 2_17", "char level 3_18", "char level 4_9", "char level 5_4", "char level 6_5"),
        new HeroSpec("Light Fairy", "Assets/Script/Unit/UnitData/Light Fairy_Data.asset", "Assets/_Prefabs/Units/Light_Fairy", typeof(LightFairyAbility), "fairy_17", "fairy_1", "fairy_15", "fairy_21", "fairy_22", "fairy_0")
    };

    [MenuItem("Tools/Prototype/Validate Gameplay Content")]
    public static void ValidateFromMenu()
    {
        List<string> errors = ValidateAll();
        if (errors.Count == 0)
            Debug.Log("Prototype validation passed: " + Heroes.Length + " heroes, identities, level prefabs, sprites, abilities, projectiles, and scale references are valid.");
        else
            Debug.LogError("Prototype validation failed:\n- " + string.Join("\n- ", errors));
    }

    public static void ValidateForCommandLine()
    {
        List<string> errors = ValidateAll();
        if (errors.Count > 0)
            throw new InvalidOperationException("Prototype validation failed:\n- " + string.Join("\n- ", errors));
        Debug.Log("Prototype command-line validation passed.");
    }

    public static List<string> ValidateAll()
    {
        List<string> errors = new List<string>();
        HashSet<Sprite> portraitIcons = new HashSet<Sprite>();

        for (int heroIndex = 0; heroIndex < Heroes.Length; heroIndex++)
            ValidateHero(Heroes[heroIndex], portraitIcons, errors);

        ValidateFeedbackAssets(errors);

        return errors;
    }

    public static List<string> ValidateClientFeedbackScope()
    {
        List<string> errors = new List<string>();
        HashSet<Sprite> portraitIcons = new HashSet<Sprite>();

        for (int heroIndex = 0; heroIndex < Heroes.Length; heroIndex++)
        {
            string name = Heroes[heroIndex].displayName;
            if (name == "Enchantress" || name == "Golden Spirit" || name == "Shapeshifter" || name == "Princess" ||
                name == "Stone Guardian" || name == "Zeus" || name == "Light Fairy")
            {
                ValidateHero(Heroes[heroIndex], portraitIcons, errors);
            }
        }

        ValidateFeedbackAssets(errors);
        return errors;
    }

    private static void ValidateHero(HeroSpec spec, HashSet<Sprite> portraitIcons, List<string> errors)
    {
        UnitData data = AssetDatabase.LoadAssetAtPath<UnitData>(spec.dataPath);
        if (data == null)
        {
            errors.Add(spec.displayName + ": UnitData asset is missing.");
            return;
        }

        if (!string.Equals(data.unitName, spec.displayName, StringComparison.Ordinal))
            errors.Add(spec.displayName + ": runtime name is '" + data.unitName + "'.");
        if (data.icon == null)
            errors.Add(spec.displayName + ": portrait icon is missing.");
        else if (!portraitIcons.Add(data.icon))
            errors.Add(spec.displayName + ": portrait icon is shared with a different hero.");
        if (data.GetIcon(1) != data.icon)
            errors.Add(spec.displayName + ": Level 1 merge icon does not match its portrait identity.");
        if (data.levelPrefabs == null || data.levelPrefabs.Length != UnitData.MaximumLevel)
        {
            errors.Add(spec.displayName + ": expected exactly six level prefabs.");
            return;
        }
        if (data.prefab != data.levelPrefabs[0])
            errors.Add(spec.displayName + ": base prefab is not the Level 1 prefab.");

        for (int level = 1; level <= UnitData.MaximumLevel; level++)
        {
            GameObject prefab = data.GetPrefabExact(level);
            if (prefab == null)
            {
                errors.Add(spec.displayName + " Lv" + level + ": prefab is missing.");
                continue;
            }

            string prefabPath = AssetDatabase.GetAssetPath(prefab).Replace('\\', '/');
            if (!prefabPath.StartsWith(spec.prefabFolder + "/", StringComparison.Ordinal))
                errors.Add(spec.displayName + " Lv" + level + ": prefab points outside its hero folder: " + prefabPath);

            ValidatePrefab(spec, data, prefab, level, errors);
        }
    }

    private static void ValidatePrefab(HeroSpec spec, UnitData data, GameObject prefab, int level, List<string> errors)
    {
        string label = spec.displayName + " Lv" + level;
        if (prefab.GetComponent<Tower>() == null)
            errors.Add(label + ": Tower component is missing.");
        if (prefab.GetComponent<BoardTower>() == null)
            errors.Add(label + ": BoardTower component is missing.");
        if (prefab.GetComponentInChildren<Collider2D>(true) == null)
            errors.Add(label + ": tower collider is missing.");
        if (!Approximately(prefab.transform.localScale, Vector3.one * 0.6f))
            errors.Add(label + ": root scale is not the normalized 0.6 value.");

        Transform visual = prefab.transform.Find("Visual");
        SpriteRenderer renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        if (renderer == null || renderer.sprite == null)
            errors.Add(label + ": Visual SpriteRenderer or sprite is missing.");
        else if (!string.Equals(renderer.sprite.name, spec.spriteNames[level - 1], StringComparison.Ordinal))
            errors.Add(label + ": expected sprite '" + spec.spriteNames[level - 1] + "' but found '" + renderer.sprite.name + "'.");

        TowerAbilityBase[] abilities = prefab.GetComponents<TowerAbilityBase>();
        if (spec.abilityType == null)
        {
            if (abilities.Length != 0)
                errors.Add(label + ": has an unexpected active ability component.");
        }
        else
        {
            if (abilities.Length != 1 || abilities[0] == null || abilities[0].GetType() != spec.abilityType)
                errors.Add(label + ": ability assignment does not match " + spec.abilityType.Name + ".");
        }

        Tower tower = prefab.GetComponent<Tower>();
        if (tower != null)
        {
            SerializedObject serializedTower = new SerializedObject(tower);
            if (level == 1 && ShouldValidateUnitDataStats(spec.displayName))
            {
                ValidateFloat(label, "UnitData attack damage", data.attackDamage,
                    serializedTower.FindProperty("damage")?.floatValue ?? float.NaN, errors);
                ValidateFloat(label, "UnitData attack speed", data.attackSpeed,
                    serializedTower.FindProperty("attackRate")?.floatValue ?? float.NaN, errors);
                ValidateFloat(label, "UnitData attack range", data.attackRange,
                    serializedTower.FindProperty("attackRange")?.floatValue ?? float.NaN, errors);
            }

            if (serializedTower.FindProperty("firePoint")?.objectReferenceValue == null)
                errors.Add(label + ": fire point is missing.");
            UnityEngine.Object projectile = serializedTower.FindProperty("bulletPrefab")?.objectReferenceValue;
            if (projectile == null)
                errors.Add(label + ": projectile reference is missing.");
            else
            {
                string actualProjectilePath = AssetDatabase.GetAssetPath(projectile).Replace('\\', '/');
                string expectedProjectilePath = GetExpectedProjectilePath(spec.displayName, level);
                if (!string.Equals(actualProjectilePath, expectedProjectilePath, StringComparison.Ordinal))
                    errors.Add(label + ": expected projectile '" + expectedProjectilePath + "' but found '" + actualProjectilePath + "'.");
            }
        }

        ValidateSpecialAbilityConfiguration(spec, prefab, label, errors);
    }

    private static bool ShouldValidateUnitDataStats(string heroName)
    {
        return heroName == "Golden Spirit" || heroName == "Shapeshifter" || heroName == "Princess" ||
               heroName == "Stone Guardian" || heroName == "Zeus" || heroName == "Light Fairy";
    }

    private static void ValidateSpecialAbilityConfiguration(
        HeroSpec spec,
        GameObject prefab,
        string label,
        List<string> errors)
    {
        if (spec.displayName == "Enchantress")
        {
            NatureBlessingBuffAbility blessing = prefab.GetComponent<NatureBlessingBuffAbility>();
            SerializedObject serialized = blessing != null ? new SerializedObject(blessing) : null;
            if (serialized == null || serialized.FindProperty("maximumBuffedTowers")?.intValue != 4)
                errors.Add(label + ": Nature Blessing must be fixed to exactly four targets.");
            if (serialized != null && serialized.FindProperty("allowBuffStacking")?.boolValue == true)
                errors.Add(label + ": Nature Blessing must not multiply repeatedly across refreshes or sources.");
        }
        else if (spec.displayName == "Zeus")
        {
            if (prefab.GetComponent<StoneGolemStunAbility>() != null)
                errors.Add(label + ": Zeus must not contain a stun ability or stun status configuration.");

            ChainLightningAbility chain = prefab.GetComponent<ChainLightningAbility>();
            SerializedObject serialized = chain != null ? new SerializedObject(chain) : null;
            if (serialized == null || serialized.FindProperty("chainDamageMultiplier").floatValue < 0f)
                errors.Add(label + ": configurable chain damage is invalid.");
        }
        else if (spec.displayName == "Stone Guardian")
        {
            StoneGolemStunAbility stun = prefab.GetComponent<StoneGolemStunAbility>();
            SerializedObject serialized = stun != null ? new SerializedObject(stun) : null;
            if (serialized == null || serialized.FindProperty("stunDuration").floatValue <= 0f)
                errors.Add(label + ": configurable stun duration is missing or invalid.");
        }
        else if (spec.displayName == "Golden Spirit")
        {
            GoldSpiritAbility gold = prefab.GetComponent<GoldSpiritAbility>();
            if (gold == null)
                return;

            SerializedObject serializedTower = new SerializedObject(prefab.GetComponent<Tower>());
            if (serializedTower.FindProperty("canReceiveAlliedDamageBuffs")?.boolValue != false)
                errors.Add(label + ": mana-support units must be excluded from Enchantress damage buffs.");

            SerializedObject serialized = new SerializedObject(gold);
            ValidateFloat(label, "mana tick interval", 5f,
                serialized.FindProperty("tickInterval").floatValue, errors);

            SerializedProperty amounts = serialized.FindProperty("manaByMergeLevel");
            if (amounts == null || amounts.arraySize != UnitData.MaximumLevel)
            {
                errors.Add(label + ": expected six configured mana-generation values.");
            }
            else
            {
                for (int level = 1; level <= UnitData.MaximumLevel; level++)
                {
                    int expected = level * 10;
                    int actual = amounts.GetArrayElementAtIndex(level - 1).intValue;
                    if (actual != expected || gold.GetManaAmountForLevel(level) != expected)
                        errors.Add(label + ": merge level " + level + " must grant " + expected + " mana.");
                }
            }
        }
        else if (spec.displayName == "Light Fairy")
        {
            LightFairyAbility fairy = prefab.GetComponent<LightFairyAbility>();
            if (fairy == null)
                errors.Add(label + ": Radiant Blessing ability is missing.");
            if (prefab.GetComponent<NatureBlessingBuffAbility>() != null)
                errors.Add(label + ": still contains the Enchantress placeholder ability.");

            SerializedObject serializedTower = new SerializedObject(prefab.GetComponent<Tower>());
            if (serializedTower.FindProperty("fullAreaTargeting")?.boolValue != true)
                errors.Add(label + ": battlefield targeting is disabled, so this unit cannot fire from every board cell.");
            if (serializedTower.FindProperty("canReceiveAlliedDamageBuffs")?.boolValue != false)
                errors.Add(label + ": merge-support units must be excluded from Enchantress damage buffs.");
        }
    }

    private static void ValidateFloat(string label, string field, float expected, float actual, List<string> errors)
    {
        if (float.IsNaN(actual) || Mathf.Abs(expected - actual) > 0.0001f)
            errors.Add(label + ": " + field + " is " + actual + " but expected " + expected + ".");
    }

    private static string GetExpectedProjectilePath(string heroName, int level)
    {
        if (heroName == "Fire Mage")
            return level == 4
                ? "Assets/_Prefabs/Bullets/Fire_Mage/Bullet_4.prefab"
                : "Assets/_Prefabs/Bullets/Fire_Mage/Bullet.prefab";
        if (heroName == "Frost Witch")
            return "Assets/_Prefabs/Bullets/Frost Witch/Frost Witch_1.prefab";
        if (heroName == "Golden Spirit")
            return "Assets/_Prefabs/Bullets/Golden Spirit/Golden Spirit_Bullet_1.prefab";
        if (heroName == "Light Fairy")
            return "Assets/_Prefabs/Bullets/Golden Spirit/Golden Spirit_Bullet_1.prefab";
        return "Assets/_Prefabs/Bullets/Bullet 1.prefab";
    }

    private static void ValidateFeedbackAssets(List<string> errors)
    {
        ValidateProjectileAssets(errors);

        EnemyCombatFeedbackTheme theme = AssetDatabase.LoadAssetAtPath<EnemyCombatFeedbackTheme>(
            "Assets/Resources/CombatFeedback/EnemyCombatFeedbackTheme.asset");
        if (theme == null)
        {
            errors.Add("Combat feedback theme is missing.");
            return;
        }

        if (theme.HealthFrameSprite == null || theme.HealthFillSprite == null || theme.StatusIconFrameSprite == null)
            errors.Add("Combat feedback theme is missing a health-bar or status-icon sprite.");
        if (AssetDatabase.LoadAssetAtPath<FloatingDamageText>(
                "Assets/Resources/CombatFeedback/FloatingDamageText.prefab") == null)
            errors.Add("Floating damage-number prefab is missing.");
    }

    private static void ValidateProjectileAssets(List<string> errors)
    {
        string[] projectileGuids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { "Assets/_Prefabs/Bullets" });

        for (int i = 0; i < projectileGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(projectileGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Bullet bullet = prefab != null ? prefab.GetComponent<Bullet>() : null;
            if (bullet == null)
                continue;

            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            bool hasVisibleSprite = false;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                if (renderers[rendererIndex] != null && renderers[rendererIndex].sprite != null)
                {
                    hasVisibleSprite = true;
                    break;
                }
            }

            if (!hasVisibleSprite)
                errors.Add(path + ": projectile has no visible sprite.");

            SerializedObject serializedBullet = new SerializedObject(bullet);
            SerializedProperty targetSize = serializedBullet.FindProperty("targetWorldSize");
            if (targetSize == null || targetSize.floatValue < 0.05f)
                errors.Add(path + ": projectile visual normalization is missing or invalid.");
        }
    }

    private static bool Approximately(Vector3 left, Vector3 right)
    {
        return Mathf.Abs(left.x - right.x) < 0.0001f &&
               Mathf.Abs(left.y - right.y) < 0.0001f &&
               Mathf.Abs(left.z - right.z) < 0.0001f;
    }
}
#endif
