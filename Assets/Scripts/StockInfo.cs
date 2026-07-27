using UnityEngine;

[System.Serializable]
public class StockInfo
{
    public string Name;

    public enum StockType
    {
        Food,
        Drink,
        Chips,
        Candy
    }

    public StockType TypeOfStock;
}