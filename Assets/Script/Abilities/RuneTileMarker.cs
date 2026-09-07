using UnityEngine;

/// <summary>
/// Lightweight visual marker for Enchantress rune tiles on board cells.
/// </summary>
[DisallowMultipleComponent]
public sealed class RuneTileMarker : MonoBehaviour
{
    private SpriteRenderer glow;

    public static RuneTileMarker Ensure(TowerBoardCell cell)
    {
        RuneTileMarker marker = cell.GetComponent<RuneTileMarker>();
        if (marker == null)
            marker = cell.gameObject.AddComponent<RuneTileMarker>();
        return marker;
    }

    public void SetActive(bool active, Color color)
    {
        if (glow == null)
        {
            Transform existing = transform.Find("RuneGlow");
            GameObject glowObject = existing != null ? existing.gameObject : new GameObject("RuneGlow");
            if (existing == null)
            {
                glowObject.transform.SetParent(transform, false);
                glowObject.transform.localPosition = Vector3.zero;
                glowObject.transform.localScale = Vector3.one * 0.85f;
            }

            glow = glowObject.GetComponent<SpriteRenderer>();
            if (glow == null)
                glow = glowObject.AddComponent<SpriteRenderer>();
            glow.sortingLayerName = "Tower";
            glow.sortingOrder = 5;
        }

        glow.enabled = active;
        if (!active)
            return;

        glow.color = new Color(color.r, color.g, color.b, 0.35f);
        // 1x1 white sprite fallback via Unity default if no sprite assigned.
        if (glow.sprite == null)
            glow.sprite = CreateFallbackSprite();
    }

    private static Sprite CreateFallbackSprite()
    {
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
    }
}
