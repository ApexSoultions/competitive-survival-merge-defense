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
    }
}
