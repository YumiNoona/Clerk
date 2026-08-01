using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Rounded Rectangle Graphic")]
public sealed class RoundedRectGraphic : MaskableGraphic
{
    [Min(0f)] public float Radius = 36f;
    [Range(1,16)] public int CornerSegments = 8;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(Radius,Mathf.Min(rect.width,rect.height)*0.5f);
        int segments = Mathf.Max(1,CornerSegments);
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = rect.center;
        vh.AddVert(vertex);
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 center = corner switch
            {
                0 => new Vector2(rect.xMax-radius,rect.yMax-radius),
                1 => new Vector2(rect.xMin+radius,rect.yMax-radius),
                2 => new Vector2(rect.xMin+radius,rect.yMin+radius),
                _ => new Vector2(rect.xMax-radius,rect.yMin+radius)
            };
            for (int step = 0; step <= segments; step++)
            {
                float angle = (corner*90f+step*90f/segments)*Mathf.Deg2Rad;
                vertex.position = center + new Vector2(
                    Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                vh.AddVert(vertex);
            }
        }
        int count = 4*(segments+1);
        for (int i = 0; i < count; i++)
        {
            vh.AddTriangle(0,i+1,(i+1)%count+1);
        }
    }
}
