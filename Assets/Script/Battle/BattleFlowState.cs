using System;
using UnityEngine;

public enum BattlePhase
{
    CharacterSelection,
    Active,
    Ended
}

/// <summary>
/// Single source of truth for whether gameplay input and simulation are allowed.
/// This intentionally does not depend on time scale: UI callbacks still run while
/// the character-selection screen has paused the game.
/// </summary>
public static class BattleFlowState
{
    public static event Action<BattlePhase> PhaseChanged;

    public static BattlePhase Phase { get; private set; } = BattlePhase.CharacterSelection;
    public static bool IsGameplayActive => Phase == BattlePhase.Active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        Phase = BattlePhase.CharacterSelection;
        PhaseChanged = null;
    }

    public static void EnterCharacterSelection()
    {
        SetPhase(BattlePhase.CharacterSelection);
    }

    public static void BeginBattle()
    {
        if (Phase == BattlePhase.Active)
            return;

        SetPhase(BattlePhase.Active);
        if (ManaManager.Instance != null)
            ManaManager.Instance.ResetMatchEconomy();
        GameplayEvents.RaiseBattleStarted();
    }

    public static void EndBattle()
    {
        if (Phase == BattlePhase.Ended)
            return;

        SetPhase(BattlePhase.Ended);
        GameplayEvents.RaiseBattleEnded();
    }

    private static void SetPhase(BattlePhase phase)
    {
        if (Phase == phase)
            return;

        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }
}
