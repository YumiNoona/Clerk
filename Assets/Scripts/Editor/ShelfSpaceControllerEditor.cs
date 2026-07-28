using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShelfSpaceController))]
public class ShelfSpaceControllerEditor : Editor
{
    private SerializedProperty info;
    private SerializedProperty shelfLabel;
    private SerializedProperty currencySymbol;
    private SerializedProperty objectsOnShelf;
    private SerializedProperty currentPlacementMode;

    private SerializedProperty firstObjectLocalPosition;
    private SerializedProperty objectSpacingX;
    private SerializedProperty objectsPerRow;
    private SerializedProperty numberOfRows;
    private SerializedProperty rowSpacing;
    private SerializedProperty objectLocalRotation;

    private SerializedProperty placementGroups;

    private void OnEnable()
    {
        info = serializedObject.FindProperty("Info");
        shelfLabel = serializedObject.FindProperty("ShelfLabel");
        currencySymbol = serializedObject.FindProperty("CurrencySymbol");
        objectsOnShelf = serializedObject.FindProperty("ObjectsOnShelf");
        currentPlacementMode = serializedObject.FindProperty("CurrentPlacementMode");

        firstObjectLocalPosition = serializedObject.FindProperty("FirstObjectLocalPosition");
        objectSpacingX = serializedObject.FindProperty("ObjectSpacingX");
        objectsPerRow = serializedObject.FindProperty("ObjectsPerRow");
        numberOfRows = serializedObject.FindProperty("NumberOfRows");
        rowSpacing = serializedObject.FindProperty("RowSpacing");
        objectLocalRotation = serializedObject.FindProperty("ObjectLocalRotation");

        placementGroups = serializedObject.FindProperty("PlacementGroups");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Shelf Information",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(info);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Shelf Label",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(shelfLabel);
        EditorGUILayout.PropertyField(currencySymbol);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Objects",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(objectsOnShelf,true);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Placement Mode",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(currentPlacementMode);

        EditorGUILayout.Space();

        ShelfSpaceController.PlacementMode selectedMode = (ShelfSpaceController.PlacementMode)currentPlacementMode.enumValueIndex;

        if (selectedMode == ShelfSpaceController.PlacementMode.SmartPlacement)
        {
            EditorGUILayout.LabelField("Smart Placement",EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(firstObjectLocalPosition);
            EditorGUILayout.PropertyField(objectSpacingX);
            EditorGUILayout.PropertyField(objectsPerRow);
            EditorGUILayout.PropertyField(numberOfRows);
            EditorGUILayout.PropertyField(rowSpacing);
            EditorGUILayout.PropertyField(objectLocalRotation);

            int validObjectsPerRow = Mathf.Max(1,objectsPerRow.intValue);
            int validNumberOfRows = Mathf.Max(1,numberOfRows.intValue);
            int capacity = validObjectsPerRow * validNumberOfRows;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Shelf Capacity: " + capacity,MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Placement Point Groups",EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(placementGroups,true);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("The stock category determines which placement-point group is used.",MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}