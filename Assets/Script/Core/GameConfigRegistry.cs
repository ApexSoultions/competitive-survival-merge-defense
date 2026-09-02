using UnityEngine;

/// <summary>
/// Single source of truth for game configuration assets.
/// Canonical path: Assets/Content/Resources/GameConfigRegistry.asset
/// (Resources.Load works; do not duplicate configs under Assets/Resources).
/// </summary>
[CreateAssetMenu(fileName = "GameConfigRegistry", menuName = "Game/Core/Game Config Registry")]
public class GameConfigRegistry : ScriptableObject
{
    [Header("Core")]
    [SerializeField] private SceneFlowConfig sceneFlow;
    [SerializeField] private GameBalanceConfig gameBalance;
    [SerializeField] private MobileQualityCatalog mobileQuality;

    [Header("Content catalogs")]
    [SerializeField] private ActiveAbilityCatalog activeAbilities;
    [SerializeField] private UnitCatalog units;
    [SerializeField] private RelicCatalog relics;
    [SerializeField] private SpecialTileCatalog specialTiles;
    [SerializeField] private WaveTable defaultWaveTable;

    public SceneFlowConfig SceneFlow => sceneFlow;
    public GameBalanceConfig GameBalance => gameBalance;
    public MobileQualityCatalog MobileQuality => mobileQuality;
    public ActiveAbilityCatalog ActiveAbilities => activeAbilities;
    public UnitCatalog Units => units;
    public RelicCatalog Relics => relics;
    public SpecialTileCatalog SpecialTiles => specialTiles;
    public WaveTable DefaultWaveTable => defaultWaveTable;

    public static GameConfigRegistry LoadDefault()
    {
        return Resources.Load<GameConfigRegistry>("GameConfigRegistry");
    }
}
