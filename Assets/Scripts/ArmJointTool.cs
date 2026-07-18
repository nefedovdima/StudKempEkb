using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class ArmJointTool : MonoBehaviour
{
    [Header("Rotation axis")]
    public Transform axisStart;
    public Transform axisEnd;

    [Header("Objects to rotate")]
    public Transform[] targets;

    [Header("Rotation")]
    public float angle = 5f;

    [SerializeField]
    private bool applied;

    public bool Applied => applied;

    public void ApplyRotation()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Debug.LogError("Exit Play Mode before rotating the arm.", this);
            return;
        }

        if (applied)
        {
            Debug.LogWarning(
                "Rotation is already applied. Press Ctrl+Z before trying another angle.",
                this);
            return;
        }

        if (!ValidateSettings(out string error))
        {
            Debug.LogError(error, this);
            return;
        }

        Vector3 pivot = axisStart.position;
        Vector3 axis = (axisEnd.position - axisStart.position).normalized;
        Quaternion rotation = Quaternion.AngleAxis(angle, axis);

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Rotate GFS-X elbow");

        try
        {
            Undo.RecordObject(this, "Rotate GFS-X elbow");

            foreach (Transform target in targets)
                Undo.RecordObject(target, "Rotate GFS-X elbow");

            foreach (Transform target in targets)
            {
                Vector3 newPosition =
                    pivot + rotation * (target.position - pivot);

                Quaternion newRotation =
                    rotation * target.rotation;

                target.SetPositionAndRotation(newPosition, newRotation);
                EditorUtility.SetDirty(target);
            }

            applied = true;
            EditorUtility.SetDirty(this);

            if (gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameObject.scene);

            Undo.CollapseUndoOperations(undoGroup);
            SceneView.RepaintAll();

            Debug.Log(
                $"Elbow rotated by {angle:F2} degrees around axis " +
                $"{axisStart.name} -> {axisEnd.name}.",
                this);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception, this);
        }
#endif
    }

    private bool ValidateSettings(out string error)
    {
        if (axisStart == null || axisEnd == null)
        {
            error = "Assign Axis Start and Axis End.";
            return false;
        }

        if (Vector3.Distance(axisStart.position, axisEnd.position) < 0.000001f)
        {
            error = "Axis Start and Axis End are too close together.";
            return false;
        }

        if (targets == null || targets.Length == 0)
        {
            error = "Add objects to the Targets list.";
            return false;
        }

        HashSet<Transform> uniqueTargets = new HashSet<Transform>();

        foreach (Transform target in targets)
        {
            if (target == null)
            {
                error = "Targets contains an empty element.";
                return false;
            }

            if (!uniqueTargets.Add(target))
            {
                error = $"Target {target.name} is added more than once.";
                return false;
            }
        }

        foreach (Transform first in targets)
        {
            foreach (Transform second in targets)
            {
                if (first != second && first.IsChildOf(second))
                {
                    error =
                        $"{first.name} is a child of {second.name}. " +
                        "Do not add both, otherwise it would rotate twice.";
                    return false;
                }
            }
        }

        error = null;
        return true;
    }

    private void OnDrawGizmos()
    {
        if (axisStart == null || axisEnd == null)
            return;

        Vector3 start = axisStart.position;
        Vector3 end = axisEnd.position;
        Vector3 difference = end - start;

        if (difference.sqrMagnitude < 0.000000000001f)
            return;

        Vector3 direction = difference.normalized;
        float extension = Mathf.Max(difference.magnitude * 3f, 0.02f);
        float pointRadius = Mathf.Max(difference.magnitude * 0.08f, 0.001f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            start - direction * extension,
            end + direction * extension);

        Gizmos.DrawSphere(start, pointRadius);
        Gizmos.DrawSphere(end, pointRadius);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ArmJointTool))]
public class ArmJointToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ArmJointTool tool = (ArmJointTool)target;

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Start with 5 degrees. If the gripper moves upward, press Ctrl+Z " +
            "and use -5 degrees.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(
                   Application.isPlaying || tool.Applied))
        {
            if (GUILayout.Button("Apply elbow rotation"))
                tool.ApplyRotation();
        }

        if (tool.Applied)
        {
            EditorGUILayout.HelpBox(
                "Rotation applied. Press Ctrl+Z before testing another angle.",
                MessageType.Warning);
        }
    }
}
#endif