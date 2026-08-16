# Mobile quality profiles

| Asset | Role |
|-------|------|
| `MobileQuality_Low.asset` | 30 FPS, reduced VFX |
| `MobileQuality_Mid.asset` | 60 FPS default |
| `MobileQuality_High.asset` | 60 FPS full juice caps |
| `MobileQualityCatalog.asset` | Auto-detect thresholds + profile refs |

Runtime load: `Resources/MobileQualityCatalog.asset` (keep in sync with this folder).

Applied by `MobileQualityService` on Bootstrap.
