using UnityEngine;

public sealed class ManaSurgeCastHandler : IGlobalActiveCastHandler
{
    public const string Id = "active_mana_surge";

    public string AbilityId => Id;

    public bool TryExecute(ActiveAbilityDefinition definition, GlobalActiveCastContext context)
    {
        if (definition == null)
            return false;

        ManaManager mana = ManaManager.Instance;
        if (mana == null)
        {
            Debug.LogWarning("[ManaSurgeCastHandler] ManaManager missing.");
            return false;
        }

        int grant = Mathf.Max(0, Mathf.RoundToInt(definition.power));
        if (grant <= 0)
            return false;

        mana.AddMana(grant);
        Debug.Log("[ManaSurgeCastHandler] Granted " + grant + " mana.");
        return true;
    }
}
