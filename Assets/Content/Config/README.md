# Runtime config

| Asset | Role |
|-------|------|
| `../Resources/GameConfigRegistry.asset` | **Only** registry — refs all other configs (`Resources.Load`) |
| `SceneFlowConfig.asset` | Hub/Battle scene names |

Canonical registry path: `Assets/Content/Resources/GameConfigRegistry.asset`

Do **not** put `GameBalanceConfig`, `SceneFlowConfig`, or `MobileQualityCatalog` under `Assets/Resources/`.
Do **not** keep a second `GameConfigRegistry` anywhere else.
