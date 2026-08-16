using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual-only opponent header + board chrome for layout / mock parity.
/// Real opponent comparison / replay / grid gameplay is M4 — do not wire gameplay here.
/// </summary>
public class OpponentDummyHud : MonoBehaviour
{
    [SerializeField] private string displayName = "OPPONENT";
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image[] unitIcons;
    [SerializeField] private Image[] abilityIcons;

    private void Awake()
    {
        ApplyDisplayName();
    }

    private void OnEnable()
    {
        ApplyDisplayName();
    }

    private void ApplyDisplayName()
    {
        if (nameLabel != null)
            nameLabel.text = displayName;
    }
}
