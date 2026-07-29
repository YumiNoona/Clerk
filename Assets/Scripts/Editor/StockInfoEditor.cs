using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StockInfo))]
public class StockInfoEditor : Editor
{
    private SerializedProperty productId;
    private SerializedProperty productName;
    private SerializedProperty category;
    private SerializedProperty basePrice;
    private SerializedProperty currentPrice;
    private SerializedProperty stockPrefab;
    private SerializedProperty defaultBoxLayout;

    private void OnEnable()
    {
        productId = serializedObject.FindProperty("productId");
        productName = serializedObject.FindProperty("ProductName");
        category = serializedObject.FindProperty("Category");
        basePrice = serializedObject.FindProperty("BasePrice");
        currentPrice = serializedObject.FindProperty("CurrentPrice");
        stockPrefab = serializedObject.FindProperty("StockPrefab");
        defaultBoxLayout = serializedObject.FindProperty("DefaultBoxLayout");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Product Identity",EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("Product ID",productId.stringValue);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Product Information",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(productName);
        EditorGUILayout.PropertyField(category);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Pricing",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(basePrice);
        EditorGUILayout.PropertyField(currentPrice);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Prefab",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stockPrefab);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Box Setup",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(defaultBoxLayout);

        serializedObject.ApplyModifiedProperties();
    }
}