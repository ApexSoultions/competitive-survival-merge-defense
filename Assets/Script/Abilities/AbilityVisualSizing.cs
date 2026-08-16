using UnityEngine;

public static class AbilityVisualSizing
{
    public static Bounds GetBounds(BoardTower tower, Transform fallback)
    {
        if (tower != null && tower.SpriteRenderer != null && tower.SpriteRenderer.sprite != null)
            return tower.SpriteRenderer.bounds;

        Vector3 position = fallback != null ? fallback.position : Vector3.zero;
        return new Bounds(position, Vector3.one);
    }

    public static float GetCharacterScale(BoardTower tower, Transform fallback, float referenceSize = 1f)
    {
        Bounds bounds = GetBounds(tower, fallback);
        float visualSize = Mathf.Max(bounds.size.x, bounds.size.y);
        return Mathf.Clamp(visualSize / Mathf.Max(0.01f, referenceSize), 0.35f, 3f);
    }

    public static Vector3 GetEffectAnchor(BoardTower tower, Transform fallback, float normalizedHeight)
    {
        Bounds bounds = GetBounds(tower, fallback);
        return new Vector3(
            bounds.center.x,
            Mathf.Lerp(bounds.min.y, bounds.max.y, Mathf.Clamp01(normalizedHeight)),
            fallback != null ? fallback.position.z : bounds.center.z);
    }

    public static Vector3 GetLocalScaleForWorldSize(Transform parent, Sprite sprite, float desiredWorldSize)
    {
        if (sprite == null)
            return Vector3.one * Mathf.Max(0.01f, desiredWorldSize);

        float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
        float parentWorldScale = Mathf.Max(0.001f, Mathf.Max(Mathf.Abs(parentScale.x), Mathf.Abs(parentScale.y)));
        float localScale = Mathf.Max(0.01f, desiredWorldSize) / Mathf.Max(0.001f, spriteSize * parentWorldScale);
        return Vector3.one * localScale;
    }
}
