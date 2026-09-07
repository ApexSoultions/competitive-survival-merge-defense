using System;
using UnityEngine;

/// <summary>
/// Visual animation states shared by all animated towers.
/// Add new values at the end to keep serialized data stable.
/// </summary>
public enum UnitVisualState
{
    Idle = 0,
    Attack = 1,
    Death = 2,
    Hit = 3,
    Cast = 4,
    Special = 5
}

[Serializable]
public struct UnitAnimationClip
{
    public Sprite[] frames;
    [Min(0.1f)] public float framesPerSecond;
    public bool loop;

    public bool HasFrames => frames != null && frames.Length > 0;
    public bool IsStatic => !HasFrames || frames.Length == 1;

    public float DurationSeconds
    {
        get
        {
            if (!HasFrames || framesPerSecond <= 0.1f)
                return 0f;
            return frames.Length / framesPerSecond;
        }
    }

    public static UnitAnimationClip Create(Sprite[] sprites, float fps, bool shouldLoop)
    {
        return new UnitAnimationClip
        {
            frames = sprites,
            framesPerSecond = Mathf.Max(0.1f, fps),
            loop = shouldLoop
        };
    }
}
