using UnityEngine;

/// <summary>
/// Content-driven clip table for a unit's visual states.
/// Missing clips fall back to Idle (or leave the current sprite unchanged).
/// </summary>
[CreateAssetMenu(menuName = "Game/Units/Animation Set", fileName = "UnitAnimationSet")]
public sealed class UnitAnimationSet : ScriptableObject
{
    [SerializeField] private UnitAnimationClip idle;
    [SerializeField] private UnitAnimationClip attack;
    [SerializeField] private UnitAnimationClip death;
    [SerializeField] private UnitAnimationClip hit;
    [SerializeField] private UnitAnimationClip cast;
    [SerializeField] private UnitAnimationClip special;

    public UnitAnimationClip Idle => idle;
    public UnitAnimationClip Attack => attack;
    public UnitAnimationClip Death => death;
    public UnitAnimationClip Hit => hit;
    public UnitAnimationClip Cast => cast;
    public UnitAnimationClip Special => special;

    public bool TryGetClip(UnitVisualState state, out UnitAnimationClip clip)
    {
        clip = GetClip(state);
        return clip.HasFrames;
    }

    public UnitAnimationClip GetClip(UnitVisualState state)
    {
        switch (state)
        {
            case UnitVisualState.Attack:
                return attack;
            case UnitVisualState.Death:
                return death;
            case UnitVisualState.Hit:
                return hit;
            case UnitVisualState.Cast:
                return cast;
            case UnitVisualState.Special:
                return special;
            case UnitVisualState.Idle:
            default:
                return idle;
        }
    }

#if UNITY_EDITOR
    public void EditorSetClip(UnitVisualState state, UnitAnimationClip clip)
    {
        switch (state)
        {
            case UnitVisualState.Attack:
                attack = clip;
                break;
            case UnitVisualState.Death:
                death = clip;
                break;
            case UnitVisualState.Hit:
                hit = clip;
                break;
            case UnitVisualState.Cast:
                cast = clip;
                break;
            case UnitVisualState.Special:
                special = clip;
                break;
            default:
                idle = clip;
                break;
        }
    }
#endif
}
