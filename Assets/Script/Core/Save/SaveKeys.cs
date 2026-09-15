namespace Game.Core.Save
{
    /// <summary>
    /// Known save keys. Prefer these constants over string literals.
    /// </summary>
    public static class SaveKeys
    {
        public const string MobileQualityTier = "MOBILE_QUALITY_TIER";

        // Reserved for Milestone 2 loadout persistence
        public const string LoadoutUnitIds = "LOADOUT_UNIT_IDS";
        public const string LoadoutActiveIds = "LOADOUT_ACTIVE_IDS";
        public const string LoadoutRelicId = "LOADOUT_RELIC_ID";
        public const string LoadoutSpecialTileId = "LOADOUT_SPECIAL_TILE_ID";

        // Phase 5 — account progression (legacy; ability unlocks are per-unit now)
        public const string AccountLevel = "ACCOUNT_LEVEL";

        // Client MVP — per-unit collection level (1–50) for L10/L20 ability unlocks
        public const string UnitLevels = "UNIT_LEVELS";

        // In-game ability test panel — session override for force-all tiers
        public const string AbilityTestForceAllOverride = "ABILITY_TEST_FORCE_ALL";
    }
}
