using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StreetSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Street.unity";
    [MenuItem("Clerk/Setup/Configure Street Scene")]
    public static void ConfigureStreetScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("Open and save Assets/Scenes/Street.unity before running the Street setup.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure Street Scene");

        Transform player = Require("Gameplay/Player");
        Transform cameraTransform = Require("Gameplay/Player/Main Camera");
        Transform furniturePoint = Require("Gameplay/Player/Main Camera/FurniturePoint");
        Transform mobilePoint = cameraTransform != null
            ? GetOrCreatePoint(
                cameraTransform,
                "MobilePoint",
                new Vector3(0.32f,-0.28f,0.65f),
                new Vector3(10f,0f,0f))
            : null;
        Transform stockPoint = Require("Gameplay/Delivery Points/Stock Delivery Point");
        Transform furnitureDeliveryPoint = Require("Gameplay/Delivery Points/Furniture Delivery Point");
        Transform purchaseObject = Require("Gameplay/Scene Systems/Purchase Service");
        Transform placementControllerObject = Require("Gameplay/Scene Systems/Furniture Placement Controller");
        Transform placementAreaObject = Require("Store/Zones/Furniture Placement Area");
        Transform entrance = Require("Store/Navigation/Customer Flow/Entrances/Main Entrance");
        Transform insidePoint = Require("Store/Navigation/Customer Flow/Entrances/Main Entrance/Inside Point");
        Transform exit = Require("Store/Navigation/Customer Flow/Exits/Main Exit");
        Transform despawnPoint = Require("Store/Navigation/Customer Flow/Exits/Main Exit/Despawn Point");

        if (new[] { player, cameraTransform, furniturePoint, stockPoint,
                furnitureDeliveryPoint, purchaseObject,
                placementControllerObject,
                placementAreaObject, entrance, insidePoint, exit, despawnPoint }
            .Any(item => item == null))
        {
            Debug.LogError("Street setup stopped because one or more required hierarchy objects are missing.");
            return;
        }

        StockDeliveryService stockService = GetOrAdd<StockDeliveryService>(purchaseObject.gameObject);
        stockService.DeliverySpawnPoint = stockPoint;

        FurnitureDeliveryService furnitureService = GetOrAdd<FurnitureDeliveryService>(purchaseObject.gameObject);
        furnitureService.FurnitureSpawnPoint = furnitureDeliveryPoint;

        FurniturePlacementArea placementArea = GetOrAdd<FurniturePlacementArea>(placementAreaObject.gameObject);
        BoxCollider areaCollider = GetOrAdd<BoxCollider>(placementAreaObject.gameObject);
        areaCollider.isTrigger = true;
        placementArea.AreaCollider = areaCollider;

        FurniturePlacementController placementController =
            GetOrAdd<FurniturePlacementController>(placementControllerObject.gameObject);
        placementController.PlayerCamera = cameraTransform.GetComponent<Camera>();
        placementController.FurnitureHoldPoint = furniturePoint;
        placementController.PlacementArea = placementArea;
        placementController.PlacementSurfaceMask = LayerMask.GetMask("Furniture Placement Surface");
        placementController.PlacementBlockingMask = LayerMask.GetMask("Furniture Placement Blocker");
        placementController.MaximumPlacementDistance = 8f;

        PlayerInteractionController interactionController =
            GetOrAdd<PlayerInteractionController>(player.gameObject);
        interactionController.TheCamera =
            cameraTransform.GetComponent<Camera>();
        interactionController.MobileHoldPoint = mobilePoint;

        interactionController.InteractionMask = LayerMask.GetMask(
            "Stock",
            "Shelf",
            "Stock Box",
            "Garbage Bin",
            "Furniture");

        foreach (string path in new[] {
                     "Store/Navigation/Customer Flow/Spawn Points/Customer Spawn 01",
                     "Store/Navigation/Customer Flow/Spawn Points/Customer Spawn 02" })
        {
            Transform spawn = Require(path);
            if (spawn != null)
            {
                GetOrAdd<CustomerSpawnPoint>(spawn.gameObject);
            }
        }

        CustomerEntrancePoint entranceComponent = GetOrAdd<CustomerEntrancePoint>(entrance.gameObject);
        SetObjectReference(entranceComponent, "insidePoint", insidePoint);

        Transform exitsGroup = Find("Store/Navigation/Customer Flow/Exits");
        if (exitsGroup != null)
        {
            CustomerExitPoint misplacedExit = exitsGroup.GetComponent<CustomerExitPoint>();
            if (misplacedExit != null)
            {
                Undo.DestroyObjectImmediate(misplacedExit);
            }
        }

        CustomerExitPoint exitComponent = GetOrAdd<CustomerExitPoint>(exit.gameObject);
        SetObjectReference(exitComponent, "despawnPoint", despawnPoint);

        PurchaseService purchaseService = GetOrAdd<PurchaseService>(purchaseObject.gameObject);
        purchaseService.StockDeliveryService = stockService;
        purchaseService.FurnitureDeliveryService = furnitureService;
        purchaseService.PurchaseCatalog = Load<PurchaseCatalog>("Assets/Data/Purchase/Main Purchase Catalog.asset");
        purchaseService.CustomerDatabase = Load<CustomerDatabase>("Assets/Data/Customer/Customer Database.asset");
        purchaseService.StartingObjectives = new[] {
            Load<ObjectiveDefinition>("Assets/Data/Objectives/01 Order Stock.asset"),
            Load<ObjectiveDefinition>("Assets/Data/Objectives/02 Serve Customers.asset"),
            Load<ObjectiveDefinition>("Assets/Data/Objectives/03 Sell Items.asset"),
            Load<ObjectiveDefinition>("Assets/Data/Objectives/04 Earn Revenue.asset")
        };
        purchaseService.EmployeeCatalog = new[] {
            Load<EmployeeDefinition>("Assets/Data/Employees/Restocker.asset")
        };
        purchaseService.MobileModel = Load<GameObject>("Assets/Models/UI/Mobile.fbx");
        purchaseService.CheckoutModel = Load<GameObject>("Assets/Prefabs/Furniture/Checkout Counters.prefab");
        purchaseService.CreateStarterCheckout = true;

        ConfigureFurniture("Store/Furniture/Short Shelf", false);
        ConfigureFurniture("Store/Furniture/Checkout Counters", true);

        EditorUtility.SetDirty(stockService);
        EditorUtility.SetDirty(furnitureService);
        EditorUtility.SetDirty(placementArea);
        EditorUtility.SetDirty(placementController);
        EditorUtility.SetDirty(interactionController);
        EditorUtility.SetDirty(entranceComponent);
        EditorUtility.SetDirty(exitComponent);
        EditorUtility.SetDirty(purchaseService);

        EnsureStreetIsBuildScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log("Clerk Street setup complete. Services, data, customer route, placement, shelf, checkout, and build scene are configured.");
    }

    private static void ConfigureFurniture(string path, bool checkout)
    {
        Transform root = Require(path);
        if (root == null)
        {
            return;
        }

        int furnitureLayer = LayerMask.NameToLayer("Furniture");
        if (furnitureLayer >= 0)
        {
            root.gameObject.layer = furnitureLayer;
        }

        PlaceableFurniture placeable = GetOrAdd<PlaceableFurniture>(root.gameObject);
        BoxCollider bounds = root.GetComponent<BoxCollider>();
        if (bounds == null)
        {
            bounds = Undo.AddComponent<BoxCollider>(root.gameObject);
            FitColliderToRenderers(root, bounds);
        }
        placeable.PlacementBounds = bounds;

        if (checkout)
        {
            GetOrAdd<CheckoutCounter>(root.gameObject);
        }

        EditorUtility.SetDirty(root.gameObject);
        EditorUtility.SetDirty(placeable);
    }

    private static void FitColliderToRenderers(Transform root, BoxCollider collider)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            collider.size = Vector3.one;
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        collider.center = root.InverseTransformPoint(worldBounds.center);
        Vector3 scale = root.lossyScale;
        collider.size = new Vector3(
            worldBounds.size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            worldBounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            worldBounds.size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
    }

    private static void EnsureStreetIsBuildScene()
    {
        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        EditorBuildSettingsScene street = existing.FirstOrDefault(item => item.path == ScenePath);
        var result = existing.Where(item => item.path != ScenePath).ToList();
        result.Insert(0, street ?? new EditorBuildSettingsScene(ScenePath, true));
        result[0].enabled = true;
        EditorBuildSettings.scenes = result.ToArray();
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Could not find {propertyName} on {target.name}.", target);
            return;
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(gameObject);
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            Debug.LogError($"Missing required asset: {path}");
        }
        return asset;
    }

    private static Transform Require(string path)
    {
        Transform result = Find(path);
        if (result == null)
        {
            Debug.LogError($"Missing Street hierarchy object: {path}");
        }
        return result;
    }

    private static Transform GetOrCreatePoint(
        Transform parent,
        string pointName,
        Vector3 localPosition,
        Vector3 localEulerAngles)
    {
        Transform point = parent.Find(pointName);
        if (point != null)
        {
            return point;
        }

        GameObject pointObject = new GameObject(pointName);
        Undo.RegisterCreatedObjectUndo(
            pointObject,"Create " + pointName);
        point = pointObject.transform;
        point.SetParent(parent,false);
        point.localPosition = localPosition;
        point.localEulerAngles = localEulerAngles;
        return point;
    }

    private static Transform Find(string path)
    {
        string[] segments = path.Split('/');
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name != segments[0])
            {
                continue;
            }

            Transform current = root.transform;
            for (int i = 1; i < segments.Length && current != null; i++)
            {
                current = current.Find(segments[i]);
            }
            return current;
        }
        return null;
    }
}
