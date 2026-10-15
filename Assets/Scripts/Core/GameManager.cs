using UnityEngine;

public class GameManager : MonoBehaviour
{
    public EconomyManager economyManager;
    public UIManager uiManager;

    public int currentWeek = 1;
    public float weekLength = 5f; // seconds per week
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= weekLength)
        {
            timer = 0f;
            RunWeek();
        }
    }

    void RunWeek()
    {
        if (economyManager == null)
        {
            Debug.LogError("EconomyManager not assigned!");
            return;
        }

        economyManager.RunSchoolDay();
        currentWeek++;

        if (economyManager.currentStock <= 0)
        {
            economyManager.Restock();
        }

        if (uiManager != null)
            uiManager.RefreshUI();
    }
}