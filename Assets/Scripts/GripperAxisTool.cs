using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class GripperAxisTool : MonoBehaviour
{
    public Transform axisStart;
    public Transform axisEnd;
    public Transform gripper;
    public Transform holdPoint;
    public Transform gripperIRPoint;

    public float angle = 90f;

    [SerializeField]
    private bool applied;

    private void OnDrawGizmos()
    {
        if (axisStart == null || axisEnd == null)
            return;

        Vector3 a = axisStart.position;
        Vector3 b = axisEnd.position;
        Vector3 d = (b - a).normalized;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(a - d * 0.05f, b + d * 0.15f);
        Gizmos.DrawSphere(a, 0.003f);
        Gizmos.DrawSphere(b, 0.003f);
    }

    [ContextMenu("Rotate Gripper")]
    private void RotateGripper()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Exit Play Mode first.");
            return;
        }

        if (applied)
        {
            Debug.LogError(
                "Rotation is already applied. Use Ctrl+Z first.");
            return;
        }

        if (axisStart == null ||
            axisEnd == null ||
            gripper == null)
        {
            Debug.LogError(
                "Axis Start, Axis End or Gripper is not assigned.");
            return;
        }

        Vector3 p = axisStart.position;
        Vector3 v = axisEnd.position - p;

        if (v.sqrMagnitude < 0.000001f)
        {
            Debug.LogError("Axis points are too close.");
            return;
        }

        Quaternion q =
            Quaternion.AngleAxis(angle, v.normalized);

#if UNITY_EDITOR
        List<Object> objects = new List<Object>
        {
            this,
            gripper
        };

        if (holdPoint != null &&
            !holdPoint.IsChildOf(gripper))
        {
            objects.Add(holdPoint);
        }

        if (gripperIRPoint != null &&
            !gripperIRPoint.IsChildOf(gripper))
        {
            objects.Add(gripperIRPoint);
        }

        Undo.RecordObjects(
            objects.ToArray(),
            "Rotate GFS-X gripper");
#endif

        RotateTransform(gripper, p, q);

        if (holdPoint != null &&
            !holdPoint.IsChildOf(gripper))
        {
            RotateTransform(holdPoint, p, q);
        }

        if (gripperIRPoint != null &&
            !gripperIRPoint.IsChildOf(gripper))
        {
            RotateTransform(gripperIRPoint, p, q);
        }

        applied = true;

#if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private static void RotateTransform(
        Transform target,
        Vector3 pivot,
        Quaternion rotation)
    {
        target.position =
            pivot + rotation * (target.position - pivot);

        target.rotation =
            rotation * target.rotation;
    }
}