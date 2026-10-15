using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public EconomyManager economyManager;
    public GameManager gameManager;

    [Header("UI Elements")]
    public TextMeshProUGUI dashboardText;
    public TextMeshProUGUI priceValueText;
    public Slider priceSlider;

    void Start()
    {
        // Auto-assign if not manually set
        if (economyManager == null)
            economyManager = FindObjectOfType<EconomyManager>();

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (priceSlider != null && economyManager != null)
        {
            priceSlider.value = economyManager.pricePerCandy;
            priceSlider.onValueChanged.AddListener(OnPriceChanged);
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (economyManager == null || gameManager == null)
            return;

        if (priceValueText != null)
            priceValueText.text = $"Price: £{economyManager.pricePerCandy:F2}";

        if (dashboardText != null)
        {
            dashboardText.text =
                $"Day: {gameManager.currentDay}\n" +
                $"Cash: £{economyManager.cash:F2}\n\n" +
                $"Stock: {economyManager.currentStock}/{economyManager.backpackCapacity}\n\n" +
                $"Revenue: £{economyManager.lastRevenue:F2}\n" +
                $"Profit: £{economyManager.lastProfit:F2}\n" +
                $"Units Sold: {economyManager.lastUnitsSold}\n\n" +
                $"Risk: {economyManager.riskLevel:F0}%";
        }
    }

    void OnPriceChanged(float newPrice)
    {
        if (economyManager == null) return;

        economyManager.pricePerCandy = newPrice;
        RefreshUI();
    }
}