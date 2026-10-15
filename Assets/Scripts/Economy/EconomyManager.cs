using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    [Header("Student Business")]
    public float cash = 20f;
    public float pricePerCandy = 2f;
    public float costPerCandy = 0.50f;

    [Header("Backpack Capacity")]
    public int backpackCapacity = 30;
    public int currentStock;

    [Header("School Market")]
    public float baseDemand = 25f;
    public float referencePrice = 2f;
    public float priceElasticity = 1.3f;

    [Header("Player Social Stats (0-100)")]
    public float popularity = 0f;   // PR
    public float gossip = 0f;       // Marketing

    [Header("Risk System")]
    public float riskLevel = 0f;          // 0–100
    public float confiscationThreshold = 75f;
    public float baseRiskPerTransaction = 1f;
    public float baseSnitchRate = 0.15f;  // 15% base chance

    [Header("Loyal Customers")]
    public int loyalCustomerCount = 0;
    public float loyalCustomerChance = 0.1f;   // 10% chance new buyer becomes loyal
    public int loyalUnitsMultiplier = 2;       // Loyal buy 2 units
    public float shortagePremiumMultiplier = 2f;

    [Header("Last Day Stats")]
    public float lastRevenue;
    public float lastProfit;
    public int lastUnitsSold;
    public int lastStudentsSoldTo;

    void Start()
    {
        Restock();
    }

    public void RunSchoolDay()
    {
        if (currentStock <= 0)
            return;

        float demand = CalculateDemand();

        int studentsSoldTo = 0;
        int unitsSold = 0;
        float revenue = 0f;

        float snitchRate = CalculateSnitchRate();
        int availableStock = currentStock;
        // 1️⃣ Loyal Customers First
        int loyalBuyersToday = Mathf.Min(loyalCustomerCount, (int)demand);

        for (int i = 0; i < loyalBuyersToday && availableStock > 0; i++)
        {
            int unitsBought = Mathf.Min(loyalUnitsMultiplier, availableStock);

            unitsSold += unitsBought;
            studentsSoldTo++;
            availableStock -= unitsBought;

            bool shortage = availableStock <= 0;
            float priceMultiplier = shortage ? shortagePremiumMultiplier : 1f;

            revenue += unitsBought * pricePerCandy * priceMultiplier;
        }
        // 2️⃣ Normal Students
        int remainingDemand = Mathf.Max(0, (int)demand - studentsSoldTo);

        for (int i = 0; i < remainingDemand && availableStock > 0; i++)
        {
            // Each student buys at least 1 unit
            unitsSold += 1;
            studentsSoldTo++;
            availableStock--;

            revenue += pricePerCandy;

            // Chance to convert to loyal
            if (Random.value < loyalCustomerChance)
            {
                loyalCustomerCount++;
            }

            // Risk check (only for non-loyal)
            if (Random.value < snitchRate)
            {
                riskLevel += baseRiskPerTransaction;
            }
        }
        // Finalize Day
        float cost = unitsSold * costPerCandy;
        float profit = revenue - cost;

        cash += profit;
        currentStock -= unitsSold;

        riskLevel = Mathf.Clamp(riskLevel, 0f, 100f);

        lastRevenue = revenue;
        lastProfit = profit;
        lastUnitsSold = unitsSold;
        lastStudentsSoldTo = studentsSoldTo;

        CheckConfiscation();
    }

    float CalculateDemand()
    {
        float priceRatio = pricePerCandy / referencePrice;
        float demand = baseDemand * Mathf.Pow(priceRatio, -priceElasticity);
        return Mathf.Clamp(demand, 0, backpackCapacity);
    }

    float CalculateSnitchRate()
    {
        // Popularity & Gossip reduce snitching
        float reduction = (popularity * 0.005f) + (gossip * 0.005f);
        float adjusted = baseSnitchRate - reduction;

        return Mathf.Clamp(adjusted, 0.01f, baseSnitchRate);
    }

    void CheckConfiscation()
    {
        if (riskLevel >= confiscationThreshold)
        {
            currentStock = 0;
            cash -= 10f;  // Fine
            riskLevel = 0f;
        }
    }

    public void Restock()
    {
        currentStock = backpackCapacity;
    }
}