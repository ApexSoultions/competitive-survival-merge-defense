using UnityEngine;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; private set; }

    [Header("Merge")]
    [SerializeField] private int maxMergeLevel = 6;

    [Header("Merge FX")]
    [SerializeField] private ParticleSystem mergeEffectPrefab = null;
    [SerializeField] private float fallbackMergeEffectScale = 1f;

    [Header("Deck")]
    [SerializeField] private UnitData[] selectedDeckUnits;

    private void Awake()
    {
        Instance = this;
    }

    public void SetSelectedDeck(UnitData[] deckUnits)
    {
        selectedDeckUnits = deckUnits != null ? (UnitData[])deckUnits.Clone() : null;
    }

    public bool TryMerge(BoardTower sourceTower, BoardTower targetTower)
    {
        if (!BattleFlowState.IsGameplayActive)
        {
            UnityEngine.Debug.Log("Merge blocked: battle is not active.");
            return false;
        }

        if (sourceTower == null || targetTower == null)
        {
            UnityEngine.Debug.LogWarning("Merge failed: source or target missing.");
            return false;
        }

        if (sourceTower == targetTower)
        {
            UnityEngine.Debug.Log("Merge failed: same tower.");
            return false;
        }

        if (sourceTower.UnitData == null || targetTower.UnitData == null)
        {
            UnityEngine.Debug.LogWarning("Merge failed: UnitData missing.");
            return false;
        }

        TowerBoardCell sourceCell = sourceTower.CurrentCell;
        TowerBoardCell targetCell = targetTower.CurrentCell;

        if (sourceCell == null || targetCell == null)
        {
            UnityEngine.Debug.LogWarning("Merge failed: cell missing.");
            return false;
        }

        if (sourceCell == targetCell ||
            sourceCell.CurrentTower != sourceTower ||
            targetCell.CurrentTower != targetTower)
        {
            UnityEngine.Debug.LogWarning("Merge failed: board cell ownership is stale.");
            return false;
        }

        LightFairyAbility lightFairy = sourceTower.GetComponent<LightFairyAbility>();
        if (lightFairy != null)
            return TryUpgradeWithLightFairy(lightFairy, sourceTower, targetTower, sourceCell, targetCell);

        ShapeshifterAbility shapeshifter = sourceTower.GetComponent<ShapeshifterAbility>();
        if (shapeshifter != null)
            return TryCopyShapeshifter(shapeshifter, sourceTower, targetTower, sourceCell);

        if (sourceTower.UnitData != targetTower.UnitData)
        {
            UnityEngine.Debug.Log("Merge failed: different unit type.");
            return false;
        }

        if (sourceTower.Level != targetTower.Level)
        {
            UnityEngine.Debug.Log("Merge failed: different level.");
            return false;
        }

        int effectiveMaxLevel = Mathf.Clamp(maxMergeLevel, 1, UnitData.MaximumLevel);

        if (sourceTower.Level < 1 || sourceTower.Level >= effectiveMaxLevel)
        {
            UnityEngine.Debug.Log("Merge failed: max level reached.");
            return false;
        }

        int nextLevel = sourceTower.Level + 1;

        UnitData resultUnit = GetRandomUnitFromDeck(sourceTower.UnitData, nextLevel);

        if (resultUnit == null)
        {
            UnityEngine.Debug.LogWarning("Merge failed: result unit missing.");
            return false;
        }

        GameObject resultPrefab = resultUnit.GetPrefabExact(nextLevel);

        if (resultPrefab == null)
        {
            UnityEngine.Debug.LogWarning("Merge failed: missing prefab for " + resultUnit.unitName + " Lv" + nextLevel);
            return false;
        }

        sourceCell.ClearCell(sourceTower);
        targetCell.ClearCell(targetTower);

        bool placed = targetCell.PlaceTower(resultPrefab, resultUnit, nextLevel);

        if (!placed)
        {
            sourceCell.SetTower(sourceTower);
            targetCell.SetTower(targetTower);
            UnityEngine.Debug.LogWarning("Merge failed: target cell could not place result tower.");
            return false;
        }

        sourceTower.SetCell(null);
        targetTower.SetCell(null);

        Destroy(sourceTower.gameObject);
        Destroy(targetTower.gameObject);

        if (targetCell.CurrentTower != null)
            targetCell.CurrentTower.TriggerPulseEffect();

        if (GameStatsTracker.Instance != null)
            GameStatsTracker.Instance.AddMerge(nextLevel);

        PlayMergeEffect(targetCell.SpawnPosition);
        GameAudioManager.PlayMerge();
        GameplayEvents.RaiseUnitMerged(resultUnit, nextLevel);

        UnityEngine.Debug.Log("Merge success: " + resultUnit.unitName + " Lv" + nextLevel);
        return true;
    }

    private bool TryUpgradeWithLightFairy(
        LightFairyAbility lightFairy,
        BoardTower sourceTower,
        BoardTower targetTower,
        TowerBoardCell sourceCell,
        TowerBoardCell targetCell)
    {
        int effectiveMaxLevel = Mathf.Clamp(maxMergeLevel, 1, UnitData.MaximumLevel);
        if (!lightFairy.CanUpgradeTarget(targetTower, effectiveMaxLevel))
        {
            UnityEngine.Debug.Log(
                "Light Fairy upgrade cancelled: target must be an active equal-level ally below the maximum merge level.");
            return false;
        }

        int upgradedLevel = targetTower.Level + 1;
        UnitData upgradedUnit = targetTower.UnitData;
        GameObject upgradedPrefab = upgradedUnit.GetPrefabExact(upgradedLevel);
        if (upgradedPrefab == null)
        {
            UnityEngine.Debug.LogWarning(
                "Light Fairy upgrade cancelled: missing exact prefab for " + upgradedUnit.unitName + " Lv" + upgradedLevel + ".");
            return false;
        }

        Tower targetAttack = targetTower.GetComponent<Tower>();
        Tower.DirectUpgradeRuntimeState combatState = targetAttack != null
            ? targetAttack.CaptureDirectUpgradeRuntimeState()
            : null;
        TowerAbilityBase[] previousAbilities = targetTower.GetComponents<TowerAbilityBase>();

        targetCell.ClearCell(targetTower);
        bool placed = targetCell.PlaceTower(upgradedPrefab, upgradedUnit, upgradedLevel);
        if (!placed)
        {
            targetCell.SetTower(targetTower);
            UnityEngine.Debug.LogWarning("Light Fairy upgrade cancelled: target cell could not place the next-level unit.");
            return false;
        }

        BoardTower upgradedTower = targetCell.CurrentTower;
        if (upgradedTower == null)
        {
            targetCell.SetTower(targetTower);
            UnityEngine.Debug.LogWarning("Light Fairy upgrade cancelled: upgraded tower was not created.");
            return false;
        }

        Tower upgradedAttack = upgradedTower.GetComponent<Tower>();
        upgradedAttack?.RestoreDirectUpgradeRuntimeState(combatState);
        TransferAbilityStates(previousAbilities, upgradedTower.GetComponents<TowerAbilityBase>());

        lightFairy.PlayUpgradeFeedback(upgradedTower);
        PlayLightFairyUpgradeEffect(targetCell.SpawnPosition);

        sourceCell.ClearCell(sourceTower);
        sourceTower.SetCell(null);
        targetTower.SetCell(null);
        sourceTower.gameObject.SetActive(false);
        targetTower.gameObject.SetActive(false);
        Destroy(sourceTower.gameObject);
        Destroy(targetTower.gameObject);

        upgradedTower.TriggerPulseEffect();
        if (GameStatsTracker.Instance != null)
            GameStatsTracker.Instance.AddMerge(upgradedLevel);

        GameplayEvents.RaiseUnitUpgraded(upgradedUnit, upgradedLevel);

        UnityEngine.Debug.Log(
            "Light Fairy upgrade success: " + upgradedUnit.unitName + " advanced to Lv" + upgradedLevel +
            " in its original cell.");
        return true;
    }

    private static void TransferAbilityStates(
        TowerAbilityBase[] previousAbilities,
        TowerAbilityBase[] upgradedAbilities)
    {
        if (previousAbilities == null || upgradedAbilities == null)
            return;

        bool[] used = new bool[upgradedAbilities.Length];
        for (int previousIndex = 0; previousIndex < previousAbilities.Length; previousIndex++)
        {
            TowerAbilityBase previous = previousAbilities[previousIndex];
            if (previous == null)
                continue;

            for (int upgradedIndex = 0; upgradedIndex < upgradedAbilities.Length; upgradedIndex++)
            {
                TowerAbilityBase upgraded = upgradedAbilities[upgradedIndex];
                if (used[upgradedIndex] || upgraded == null || upgraded.GetType() != previous.GetType())
                    continue;

                previous.TransferDirectUpgradeStateTo(upgraded);
                used[upgradedIndex] = true;
                break;
            }
        }
    }

    private bool TryCopyShapeshifter(
        ShapeshifterAbility shapeshifter,
        BoardTower sourceTower,
        BoardTower targetTower,
        TowerBoardCell sourceCell)
    {
        if (!shapeshifter.CanCopyTarget(targetTower))
        {
            UnityEngine.Debug.Log("Shapeshifter copy cancelled: target is invalid or has a different merge level.");
            return false;
        }

        int copiedLevel = sourceTower.Level;
        UnitData copiedUnit = targetTower.UnitData;
        GameObject copiedPrefab = copiedUnit.GetPrefabExact(copiedLevel);

        if (copiedPrefab == null)
        {
            UnityEngine.Debug.LogWarning(
                "Shapeshifter copy cancelled: missing exact prefab for " + copiedUnit.unitName + " Lv" + copiedLevel + ".");
            return false;
        }

        sourceCell.ClearCell(sourceTower);
        bool placed = sourceCell.PlaceTower(copiedPrefab, copiedUnit, copiedLevel);

        if (!placed)
        {
            sourceCell.SetTower(sourceTower);
            UnityEngine.Debug.LogWarning("Shapeshifter copy cancelled: source cell could not place the copied unit.");
            return false;
        }

        BoardTower copiedTower = sourceCell.CurrentTower;
        shapeshifter.PlayTransformationFeedback(targetTower);

        sourceTower.SetCell(null);
        Destroy(sourceTower.gameObject);

        if (copiedTower != null)
            copiedTower.TriggerPulseEffect();

        GameplayEvents.RaiseUnitTransformed(copiedUnit, copiedLevel);

        UnityEngine.Debug.Log(
            "Shapeshifter copy success: transformed into " + copiedUnit.unitName + " Lv" + copiedLevel +
            " in the original cell; target was unchanged.");
        return true;
    }

    private UnitData GetRandomUnitFromDeck(UnitData fallbackUnit, int resultLevel)
    {
        UnitData[] deck = selectedDeckUnits;

        if ((deck == null || deck.Length == 0) && SummonManager.Instance != null)
            deck = SummonManager.Instance.SelectedDeckUnits;

        if (deck == null || deck.Length == 0)
            return fallbackUnit;

        int startIndex = UnityEngine.Random.Range(0, deck.Length);

        for (int offset = 0; offset < deck.Length; offset++)
        {
            UnitData unit = deck[(startIndex + offset) % deck.Length];

            if (unit != null && unit.GetPrefabExact(resultLevel) != null)
                return unit;
        }

        return fallbackUnit != null && fallbackUnit.GetPrefabExact(resultLevel) != null
            ? fallbackUnit
            : null;
    }

    private void PlayLightFairyUpgradeEffect(Vector3 position)
    {
        PlayMergeEffect(position);
        GameAudioManager.PlayMerge();
    }

    private void PlayMergeEffect(Vector3 position)
    {
        ParticleSystem effect = mergeEffectPrefab != null
            ? Instantiate(mergeEffectPrefab, position, Quaternion.identity)
            : CreateFallbackMergeEffect(position);

        if (effect == null)
            return;

        effect.Play(true);

        ParticleSystem.MainModule main = effect.main;
        float destroyDelay = main.duration + main.startLifetime.constantMax + 0.5f;
        Destroy(effect.gameObject, destroyDelay);
    }

    private ParticleSystem CreateFallbackMergeEffect(Vector3 position)
    {
        GameObject effectObject = new GameObject("Merge_Burst_FX");
        effectObject.transform.position = position;

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.7f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 5.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f * fallbackMergeEffectScale, 0.22f * fallbackMergeEffectScale);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 140;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)58),
            new ParticleSystem.Burst(0.08f, (short)26)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.32f * fallbackMergeEffectScale;
        shape.arc = 360f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.28f), 0f),
                new GradientColorKey(new Color(1f, 0.52f, 0.12f), 0.45f),
                new GradientColorKey(new Color(0.35f, 0.95f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 240;

        // Fix pink particles
#if UNITY_EDITOR
        renderer.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
        if (renderer.sharedMaterial == null)
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
#endif

        return particles;
    }

    private void OnValidate()
    {
        maxMergeLevel = Mathf.Clamp(maxMergeLevel, 1, UnitData.MaximumLevel);
    }
}
