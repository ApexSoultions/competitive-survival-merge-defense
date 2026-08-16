# Balance

Canonical asset: `Assets/Content/Balance/GameBalanceConfig.asset`  
Access: `GameServices.Instance.Config.GameBalance` (via `GameConfigRegistry` at `Assets/Content/Resources/GameConfigRegistry.asset`)

Do **not** duplicate this under `Assets/Resources/`. Change `startingMana`, summon costs, etc. here only.

Summon cost is battle state on `ManaManager` and resets on `BeginBattle` via `ResetMatchEconomy()`. It is not a wallet currency.
