# Global active abilities (Option A)

- Definitions: `Active_*.asset` (`ActiveAbilityDefinition`)
- Catalog: `ActiveAbilityCatalog.asset`
- Cast runtime: `GlobalActiveCastService` + `Assets/Script/Abilities/Handlers/`

## Launch pool (all `implemented = true`)

| Id | Targeting | Effect |
|----|-----------|--------|
| `active_mana_surge` | None | Grants mana (`power`) |
| `active_meteor_strike` | Point | AoE fire damage |
| `active_frost_nova` | Global | AoE frost damage + 40% slow |
| `active_execution_sigil` | Enemy | Single-target arcane damage; ×1.75 if target ≤30% HP |
| `active_radiant_cleanse` | Global | Pulse all towers + temporary damage buff (`power` = % bonus) |
| `active_arcane_overclock` | Global | Temporary attack-rate buff (`power` = % bonus) |

Create more via **Create → Game → Abilities → Active Ability Definition**.
