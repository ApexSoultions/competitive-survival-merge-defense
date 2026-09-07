using UnityEngine;

public class AccountLevelSmokeTest : MonoBehaviour
{
    public int value;
    void Start()
    {
        Debug.Log("[Account] before=" + AccountProgressService.GetAccountLevel()
            + " saved=" + AccountProgressService.HasSavedAccountLevel());
        AccountProgressService.SetAccountLevel(value);
        Debug.Log("[Account] after=" + AccountProgressService.GetAccountLevel());
    }
}