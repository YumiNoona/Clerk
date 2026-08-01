using UnityEngine;

[CreateAssetMenu(fileName = "New Box Layout",menuName = "Clerk/Products/Box Layout")]
public class BoxLayout : ScriptableObject
{
    [Header("Capacity")]
    public int Columns = 4;
    public int Rows = 2;
    public int Layers = 1;

    [Header("Placement")]
    public Vector3 FirstLocalPosition = new Vector3(-0.3f,0.1f,-0.15f);
    public Vector3 ColumnSpacing = new Vector3(0.2f,0f,0f);
    public Vector3 RowSpacing = new Vector3(0f,0f,0.2f);
    public Vector3 LayerSpacing = new Vector3(0f,0.2f,0f);
    public Vector3 LocalRotation = Vector3.zero;

    [Header("Runtime Visual Limit")]
    public int MaximumRuntimePreviewObjects = 8;

    public int Capacity
    {
        get
        {
            int safeColumns = Mathf.Max(1,Columns);
            int safeRows = Mathf.Max(1,Rows);
            int safeLayers = Mathf.Max(1,Layers);

            return safeColumns * safeRows * safeLayers;
        }
    }

    public Vector3 GetLocalPosition(int index)
    {
        int safeColumns = Mathf.Max(1,Columns);
        int safeRows = Mathf.Max(1,Rows);

        int column = index % safeColumns;
        int row = (index / safeColumns) % safeRows;
        int layer = index / (safeColumns * safeRows);

        Vector3 position = FirstLocalPosition;
        position += ColumnSpacing * column;
        position += RowSpacing * row;
        position += LayerSpacing * layer;

        return position;
    }

    private void OnValidate()
    {
        Columns = Mathf.Max(1,Columns);
        Rows = Mathf.Max(1,Rows);
        Layers = Mathf.Max(1,Layers);
        MaximumRuntimePreviewObjects = Mathf.Max(0,MaximumRuntimePreviewObjects);
    }
}
