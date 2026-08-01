using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FurniturePlacementProfile))]
public sealed class FurniturePlacementProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "One profile owns the furniture footprint and all local interaction markers. " +
            "Select the furniture in Scene view to move each colored marker and its facing direction.",
            MessageType.Info);
        DrawDefaultInspector();
        FurniturePlacementProfile profile = (FurniturePlacementProfile)target;
        if (GUILayout.Button("Add Customer Marker")) AddMarker(profile,FurnitureMarkerType.Customer);
        if (GUILayout.Button("Add Employee Marker")) AddMarker(profile,FurnitureMarkerType.Employee);
        if (GUILayout.Button("Add Player Marker")) AddMarker(profile,FurnitureMarkerType.Player);
        if (GUILayout.Button("Add Queue Marker")) AddMarker(profile,FurnitureMarkerType.Queue);
    }

    private static void AddMarker(FurniturePlacementProfile profile,FurnitureMarkerType type)
    {
        Undo.RecordObject(profile,"Add Furniture Marker");
        profile.Markers.Add(new FurnitureInteractionMarker
        {
            Name = type.ToString(),
            Type = type,
            LocalPosition = Vector3.zero
        });
        EditorUtility.SetDirty(profile);
    }

    private void OnSceneGUI()
    {
        FurniturePlacementProfile profile = (FurniturePlacementProfile)target;
        Transform root = profile.transform;
        for (int i = 0; i < profile.Markers.Count; i++)
        {
            FurnitureInteractionMarker marker = profile.Markers[i];
            if (marker == null) continue;
            Color color = PlacementGuideRenderer.GetMarkerColor(marker.Type);
            Handles.color = color;
            Vector3 world = root.TransformPoint(marker.LocalPosition);
            Quaternion worldRotation = root.rotation * Quaternion.Euler(marker.LocalEulerAngles);
            float size = HandleUtility.GetHandleSize(world) * 0.12f;
            Handles.SphereHandleCap(0,world,Quaternion.identity,size,EventType.Repaint);
            Handles.ArrowHandleCap(0,world,worldRotation,size * 4f,EventType.Repaint);
            Handles.Label(world + Vector3.up * size,marker.Name);

            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.PositionHandle(world,worldRotation);
            Quaternion rotated = Handles.RotationHandle(worldRotation,moved);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(profile,"Move Furniture Marker");
                marker.LocalPosition = root.InverseTransformPoint(moved);
                marker.LocalEulerAngles = (Quaternion.Inverse(root.rotation) * rotated).eulerAngles;
                EditorUtility.SetDirty(profile);
            }
        }
    }
}
