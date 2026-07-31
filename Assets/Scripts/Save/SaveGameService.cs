using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class SaveGameService : MonoBehaviour
{
    private const string SaveFolderName = "Saves";
    private const int MinimumSlot = 0;
    private const int MaximumSlot = 9;

    private PurchaseCatalog purchaseCatalog;

    public int CurrentSlot { get; private set; }

    public event Action<int> GameSaved;
    public event Action<int> GameLoaded;
    public event Action<string> SaveFailed;

    public void Configure(PurchaseCatalog catalog)
    {
        purchaseCatalog = catalog;
    }

    public bool Save(int slot)
    {
        slot = NormalizeSlot(slot);

        try
        {
            SaveGameData data = Capture();
            string json =
                JsonUtility.ToJson(data,true);

            string path = GetSavePath(slot);
            string directory =
                Path.GetDirectoryName(path);

            Directory.CreateDirectory(directory);

            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath,json);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporaryPath,path);
            CurrentSlot = slot;
            GameSaved?.Invoke(slot);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception,this);
            SaveFailed?.Invoke(exception.Message);
            return false;
        }
    }

    public bool Load(int slot)
    {
        slot = NormalizeSlot(slot);
        string path = GetSavePath(slot);

        if (!File.Exists(path))
        {
            SaveFailed?.Invoke(
                "Save slot " + slot + " is empty.");

            return false;
        }

        try
        {
            string json = File.ReadAllText(path);

            SaveGameData data =
                JsonUtility.FromJson<SaveGameData>(
                    json);

            if (data == null)
            {
                SaveFailed?.Invoke(
                    "Save data could not be read.");

                return false;
            }

            Restore(data);
            CurrentSlot = slot;
            GameLoaded?.Invoke(slot);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception,this);
            SaveFailed?.Invoke(exception.Message);
            return false;
        }
    }

    public bool SaveExists(int slot)
    {
        return File.Exists(
            GetSavePath(NormalizeSlot(slot)));
    }

    public bool DeleteSave(int slot)
    {
        string path =
            GetSavePath(NormalizeSlot(slot));

        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    public SaveGameData Capture()
    {
        SaveGameData data = new SaveGameData
        {
            SavedAtUtc =
                DateTime.UtcNow.ToString("O"),
            WalletCents =
                WalletController.Instance != null
                    ? WalletController.Instance
                        .Balance.MinorUnits
                    : 0
        };

        CapturePlayer(data);
        CaptureDay(data);
        CaptureProducts(data);
        CaptureShelves(data);
        CaptureFurniture(data);
        CaptureStockBoxes(data);
        CaptureEmployees(data);

        if (GameBootstrap.Instance != null)
        {
            data.Ledger.AddRange(
                GameBootstrap.Instance.Economy.Entries);

            data.Progression =
                GameBootstrap.Instance.Progression
                    .Capture();

            data.Statistics =
                GameBootstrap.Instance.Statistics.Data;

            data.Finance =
                GameBootstrap.Instance.Finance
                    .Capture();

            data.Objectives =
                GameBootstrap.Instance.Objectives
                    .Capture();
        }

        return data;
    }

    private void Restore(SaveGameData data)
    {
        if (WalletController.Instance != null)
        {
            WalletController.Instance.SetBalance(
                new Money(data.WalletCents));
        }

        if (GameBootstrap.Instance == null)
        {
            return;
        }

        GameBootstrap.Instance.Economy
            .RestoreEntries(data.Ledger);

        GameBootstrap.Instance.Progression
            .Restore(data.Progression);

        GameBootstrap.Instance.Statistics
            .Restore(data.Statistics);

        GameBootstrap.Instance.Finance
            .Restore(data.Finance);

        GameBootstrap.Instance.Objectives
            .Restore(data.Objectives);

        GameBootstrap.Instance.Days.Restore(
            data.Day.Day,
            data.Day.CurrentMinute,
            data.Day.IsRunning);

        RestoreProducts(data.Products);
        RestoreFurniture(data.Furniture);
        RestoreShelves(data.Shelves);
        RestoreStockBoxes(data.StockBoxes);
        RestoreEmployees(data.Employees);
        RestorePlayer(data.Player);
    }

    private static void CapturePlayer(
        SaveGameData data)
    {
        PlayerController player =
            FindAnyObjectByType<PlayerController>();

        if (player == null)
        {
            return;
        }

        data.Player.Position =
            player.transform.position;

        data.Player.Rotation =
            player.transform.rotation;
    }

    private static void CaptureDay(
        SaveGameData data)
    {
        if (GameBootstrap.Instance == null)
        {
            return;
        }

        StoreDayController day =
            GameBootstrap.Instance.Days;

        data.Day.Day = day.CurrentDay;
        data.Day.CurrentMinute = day.CurrentMinute;
        data.Day.IsRunning = day.IsDayRunning;
    }

    private static void CaptureProducts(
        SaveGameData data)
    {
        if (GameBootstrap.Instance == null)
        {
            return;
        }

        foreach (ProductState state in
                 GameBootstrap.Instance.Products.States)
        {
            data.Products.Add(
                new ProductSaveData
                {
                    ProductId = state.ProductId,
                    CurrentPriceCents =
                        Money.FromFloat(
                            state.CurrentPrice)
                            .MinorUnits
                });
        }
    }

    private static void CaptureShelves(
        SaveGameData data)
    {
        ShelfSpaceController[] shelves =
            FindObjectsByType<ShelfSpaceController>(
                FindObjectsInactive.Include);

        for (int i = 0; i < shelves.Length; i++)
        {
            ShelfSpaceController shelf = shelves[i];
            PlaceableFurniture parentFurniture =
                shelf.GetComponentInParent<
                    PlaceableFurniture>();

            data.Shelves.Add(
                new ShelfSaveData
                {
                    ShelfId = shelf.ShelfId,
                    ParentFurnitureId =
                        parentFurniture != null
                            ? parentFurniture.FurnitureId
                            : string.Empty,
                    RelativePath =
                        parentFurniture != null
                            ? GetRelativePath(
                                parentFurniture.transform,
                                shelf.transform)
                            : string.Empty,
                    ProductId =
                        shelf.Info != null
                            ? shelf.Info.ProductId
                            : string.Empty,
                    Quantity = shelf.StockCount
                });
        }
    }

    private static void CaptureFurniture(
        SaveGameData data)
    {
        PlaceableFurniture[] furniture =
            FindObjectsByType<PlaceableFurniture>(
                FindObjectsInactive.Include);

        for (int i = 0; i < furniture.Length; i++)
        {
            PlaceableFurniture item = furniture[i];

            data.Furniture.Add(
                new FurnitureSaveData
                {
                    FurnitureId = item.FurnitureId,
                    PurchaseId = item.PurchaseId,
                    PurchasedInstance =
                        item.IsPurchasedInstance,
                    Position =
                        item.transform.position,
                    Rotation =
                        item.transform.rotation
                });
        }
    }

    private static void CaptureStockBoxes(
        SaveGameData data)
    {
        StockBoxController[] boxes =
            FindObjectsByType<StockBoxController>(
                FindObjectsInactive.Include);

        for (int i = 0; i < boxes.Length; i++)
        {
            StockBoxController box = boxes[i];

            data.StockBoxes.Add(
                new StockBoxSaveData
                {
                    BoxId = box.BoxId,
                    PurchaseId = box.PurchaseId,
                    ProductId =
                        box.Product != null
                            ? box.Product.ProductId
                            : string.Empty,
                    Quantity = box.Quantity,
                    Position = box.transform.position,
                    Rotation = box.transform.rotation
                });
        }
    }

    private static void CaptureEmployees(
        SaveGameData data)
    {
        EmployeeContext[] employees =
            FindObjectsByType<EmployeeContext>(
                FindObjectsInactive.Include);

        for (int i = 0;
             i < employees.Length;
             i++)
        {
            EmployeeContext employee = employees[i];

            if (employee.Definition == null)
            {
                continue;
            }

            data.Employees.Add(
                new EmployeeSaveData
                {
                    EmployeeId =
                        employee.EmployeeId,
                    EmployeeTypeId =
                        employee.Definition
                            .EmployeeTypeId,
                    Position =
                        employee.transform.position,
                    Rotation =
                        employee.transform.rotation
                });
        }
    }

    private void RestoreProducts(
        IReadOnlyList<ProductSaveData> products)
    {
        if (products == null)
        {
            return;
        }

        for (int i = 0; i < products.Count; i++)
        {
            ProductSaveData saved = products[i];
            StockInfo product =
                ResolveProduct(saved.ProductId);

            if (product != null)
            {
                GameBootstrap.Instance.Products
                    .TrySetPrice(
                        product,
                        new Money(
                            saved.CurrentPriceCents)
                            .AsFloat);
            }
        }
    }

    private void RestoreShelves(
        IReadOnlyList<ShelfSaveData> savedShelves)
    {
        if (savedShelves == null)
        {
            return;
        }

        ShelfSpaceController[] shelves =
            FindObjectsByType<ShelfSpaceController>(
                FindObjectsInactive.Include);

        Dictionary<string,ShelfSpaceController> byId =
            new Dictionary<string,ShelfSpaceController>();

        for (int i = 0; i < shelves.Length; i++)
        {
            byId[shelves[i].ShelfId] = shelves[i];
        }

        for (int i = 0; i < savedShelves.Count; i++)
        {
            ShelfSaveData saved = savedShelves[i];
            ShelfSpaceController shelf = null;

            if (!string.IsNullOrWhiteSpace(
                    saved.ParentFurnitureId) &&
                !string.IsNullOrWhiteSpace(
                    saved.RelativePath))
            {
                PlaceableFurniture parent =
                    FindFurnitureById(
                        saved.ParentFurnitureId);

                Transform nested =
                    parent != null
                        ? FindRelativePath(
                            parent.transform,
                            saved.RelativePath)
                        : null;

                shelf =
                    nested != null
                        ? nested.GetComponent<
                            ShelfSpaceController>()
                        : null;
            }

            if (shelf == null)
            {
                byId.TryGetValue(
                    saved.ShelfId,
                    out shelf);
            }

            if (shelf != null)
            {
                shelf.RestoreInventory(
                    ResolveProduct(saved.ProductId),
                    saved.Quantity);
            }
        }
    }

    private void RestoreFurniture(
        IReadOnlyList<FurnitureSaveData> savedFurniture)
    {
        if (savedFurniture == null)
        {
            return;
        }

        PlaceableFurniture[] existing =
            FindObjectsByType<PlaceableFurniture>(
                FindObjectsInactive.Include);

        Dictionary<string,PlaceableFurniture> byId =
            new Dictionary<string,PlaceableFurniture>();

        for (int i = 0; i < existing.Length; i++)
        {
            byId[existing[i].FurnitureId] =
                existing[i];
        }

        HashSet<string> savedIds =
            new HashSet<string>();

        for (int i = 0;
             i < savedFurniture.Count;
             i++)
        {
            savedIds.Add(
                savedFurniture[i].FurnitureId);
        }

        for (int i = 0; i < existing.Length; i++)
        {
            PlaceableFurniture item = existing[i];

            if (item != null &&
                item.IsPurchasedInstance &&
                !savedIds.Contains(item.FurnitureId))
            {
                Destroy(item.gameObject);
            }
        }

        for (int i = 0;
             i < savedFurniture.Count;
             i++)
        {
            FurnitureSaveData saved =
                savedFurniture[i];

            if (!byId.TryGetValue(
                    saved.FurnitureId,
                    out PlaceableFurniture item))
            {
                FurniturePurchaseData purchase =
                    ResolveFurniturePurchase(
                        saved.PurchaseId);

                if (purchase == null ||
                    purchase.FurniturePrefab == null)
                {
                    continue;
                }

                item = Instantiate(
                    purchase.FurniturePrefab,
                    saved.Position,
                    saved.Rotation);
            }

            item.transform.SetPositionAndRotation(
                saved.Position,
                saved.Rotation);

            item.RestoreIdentity(
                saved.FurnitureId,
                saved.PurchaseId,
                saved.PurchasedInstance);
        }
    }

    private void RestoreStockBoxes(
        IReadOnlyList<StockBoxSaveData> savedBoxes)
    {
        if (savedBoxes == null)
        {
            return;
        }

        StockBoxController[] existing =
            FindObjectsByType<StockBoxController>(
                FindObjectsInactive.Include);

        Dictionary<string,StockBoxController> byId =
            new Dictionary<string,StockBoxController>();

        for (int i = 0; i < existing.Length; i++)
        {
            byId[existing[i].BoxId] = existing[i];
        }

        HashSet<string> savedIds =
            new HashSet<string>();

        for (int i = 0; i < savedBoxes.Count; i++)
        {
            savedIds.Add(savedBoxes[i].BoxId);
        }

        for (int i = 0; i < existing.Length; i++)
        {
            StockBoxController box = existing[i];

            if (box != null &&
                !string.IsNullOrWhiteSpace(
                    box.PurchaseId) &&
                !savedIds.Contains(box.BoxId))
            {
                Destroy(box.gameObject);
            }
        }

        for (int i = 0; i < savedBoxes.Count; i++)
        {
            StockBoxSaveData saved = savedBoxes[i];

            if (!byId.TryGetValue(
                    saved.BoxId,
                    out StockBoxController box))
            {
                StockPurchaseData purchase =
                    ResolveStockPurchase(
                        saved.PurchaseId);

                if (purchase == null ||
                    purchase.BoxPrefab == null)
                {
                    continue;
                }

                box = Instantiate(
                    purchase.BoxPrefab,
                    saved.Position,
                    saved.Rotation);
            }

            StockInfo product =
                ResolveProduct(saved.ProductId);

            box.transform.SetPositionAndRotation(
                saved.Position,
                saved.Rotation);

            box.RestoreState(
                product,
                product != null
                    ? product.DefaultBoxLayout
                    : null,
                saved.Quantity,
                saved.BoxId,
                saved.PurchaseId);
        }
    }

    private static void RestorePlayer(
        PlayerSaveData saved)
    {
        if (saved == null)
        {
            return;
        }

        PlayerController player =
            FindAnyObjectByType<PlayerController>();

        if (player == null)
        {
            return;
        }

        CharacterController controller =
            player.CharacterController;

        bool wasEnabled =
            controller != null &&
            controller.enabled;

        if (controller != null)
        {
            controller.enabled = false;
        }

        player.transform.SetPositionAndRotation(
            saved.Position,
            saved.Rotation);

        if (controller != null)
        {
            controller.enabled = wasEnabled;
        }
    }

    private void RestoreEmployees(
        IReadOnlyList<EmployeeSaveData>
            savedEmployees)
    {
        savedEmployees ??=
            Array.Empty<EmployeeSaveData>();

        EmployeeContext[] existing =
            FindObjectsByType<EmployeeContext>(
                FindObjectsInactive.Include);

        Dictionary<string,EmployeeContext> byId =
            new Dictionary<string,EmployeeContext>();

        for (int i = 0; i < existing.Length; i++)
        {
            byId[existing[i].EmployeeId] =
                existing[i];
        }

        HashSet<string> savedIds =
            new HashSet<string>();

        for (int i = 0;
             i < savedEmployees.Count;
             i++)
        {
            EmployeeSaveData saved =
                savedEmployees[i];

            savedIds.Add(saved.EmployeeId);

            if (!byId.TryGetValue(
                    saved.EmployeeId,
                    out EmployeeContext employee))
            {
                EmployeeDefinition definition =
                    ResolveEmployeeDefinition(
                        saved.EmployeeTypeId);

                if (definition == null ||
                    definition.Prefab == null)
                {
                    continue;
                }

                GameObject instance =
                    Instantiate(
                        definition.Prefab,
                        saved.Position,
                        saved.Rotation);

                employee =
                    instance.GetComponent<
                        EmployeeContext>() ??
                    instance.AddComponent<
                        EmployeeContext>();

                employee.Initialize(
                    definition,
                    saved.EmployeeId);

                if (definition.Role ==
                        EmployeeRole.Restocker &&
                    instance.GetComponent<
                        RestockEmployeeBrain>() ==
                    null)
                {
                    instance.AddComponent<
                        RestockEmployeeBrain>();
                }
            }

            employee.transform.SetPositionAndRotation(
                saved.Position,
                saved.Rotation);
        }

        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null &&
                !savedIds.Contains(
                    existing[i].EmployeeId))
            {
                Destroy(existing[i].gameObject);
            }
        }
    }

    private static PlaceableFurniture
        FindFurnitureById(string furnitureId)
    {
        PlaceableFurniture[] furniture =
            FindObjectsByType<PlaceableFurniture>(
                FindObjectsInactive.Include);

        for (int i = 0; i < furniture.Length; i++)
        {
            if (furniture[i].FurnitureId ==
                furnitureId)
            {
                return furniture[i];
            }
        }

        return null;
    }

    private static string GetRelativePath(
        Transform root,
        Transform child)
    {
        if (root == null ||
            child == null ||
            child == root)
        {
            return string.Empty;
        }

        List<int> indices = new List<int>();
        Transform current = child;

        while (current != null &&
               current != root)
        {
            indices.Add(current.GetSiblingIndex());
            current = current.parent;
        }

        if (current != root)
        {
            return string.Empty;
        }

        indices.Reverse();
        return string.Join(".",indices);
    }

    private static Transform FindRelativePath(
        Transform root,
        string path)
    {
        if (root == null ||
            string.IsNullOrWhiteSpace(path))
        {
            return root;
        }

        string[] segments = path.Split('.');
        Transform current = root;

        for (int i = 0; i < segments.Length; i++)
        {
            if (!int.TryParse(
                    segments[i],
                    out int childIndex) ||
                childIndex < 0 ||
                childIndex >= current.childCount)
            {
                return null;
            }

            current =
                current.GetChild(childIndex);
        }

        return current;
    }

    private StockInfo ResolveProduct(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId) ||
            purchaseCatalog == null ||
            purchaseCatalog.StockPurchases == null)
        {
            return null;
        }

        for (int i = 0;
             i < purchaseCatalog.StockPurchases.Count;
             i++)
        {
            StockPurchaseData purchase =
                purchaseCatalog.StockPurchases[i];

            if (purchase != null &&
                purchase.Product != null &&
                purchase.Product.ProductId == productId)
            {
                return purchase.Product;
            }
        }

        return null;
    }

    private static EmployeeDefinition
        ResolveEmployeeDefinition(string employeeTypeId)
    {
        EmployeeDefinition[] catalog =
            PurchaseService.Instance != null
                ? PurchaseService.Instance
                    .EmployeeCatalog
                : null;

        if (catalog == null)
        {
            return null;
        }

        for (int i = 0; i < catalog.Length; i++)
        {
            EmployeeDefinition definition =
                catalog[i];

            if (definition != null &&
                definition.EmployeeTypeId ==
                employeeTypeId)
            {
                return definition;
            }
        }

        return null;
    }

    private FurniturePurchaseData
        ResolveFurniturePurchase(string purchaseId)
    {
        if (string.IsNullOrWhiteSpace(purchaseId) ||
            purchaseCatalog == null ||
            purchaseCatalog.FurniturePurchases == null)
        {
            return null;
        }

        for (int i = 0;
             i < purchaseCatalog.FurniturePurchases.Count;
             i++)
        {
            FurniturePurchaseData purchase =
                purchaseCatalog.FurniturePurchases[i];

            if (purchase != null &&
                purchase.PurchaseId == purchaseId)
            {
                return purchase;
            }
        }

        return null;
    }

    private StockPurchaseData
        ResolveStockPurchase(string purchaseId)
    {
        if (string.IsNullOrWhiteSpace(purchaseId) ||
            purchaseCatalog == null ||
            purchaseCatalog.StockPurchases == null)
        {
            return null;
        }

        for (int i = 0;
             i < purchaseCatalog.StockPurchases.Count;
             i++)
        {
            StockPurchaseData purchase =
                purchaseCatalog.StockPurchases[i];

            if (purchase != null &&
                purchase.PurchaseId == purchaseId)
            {
                return purchase;
            }
        }

        return null;
    }

    private static int NormalizeSlot(int slot)
    {
        return Mathf.Clamp(
            slot,
            MinimumSlot,
            MaximumSlot);
    }

    private static string GetSavePath(int slot)
    {
        return Path.Combine(
            Application.persistentDataPath,
            SaveFolderName,
            "slot_" + slot + ".json");
    }
}
