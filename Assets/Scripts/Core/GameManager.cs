using UnityEngine;

public class GameManager : MonoBehaviour
{
    public EconomyManager economyManager;
    public UIManager uiManager;

    public int currentDay = 1;
    public float dayLength = 5f; // seconds per school day
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= dayLength)
        {
            timer = 0f;
            RunDay();
        }
    }

    void RunDay()
    {
        if (economyManager == null)
        {
            Debug.LogError("EconomyManager not assigned!");
            return;
        }

        economyManager.RunSchoolDay();
        currentDay++;

        if (economyManager.currentStock <= 0)
        {
            economyManager.Restock();
        }

        if (uiManager != null)
            uiManager.RefreshUI();
    }
}