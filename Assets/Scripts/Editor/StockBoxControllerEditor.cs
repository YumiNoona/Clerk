using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StockBoxController))]
public class StockBoxControllerEditor : Editor
{
    private StockBoxController box;

    private SerializedProperty product;
    private SerializedProperty layout;
    private SerializedProperty quantity;

    private SerializedProperty theRB;
    private SerializedProperty boxCollider;

    private SerializedProperty leftFlapPivot;
    private SerializedProperty rightFlapPivot;
    private SerializedProperty leftFlapClosedRotation;
    private SerializedProperty leftFlapOpenRotation;
    private SerializedProperty rightFlapClosedRotation;
    private SerializedProperty rightFlapOpenRotation;
    private SerializedProperty flapAnimationSpeed;

    private SerializedProperty showRuntimeContents;
    private SerializedProperty contentOrigin;
    private SerializedProperty runtimeContentsRevealDelay;

    private SerializedProperty productNameLabel;
    private SerializedProperty quantityLabel;

    private void OnEnable()
    {
        box = (StockBoxController)target;

        product = serializedObject.FindProperty("Product");
        layout = serializedObject.FindProperty("Layout");
        quantity = serializedObject.FindProperty("Quantity");

        theRB = serializedObject.FindProperty("TheRB");
        boxCollider = serializedObject.FindProperty("BoxCollider");

        leftFlapPivot = serializedObject.FindProperty("LeftFlapPivot");
        rightFlapPivot = serializedObject.FindProperty("RightFlapPivot");
        leftFlapClosedRotation = serializedObject.FindProperty("LeftFlapClosedRotation");
        leftFlapOpenRotation = serializedObject.FindProperty("LeftFlapOpenRotation");
        rightFlapClosedRotation = serializedObject.FindProperty("RightFlapClosedRotation");
        rightFlapOpenRotation = serializedObject.FindProperty("RightFlapOpenRotation");
        flapAnimationSpeed = serializedObject.FindProperty("FlapAnimationSpeed");

        showRuntimeContents = serializedObject.FindProperty("ShowRuntimeContents");
        contentOrigin = serializedObject.FindProperty("ContentOrigin");
        runtimeContentsRevealDelay = serializedObject.FindProperty("RuntimeContentsRevealDelay");

        productNameLabel = serializedObject.FindProperty("ProductNameLabel");
        quantityLabel = serializedObject.FindProperty("QuantityLabel");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Product",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(product);
        EditorGUILayout.PropertyField(layout);

        BoxLayout activeLayout = GetActiveLayout();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Quantity",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(quantity);

        if (activeLayout != null)
        {
            EditorGUILayout.HelpBox("Box Capacity: " + activeLayout.Capacity,MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a BoxLayout directly or through the StockInfo asset.",MessageType.Warning);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Components",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(theRB);
        EditorGUILayout.PropertyField(boxCollider);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Flaps",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(leftFlapPivot);
        EditorGUILayout.PropertyField(rightFlapPivot);
        EditorGUILayout.PropertyField(leftFlapClosedRotation);
        EditorGUILayout.PropertyField(leftFlapOpenRotation);
        EditorGUILayout.PropertyField(rightFlapClosedRotation);
        EditorGUILayout.PropertyField(rightFlapOpenRotation);
        EditorGUILayout.PropertyField(flapAnimationSpeed);

        EditorGUILayout.Space();

        DrawRuntimeContentsSection();
        DrawEditorPreviewSection();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Label",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(productNameLabel);
        EditorGUILayout.PropertyField(quantityLabel);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRuntimeContentsSection()
    {
        EditorGUILayout.LabelField("Runtime Contents",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showRuntimeContents,new GUIContent("Show Runtime Contents"));

        if (showRuntimeContents.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(contentOrigin);
            EditorGUILayout.PropertyField(runtimeContentsRevealDelay);

            BoxLayout activeLayout = GetActiveLayout();

            if (activeLayout != null)
            {
                EditorGUILayout.HelpBox("Players will see up to " + activeLayout.MaximumRuntimePreviewObjects + " visual objects when the box is open.",MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox("No visual product objects will be generated during gameplay. The box can still stock shelves normally.",MessageType.Info);
        }
    }

    private void DrawEditorPreviewSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Layout Preview",EditorStyles.boldLabel);

        bool previousShowEditorPreview = box.ShowEditorPreview;
        box.ShowEditorPreview = EditorGUILayout.Toggle("Show Editor Preview",box.ShowEditorPreview);

        if (previousShowEditorPreview != box.ShowEditorPreview)
        {
            EditorUtility.SetDirty(box);

            if (!box.ShowEditorPreview)
            {
                ClearEditorPreview();
            }

            SceneView.RepaintAll();
        }

        if (!box.ShowEditorPreview)
        {
            EditorGUILayout.HelpBox("Editor preview tools are hidden. Runtime box behavior is unaffected.",MessageType.Info);
            return;
        }

        EditorGUI.indentLevel++;

        box.ShowLayoutGizmos = EditorGUILayout.Toggle("Show Layout Gizmos",box.ShowLayoutGizmos);

        if (GUILayout.Button("Generate Layout Preview"))
        {
            GenerateEditorPreview();
        }

        if (GUILayout.Button("Refresh Layout Preview"))
        {
            ClearEditorPreview();
            GenerateEditorPreview();
        }

        if (GUILayout.Button("Clear Layout Preview"))
        {
            ClearEditorPreview();
        }

        EditorGUI.indentLevel--;
    }

    private BoxLayout GetActiveLayout()
    {
        if (box.Layout != null)
        {
            return box.Layout;
        }

        if (box.Product != null)
        {
            return box.Product.DefaultBoxLayout;
        }

        return null;
    }

    private Transform GetContentOrigin()
    {
        if (box.ContentOrigin != null)
        {
            return box.ContentOrigin;
        }

        return box.transform;
    }

    private void GenerateEditorPreview()
    {
        ClearEditorPreview();

        BoxLayout activeLayout = GetActiveLayout();

        if (activeLayout == null)
        {
            Debug.LogWarning("No BoxLayout is assigned.",box);
            return;
        }

        if (box.Product == null || box.Product.StockPrefab == null)
        {
            Debug.LogWarning("The box Product or StockPrefab is missing.",box);
            return;
        }

        Transform parent = GetContentOrigin();

        GameObject previewRootObject = new GameObject("_EDITOR_LAYOUT_PREVIEW");
        Undo.RegisterCreatedObjectUndo(previewRootObject,"Generate box layout preview");

        box.EditorPreviewRoot = previewRootObject.transform;
        box.EditorPreviewRoot.SetParent(parent,false);

        int count = activeLayout.Capacity;

        for (int i = 0; i < count; i++)
        {
            GameObject previewObject = (GameObject)PrefabUtility.InstantiatePrefab(box.Product.StockPrefab.gameObject,box.EditorPreviewRoot);

            if (previewObject == null)
            {
                continue;
            }

            previewObject.transform.localPosition = activeLayout.GetLocalPosition(i);
            previewObject.transform.localRotation = Quaternion.Euler(activeLayout.LocalRotation);

            DisableEditorPreviewComponents(previewObject);
        }

        EditorUtility.SetDirty(box);
        Selection.activeGameObject = box.gameObject;
        SceneView.RepaintAll();
    }

    private void ClearEditorPreview()
    {
        Transform existingPreview = box.EditorPreviewRoot;

        if (existingPreview == null)
        {
            existingPreview = GetContentOrigin().Find("_EDITOR_LAYOUT_PREVIEW");
        }

        if (existingPreview != null)
        {
            Undo.DestroyObjectImmediate(existingPreview.gameObject);
        }

        box.EditorPreviewRoot = null;
        EditorUtility.SetDirty(box);
        SceneView.RepaintAll();
    }

    private void DisableEditorPreviewComponents(GameObject previewObject)
    {
        Rigidbody[] rigidbodies = previewObject.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
        }

        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        StockObject[] stockObjects = previewObject.GetComponentsInChildren<StockObject>(true);

        for (int i = 0; i < stockObjects.Length; i++)
        {
            stockObjects[i].enabled = false;
        }

        previewObject.hideFlags = HideFlags.DontSaveInBuild;
    }

    private void OnSceneGUI()
    {
        if (!box.ShowEditorPreview)
        {
            return;
        }

        BoxLayout activeLayout = GetActiveLayout();

        if (activeLayout == null)
        {
            return;
        }

        Transform origin = GetContentOrigin();
        Vector3 worldPosition = origin.TransformPoint(activeLayout.FirstLocalPosition);

        EditorGUI.BeginChangeCheck();

        Vector3 newWorldPosition = Handles.PositionHandle(worldPosition,origin.rotation);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(activeLayout,"Move box layout start position");

            activeLayout.FirstLocalPosition = origin.InverseTransformPoint(newWorldPosition);

            EditorUtility.SetDirty(activeLayout);
            SceneView.RepaintAll();
        }
    }
}