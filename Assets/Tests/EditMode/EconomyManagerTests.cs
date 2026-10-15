using NUnit.Framework;
using UnityEngine;

public class EconomyManagerTests
{
    private GameObject go;
    private EconomyManager economy;

    [SetUp]
    public void SetUp()
    {
        // Start() doesn't run in Edit Mode, so the backpack starts empty
        go = new GameObject("EconomyManager");
        economy = go.AddComponent<EconomyManager>();
        Random.InitState(12345);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Restock_FillsBackpackToCapacity()
    {
        economy.currentStock = 3;

        economy.Restock();

        Assert.AreEqual(economy.backpackCapacity, economy.currentStock);
    }

    [Test]
    public void RunSchoolDay_WithNoStock_ChangesNothing()
    {
        float cashBefore = economy.cash;

        economy.RunSchoolDay();

        Assert.AreEqual(cashBefore, economy.cash);
        Assert.AreEqual(0, economy.lastUnitsSold);
    }

    [Test]
    public void RunSchoolDay_AtReferencePrice_SellsBaseDemandAndBanksProfit()
    {
        economy.Restock();
        economy.pricePerCandy = economy.referencePrice;
        float cashBefore = economy.cash;

        economy.RunSchoolDay();

        // At the reference price demand equals baseDemand, capped by the backpack
        int expectedUnits = Mathf.Min((int)economy.baseDemand, economy.backpackCapacity);
        float expectedProfit = expectedUnits * (economy.pricePerCandy - economy.costPerCandy);
        Assert.AreEqual(expectedUnits, economy.lastUnitsSold);
        Assert.AreEqual(economy.backpackCapacity - expectedUnits, economy.currentStock);
        Assert.AreEqual(expectedProfit, economy.lastProfit, 0.001f);
        Assert.AreEqual(cashBefore + expectedProfit, economy.cash, 0.001f);
    }

    [Test]
    public void RunSchoolDay_HigherPrice_SellsFewerUnits()
    {
        economy.Restock();
        economy.pricePerCandy = economy.referencePrice;
        economy.RunSchoolDay();
        int unitsAtReferencePrice = economy.lastUnitsSold;

        economy.Restock();
        economy.loyalCustomerCount = 0;
        economy.pricePerCandy = economy.referencePrice * 2f;
        economy.RunSchoolDay();

        Assert.Less(economy.lastUnitsSold, unitsAtReferencePrice);
    }

    [Test]
    public void RunSchoolDay_RiskAtThreshold_ConfiscatesStockAndFines()
    {
        economy.Restock();
        economy.riskLevel = economy.confiscationThreshold;
        float cashBefore = economy.cash;

        economy.RunSchoolDay();

        Assert.AreEqual(0, economy.currentStock);
        Assert.AreEqual(0f, economy.riskLevel);
        Assert.AreEqual(cashBefore + economy.lastProfit - 10f, economy.cash, 0.001f);
    }
}
