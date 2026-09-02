# Unit catalog

- `UnitCatalog.asset` — roster for hub Deck Builder / loadout save
- Assigned on `GameConfigRegistry.units`
- Each `UnitData` needs a stable `unitId` (NamingMap)

## Deck card display (Deck Builder)

On each **Unit Data** asset (`Assets/Script/Unit/UnitData/*.asset`):

| Field | Shows on card |
|-------|----------------|
| **unitName** | `Deck_Name` / `Unit_Name` text (if that label exists on the prefab) |
| **icon** + **levelIcons[0..5]** | **Deck_Image** — main unit portrait (`GetDeckPortrait(level)`) |
| **tagIcon** | **Unit_Icon** — small class/role badge in the corner |
| Level in UI | **Deck_Level** — code writes `LVL 1` for MVP collection |

**tagIcon** sprites live in `Assets/GUI/.../Unit_Icon.png` / trait art per unit.  
**Portrait** sprites are usually from `Assets/Sprite/Deck_pi/...` (same as `levelIcons`).

After editing assets, re-open Team → Deck Builder; no code change needed.

Install / refresh: **Tools → Deck Builder → Ensure Unit Catalog + IDs**
