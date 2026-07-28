using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StockInfo))]
public class StockInfoEditor : Editor
{
    private SerializedProperty productId;
    private SerializedProperty productName;
    private SerializedProperty category;
    private SerializedProperty price;

    private void OnEnable()
    {
        productId = serializedObject.FindProperty("productId");
        productName = serializedObject.FindProperty("ProductName");
        category = serializedObject.FindProperty("Category");
        price = serializedObject.FindProperty("Price");
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
        EditorGUILayout.PropertyField(price);

        serializedObject.ApplyModifiedProperties();
    }
}