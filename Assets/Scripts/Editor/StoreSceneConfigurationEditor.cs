using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StorePointHierarchyMigration
{
    static StorePointHierarchyMigration()
    {
        EditorApplication.delayCall += MigrateOpenScene;
    }

    private static void MigrateOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        StoreSceneConfiguration configuration =
            Object.FindAnyObjectByType<StoreSceneConfiguration>();

        if (configuration == null)
        {
            return;
        }

        ConsolidateDeliveryServices(configuration);

        Transform zones = configuration.transform.root.Find("Store/Zones");
        bool removedUnusedZone = false;

        if (zones != null)
        {
            removedUnusedZone |= RemoveChild(zones,"Sales Floor");
            removedUnusedZone |= RemoveChild(zones,"Checkout Zone");
            removedUnusedZone |= RemoveChild(zones,"Stockroom Zone");
            removedUnusedZone |= RemoveChild(zones,"Delivery Zone");
        }

        if (removedUnusedZone)
        {
            EditorSceneManager.MarkSceneDirty(configuration.gameObject.scene);
            Debug.Log(
                "Removed unused legacy zones. Furniture Placement Area was kept.",
                configuration);
        }

        SerializedObject serialized = new SerializedObject(configuration);
        SerializedProperty migrated =
            serialized.FindProperty("legacyHierarchyMigrated");

        if (migrated == null || migrated.boolValue)
        {
            return;
        }

        RemoveChild(configuration.transform,"Spawn Points");
        RemoveChild(configuration.transform,"Entrances");
        RemoveChild(configuration.transform,"Exits");

        StoreDeliveryConfiguration delivery =
            Object.FindAnyObjectByType<StoreDeliveryConfiguration>();

        if (delivery != null)
        {
            RemoveChild(delivery.transform,"Stock Delivery Point");
            RemoveChild(delivery.transform,"Furniture Delivery Point");
        }

        migrated.boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(configuration);
        EditorSceneManager.MarkSceneDirty(configuration.gameObject.scene);
        Debug.Log(
            "Migrated legacy point empties into the two store configuration components. " +
            "Save the scene to keep the cleaned hierarchy.",
            configuration);
    }

    private static bool RemoveChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            Undo.DestroyObjectImmediate(child.gameObject);
            return true;
        }

        return false;
    }

    private static void ConsolidateDeliveryServices(
        StoreSceneConfiguration configuration)
    {
        PurchaseService purchase =
            Object.FindAnyObjectByType<PurchaseService>();

        if (purchase == null)
        {
            return;
        }

        StockDeliveryService oldStock = purchase.StockDeliveryService != null
            ? purchase.StockDeliveryService
            : Object.FindAnyObjectByType<StockDeliveryService>();
        FurnitureDeliveryService oldFurniture =
            purchase.FurnitureDeliveryService != null
                ? purchase.FurnitureDeliveryService
                : Object.FindAnyObjectByType<FurnitureDeliveryService>();

        StockDeliveryService stock =
            purchase.GetComponent<StockDeliveryService>();
        if (stock == null)
        {
            stock = Undo.AddComponent<StockDeliveryService>(purchase.gameObject);
        }

        FurnitureDeliveryService furniture =
            purchase.GetComponent<FurnitureDeliveryService>();
        if (furniture == null)
        {
            furniture = Undo.AddComponent<FurnitureDeliveryService>(purchase.gameObject);
        }

        if (oldStock != null && oldStock != stock)
        {
            stock.DeliverySpawnPoint = oldStock.DeliverySpawnPoint;
        }

        if (oldFurniture != null && oldFurniture != furniture)
        {
            furniture.FurnitureSpawnPoint = oldFurniture.FurnitureSpawnPoint;
        }

        purchase.StockDeliveryService = stock;
        purchase.FurnitureDeliveryService = furniture;
        EditorUtility.SetDirty(purchase);

        if (oldStock != null && oldStock.gameObject != purchase.gameObject)
        {
            Undo.DestroyObjectImmediate(oldStock.gameObject);
        }

        if (oldFurniture != null && oldFurniture.gameObject != purchase.gameObject)
        {
            Undo.DestroyObjectImmediate(oldFurniture.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(configuration.gameObject.scene);
    }
}

[CustomEditor(typeof(StoreSceneConfiguration))]
public sealed class StoreSceneConfigurationEditor : Editor
{
    private int selectedPoint;
    private SerializedProperty spawns;
    private SerializedProperty entrance;
    private SerializedProperty inside;
    private SerializedProperty clerkPoint;
    private SerializedProperty queue;
    private SerializedProperty exit;
    private SerializedProperty despawn;
    private SerializedProperty pedestrianTrack;

    private void OnEnable()
    {
        spawns = serializedObject.FindProperty("customerSpawns");
        entrance = serializedObject.FindProperty("entranceWaitPoint");
        inside = serializedObject.FindProperty("insidePoint");
        clerkPoint = serializedObject.FindProperty("checkoutClerkPoint");
        queue = serializedObject.FindProperty("checkoutQueue");
        exit = serializedObject.FindProperty("exitPoint");
        despawn = serializedObject.FindProperty("despawnPoint");
        pedestrianTrack = serializedObject.FindProperty("pedestrianTrack");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox(
            "All positions live in this component. No point GameObjects or " +
            "individual point scripts are required. Select this object and " +
            "move the colored wire handles in the Scene view.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Customer Spawn"))
        {
            AddSpawn();
            selectedPoint = spawns.arraySize - 1;
            FinishAddAndFocus(
                spawns.GetArrayElementAtIndex(spawns.arraySize - 1)
                    .FindPropertyRelative("Pose")
                    .FindPropertyRelative("Position").vector3Value);
        }
        if (GUILayout.Button("+ Checkout Queue"))
        {
            AddPose(queue,new Vector3(0f,0f,-queue.arraySize));
            selectedPoint = spawns.arraySize + 3 + queue.arraySize - 1;
            FinishAddAndFocus(
                queue.GetArrayElementAtIndex(queue.arraySize - 1)
                    .FindPropertyRelative("Position").vector3Value);
        }
        if (GUILayout.Button("+ Pedestrian Point"))
        {
            AddVector(pedestrianTrack,new Vector3(pedestrianTrack.arraySize * 3f,0f,0f));
            selectedPoint = spawns.arraySize + queue.arraySize +
                5 + pedestrianTrack.arraySize - 1;
            FinishAddAndFocus(
                pedestrianTrack.GetArrayElementAtIndex(
                    pedestrianTrack.arraySize - 1).vector3Value);
        }
        EditorGUILayout.EndHorizontal();

        string[] pointNames = BuildPointNames();
        selectedPoint = Mathf.Clamp(
            selectedPoint,
            0,
            Mathf.Max(0,pointNames.Length - 1));
        selectedPoint = EditorGUILayout.Popup(
            "Edit Point",
            selectedPoint,
            pointNames);

        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();
    }

    private void AddSpawn()
    {
        int index = spawns.arraySize;
        spawns.InsertArrayElementAtIndex(index);
        SerializedProperty item = spawns.GetArrayElementAtIndex(index);
        item.FindPropertyRelative("Pose").FindPropertyRelative("Position").vector3Value =
            new Vector3(index * 3f,0f,0f);
        item.FindPropertyRelative("Pose").FindPropertyRelative("EulerAngles").vector3Value =
            Vector3.zero;
        item.FindPropertyRelative("Weight").floatValue = 1f;
        item.FindPropertyRelative("Radius").floatValue = 0f;
    }

    private static void AddPose(SerializedProperty list, Vector3 value)
    {
        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);
        SerializedProperty pose = list.GetArrayElementAtIndex(index);
        pose.FindPropertyRelative("Position").vector3Value = value;
        pose.FindPropertyRelative("EulerAngles").vector3Value = Vector3.zero;
    }

    private static void AddVector(SerializedProperty list, Vector3 value)
    {
        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);
        list.GetArrayElementAtIndex(index).vector3Value = value;
    }

    private void OnSceneGUI()
    {
        serializedObject.Update();
        StoreSceneConfiguration configuration = (StoreSceneConfiguration)target;
        Transform root = configuration.transform;

        int cursor = 0;

        for (int i = 0; i < spawns.arraySize; i++)
        {
            SerializedProperty pose = spawns.GetArrayElementAtIndex(i)
                .FindPropertyRelative("Pose");
            DrawSelectableSphere(
                root.TransformPoint(
                    pose.FindPropertyRelative("Position").vector3Value),
                "SPAWN " + (i + 1),
                Color.cyan,
                cursor);

            if (selectedPoint == cursor)
            {
                DrawPoseHandle(root,pose,"SPAWN " + (i + 1),Color.cyan);
            }
            cursor++;
        }

        DrawSelectableSphere(GetWorldPosition(root,entrance),"ENTRANCE",Color.green,cursor);
        if (selectedPoint == cursor)
            DrawPoseHandle(root,entrance,"ENTRANCE",Color.green);
        cursor++;

        Color insideColor = new Color(0.2f,1f,0.5f);
        DrawSelectableSphere(GetWorldPosition(root,inside),"INSIDE",insideColor,cursor);
        if (selectedPoint == cursor)
            DrawPoseHandle(root,inside,"INSIDE",insideColor);
        cursor++;

        Color clerkColor = new Color(0.25f,0.55f,1f);
        DrawSelectableSphere(
            GetWorldPosition(root,clerkPoint),
            "CHECKOUT CLERK",
            clerkColor,
            cursor);
        if (selectedPoint == cursor)
            DrawPoseHandle(root,clerkPoint,"CHECKOUT CLERK",clerkColor);
        cursor++;

        for (int i = 0; i < queue.arraySize; i++)
        {
            SerializedProperty pose = queue.GetArrayElementAtIndex(i);
            DrawSelectableSphere(
                GetWorldPosition(root,pose),
                "QUEUE " + (i + 1),
                Color.yellow,
                cursor);

            if (selectedPoint == cursor)
            {
                DrawPoseHandle(root,pose,
                    "QUEUE " + (i + 1),Color.yellow);
            }
            cursor++;
        }

        Color exitColor = new Color(1f,0.55f,0f);
        DrawSelectableSphere(GetWorldPosition(root,exit),"EXIT",exitColor,cursor);
        if (selectedPoint == cursor)
            DrawPoseHandle(root,exit,"EXIT",exitColor);
        cursor++;

        DrawSelectableSphere(GetWorldPosition(root,despawn),"DESPAWN",Color.red,cursor);
        if (selectedPoint == cursor)
            DrawPoseHandle(root,despawn,"DESPAWN",Color.red);
        cursor++;

        for (int i = 0; i < pedestrianTrack.arraySize; i++)
        {
            Vector3 world = root.TransformPoint(
                pedestrianTrack.GetArrayElementAtIndex(i).vector3Value);
            DrawSelectableSphere(
                world,
                "PEDESTRIAN " + (i + 1),
                new Color(0.15f,0.8f,1f),
                cursor);

            if (selectedPoint == cursor)
            {
                DrawPedestrianPoint(root,i);
            }
            cursor++;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPedestrianPoint(Transform root, int index)
    {
        Handles.color = new Color(0.15f,0.8f,1f);
        SerializedProperty point = pedestrianTrack.GetArrayElementAtIndex(index);
        Vector3 world = root.TransformPoint(point.vector3Value);
        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.PositionHandle(world,Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target,"Move Pedestrian Track Point");
            point.vector3Value = root.InverseTransformPoint(moved);
        }
    }

    private string[] BuildPointNames()
    {
        int count = spawns.arraySize + queue.arraySize +
            pedestrianTrack.arraySize + 5;
        string[] names = new string[Mathf.Max(1,count)];
        int cursor = 0;

        for (int i = 0; i < spawns.arraySize; i++)
            names[cursor++] = "Customer Spawn " + (i + 1);
        names[cursor++] = "Entrance";
        names[cursor++] = "Inside";
        names[cursor++] = "Checkout Clerk";
        for (int i = 0; i < queue.arraySize; i++)
            names[cursor++] = "Checkout Queue " + (i + 1);
        names[cursor++] = "Exit";
        names[cursor++] = "Despawn";
        for (int i = 0; i < pedestrianTrack.arraySize; i++)
            names[cursor++] = "Pedestrian " + (i + 1);

        return names;
    }

    private void DrawPoseHandle(
        Transform root,
        SerializedProperty pose,
        string label,
        Color color)
    {
        SerializedProperty position = pose.FindPropertyRelative("Position");
        SerializedProperty euler = pose.FindPropertyRelative("EulerAngles");
        Vector3 world = root.TransformPoint(position.vector3Value);
        Quaternion rotation = root.rotation * Quaternion.Euler(euler.vector3Value);

        Handles.color = color;
        Handles.ArrowHandleCap(0,world,rotation,0.8f,EventType.Repaint);
        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.PositionHandle(world,rotation);
        Quaternion turned = Handles.RotationHandle(rotation,world);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target,"Move " + label);
            position.vector3Value = root.InverseTransformPoint(moved);
            euler.vector3Value = (Quaternion.Inverse(root.rotation) * turned).eulerAngles;
        }
    }

    private void DrawSelectableSphere(
        Vector3 world,
        string label,
        Color color,
        int pointIndex)
    {
        Handles.color = selectedPoint == pointIndex
            ? Color.white
            : color;

        float size = HandleUtility.GetHandleSize(world) * 0.16f;

        if (Handles.Button(
                world,
                Quaternion.identity,
                size,
                size * 1.2f,
                Handles.SphereHandleCap))
        {
            selectedPoint = pointIndex;
            Repaint();
        }

        if (selectedPoint == pointIndex)
        {
            Handles.color = color;
            Handles.Label(
                world + Vector3.up * size * 1.4f,
                label);
        }
    }

    private static Vector3 GetWorldPosition(
        Transform root,
        SerializedProperty pose)
    {
        return root.TransformPoint(
            pose.FindPropertyRelative("Position").vector3Value);
    }

    private void FinishAddAndFocus(Vector3 localPosition)
    {
        serializedObject.ApplyModifiedProperties();
        StoreSceneConfiguration configuration =
            (StoreSceneConfiguration)target;
        Vector3 world = configuration.transform.TransformPoint(localPosition);
        SceneView.lastActiveSceneView?.Frame(
            new Bounds(world,Vector3.one * 2f),
            false);
        SceneView.RepaintAll();
    }
}

[CustomEditor(typeof(StoreDeliveryConfiguration))]
public sealed class StoreDeliveryConfigurationEditor : Editor
{
    private int selectedPoint;
    private SerializedProperty stock;
    private SerializedProperty furniture;

    private void OnEnable()
    {
        stock = serializedObject.FindProperty("stockDelivery");
        furniture = serializedObject.FindProperty("furnitureDelivery");
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Both delivery positions are stored here. Move their wireframe " +
            "handles in the Scene view; no delivery empties are needed.",
            MessageType.Info);
        selectedPoint = EditorGUILayout.Popup(
            "Edit Point",
            selectedPoint,
            new[] { "Stock Delivery", "Furniture Delivery" });
        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        serializedObject.Update();
        Transform root = ((StoreDeliveryConfiguration)target).transform;

        DrawDeliverySphere(root,stock,"STOCK DELIVERY",Color.magenta,0);
        DrawDeliverySphere(
            root,
            furniture,
            "FURNITURE DELIVERY",
            new Color(0.65f,0.3f,1f),
            1);

        if (selectedPoint == 0)
            DrawDelivery(root,stock,"STOCK DELIVERY",Color.magenta);
        else
            DrawDelivery(root,furniture,"FURNITURE DELIVERY",new Color(0.65f,0.3f,1f));
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDeliverySphere(
        Transform root,
        SerializedProperty pose,
        string label,
        Color color,
        int index)
    {
        Vector3 world = root.TransformPoint(
            pose.FindPropertyRelative("Position").vector3Value);
        float size = HandleUtility.GetHandleSize(world) * 0.16f;
        Handles.color = selectedPoint == index ? Color.white : color;

        if (Handles.Button(
                world,
                Quaternion.identity,
                size,
                size * 1.2f,
                Handles.SphereHandleCap))
        {
            selectedPoint = index;
            Repaint();
        }

        if (selectedPoint == index)
        {
            Handles.color = color;
            Handles.Label(world + Vector3.up * size * 1.4f,label);
        }
    }

    private void DrawDelivery(
        Transform root,
        SerializedProperty pose,
        string label,
        Color color)
    {
        SerializedProperty position = pose.FindPropertyRelative("Position");
        SerializedProperty euler = pose.FindPropertyRelative("EulerAngles");
        Vector3 world = root.TransformPoint(position.vector3Value);
        Quaternion rotation = root.rotation * Quaternion.Euler(euler.vector3Value);
        Handles.color = color;
        Handles.ArrowHandleCap(0,world,rotation,1f,EventType.Repaint);

        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.PositionHandle(world,rotation);
        Quaternion turned = Handles.RotationHandle(rotation,world);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target,"Move " + label);
            position.vector3Value = root.InverseTransformPoint(moved);
            euler.vector3Value = (Quaternion.Inverse(root.rotation) * turned).eulerAngles;
        }
    }
}
