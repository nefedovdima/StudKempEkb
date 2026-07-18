using UnityEditor;
using UnityEngine;

public static class MeasureSelectedBounds
{
    [MenuItem("Tools/Measure Selected Bounds")]
    private static void MeasureBounds()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("Measure Selected Bounds: no GameObject is selected.");
            return;
        }

        Renderer[] renderers = selectedObject.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogWarning(
                $"Measure Selected Bounds: '{selectedObject.name}' has no Renderer components in its hierarchy.",
                selectedObject
            );
            return;
        }

        Bounds combinedBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 size = combinedBounds.size;
        Vector3 center = combinedBounds.center;

        Debug.Log(
            $"Bounds for '{selectedObject.name}' ({renderers.Length} Renderer(s))\n" +
            $"Size X: {size.x:F4} m ({size.x * 100f:F2} cm)\n" +
            $"Size Y: {size.y:F4} m ({size.y * 100f:F2} cm)\n" +
            $"Size Z: {size.z:F4} m ({size.z * 100f:F2} cm)\n" +
            $"Center: X = {center.x:F4}, Y = {center.y:F4}, Z = {center.z:F4}",
            selectedObject
        );
    }
}
