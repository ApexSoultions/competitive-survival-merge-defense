using UnityEngine;

/// <summary>
/// Lightweight sprite-frame player for towers.
/// Optimized: skips Update work for missing/static clips. Extensible via <see cref="UnitVisualState"/>.
/// Prefabs without this component keep a static SpriteRenderer (existing roster).
/// </summary>
[DisallowMultipleComponent]
public sealed class UnitVisualAnimator : MonoBehaviour
{
    [SerializeField] private UnitAnimationSet animationSet;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField, Min(0.1f)] private float defaultFramesPerSecond = 10f;

    private UnitVisualState currentState = UnitVisualState.Idle;
    private UnitAnimationClip activeClip;
    private int frameIndex;
    private float frameTimer;
    private bool playing;
    private bool returnToIdleWhenDone;
    private bool needsUpdate;

    public UnitVisualState CurrentState => currentState;
    public UnitAnimationSet AnimationSet => animationSet;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>(true);

        Play(UnitVisualState.Idle, forceRestart: true);
    }

    private void OnEnable()
    {
        if (activeClip.HasFrames)
            Play(currentState, forceRestart: true);
    }

    private void Update()
    {
        if (!needsUpdate || !playing || targetRenderer == null)
            return;

        float fps = activeClip.framesPerSecond > 0.1f ? activeClip.framesPerSecond : defaultFramesPerSecond;
        float frameDuration = 1f / fps;
        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            AdvanceFrame();
            if (!playing)
                break;
        }
    }

    public void SetAnimationSet(UnitAnimationSet set)
    {
        animationSet = set;
        Play(UnitVisualState.Idle, forceRestart: true);
    }

    public void PlayAttack()
    {
        Play(UnitVisualState.Attack);
    }

    public void Play(UnitVisualState state, bool forceRestart = false)
    {
        if (targetRenderer == null)
            return;

        if (!forceRestart && playing && currentState == state && activeClip.loop)
            return;

        UnitAnimationClip clip = default;
        if (animationSet != null)
            clip = animationSet.GetClip(state);

        if (!clip.HasFrames && state != UnitVisualState.Idle && animationSet != null)
            clip = animationSet.Idle;

        currentState = state;
        activeClip = clip;
        frameIndex = 0;
        frameTimer = 0f;
        returnToIdleWhenDone = !clip.loop && state != UnitVisualState.Idle && state != UnitVisualState.Death;

        if (!clip.HasFrames)
        {
            playing = false;
            needsUpdate = false;
            return;
        }

        ApplyFrame(0);
        playing = true;
        needsUpdate = !clip.IsStatic || !clip.loop;
        if (clip.IsStatic && clip.loop)
        {
            // Single looping frame: no per-frame work.
            needsUpdate = false;
        }
        else if (clip.IsStatic && !clip.loop)
        {
            // One-shot static: briefly show then return to idle next frame.
            needsUpdate = returnToIdleWhenDone;
        }
    }

    private void AdvanceFrame()
    {
        if (!activeClip.HasFrames)
        {
            playing = false;
            needsUpdate = false;
            return;
        }

        int next = frameIndex + 1;
        if (next >= activeClip.frames.Length)
        {
            if (activeClip.loop)
            {
                next = 0;
            }
            else if (returnToIdleWhenDone)
            {
                Play(UnitVisualState.Idle, forceRestart: true);
                return;
            }
            else
            {
                playing = false;
                needsUpdate = false;
                return;
            }
        }

        frameIndex = next;
        ApplyFrame(frameIndex);
    }

    private void ApplyFrame(int index)
    {
        if (targetRenderer == null || !activeClip.HasFrames)
            return;

        if (index < 0 || index >= activeClip.frames.Length)
            return;

        Sprite sprite = activeClip.frames[index];
        if (sprite != null)
            targetRenderer.sprite = sprite;
    }
}
