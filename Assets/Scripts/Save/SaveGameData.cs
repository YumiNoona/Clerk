using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ProductSaveData
{
    public string ProductId;
    public long CurrentPriceCents;
}

[Serializable]
public sealed class ShelfSaveData
{
    public string ShelfId;
    public string ParentFurnitureId;
    public string RelativePath;
    public string ProductId;
    public int Quantity;
}

[Serializable]
public sealed class FurnitureSaveData
{
    public string FurnitureId;
    public string PurchaseId;
    public bool PurchasedInstance;
    public Vector3 Position;
    public Quaternion Rotation;
}

[Serializable]
public sealed class StockBoxSaveData
{
    public string BoxId;
    public string PurchaseId;
    public string ProductId;
    public int Quantity;
    public Vector3 Position;
    public Quaternion Rotation;
}

[Serializable]
public sealed class PlayerSaveData
{
    public Vector3 Position;
    public Quaternion Rotation;
}

[Serializable]
public sealed class EmployeeSaveData
{
    public string EmployeeId;
    public string EmployeeTypeId;
    public Vector3 Position;
    public Quaternion Rotation;
}

[Serializable]
public sealed class DaySaveData
{
    public int Day = 1;
    public float CurrentMinute;
    public bool IsRunning;
}

[Serializable]
public sealed class SaveGameData
{
    public const int CurrentVersion = 3;

    public int Version = CurrentVersion;
    public string SavedAtUtc;
    public long WalletCents;
    public PlayerSaveData Player =
        new PlayerSaveData();

    public DaySaveData Day =
        new DaySaveData();

    public List<ProductSaveData> Products =
        new List<ProductSaveData>();

    public List<ShelfSaveData> Shelves =
        new List<ShelfSaveData>();

    public List<FurnitureSaveData> Furniture =
        new List<FurnitureSaveData>();

    public List<StockBoxSaveData> StockBoxes =
        new List<StockBoxSaveData>();

    public List<EmployeeSaveData> Employees =
        new List<EmployeeSaveData>();

    public List<LedgerEntry> Ledger =
        new List<LedgerEntry>();

    public ProgressionData Progression =
        new ProgressionData();

    public StoreStatisticsData Statistics =
        new StoreStatisticsData();

    public FinanceData Finance =
        new FinanceData();

    public List<ObjectiveProgressData> Objectives =
        new List<ObjectiveProgressData>();
}
