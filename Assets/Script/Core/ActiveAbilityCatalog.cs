using UnityEngine;

/// <summary>
/// Ordered catalog of global actives available for unlock / loadout (M2 wires picker).
/// </summary>
[CreateAssetMenu(fileName = "ActiveAbilityCatalog", menuName = "Game/Abilities/Active Ability Catalog")]
public class ActiveAbilityCatalog : ScriptableObject
{
    public ActiveAbilityDefinition[] abilities;

    public ActiveAbilityDefinition FindById(string id)
    {
        if (abilities == null || string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < abilities.Length; i++)
        {
            ActiveAbilityDefinition def = abilities[i];
            if (def != null && def.id == id)
                return def;
        }

        return null;
    }
}
