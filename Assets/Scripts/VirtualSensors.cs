using System.Collections.Generic;
using UnityEngine;

public class VirtualSensors : MonoBehaviour
{
    [Header("Sensor points")]
    [SerializeField] Transform centerPoint;
    [SerializeField] Transform leftIRPoint;
    [SerializeField] Transform rightIRPoint;
    [SerializeField] Transform gripperIRPoint;

    [Header("Sensor ranges")]
    [SerializeField] float ultrasonicRange = 2f;
    [SerializeField] float irRange = 0.15f;
    [SerializeField] Vector3 gripperHalfExtents =
        new Vector3(0.05f, 0.025f, 0.035f);

    const int GripperOverlapBufferSize = 64;
    readonly Collider[] gripperOverlapBuffer =
        new Collider[GripperOverlapBufferSize];
    readonly List<Collider> gripperChildColliderBuffer =
        new List<Collider>(GripperOverlapBufferSize);

    [Header("Ultrasonic cone")]
    [SerializeField] int ultrasonicRays = 5;
    [SerializeField] float coneAngle = 30f;

    [Header("Current values")]
    [SerializeField] float ultrasonic = 1f;
    [SerializeField] float leftIR;
    [SerializeField] float rightIR;
    [SerializeField] float gripperIR;

    public float Ultrasonic => ultrasonic;
    public float LeftIR => leftIR;
    public float RightIR => rightIR;
    public float GripperIR => gripperIR;

    void FixedUpdate()
    {
        if (centerPoint == null ||
            leftIRPoint == null ||
            rightIRPoint == null ||
            gripperIRPoint == null)
        {
            return;
        }

        ultrasonic = ReadUltrasonic();

        leftIR = ReadIR(
            leftIRPoint.position,
            transform.forward
        );

        rightIR = ReadIR(
            rightIRPoint.position,
            -transform.forward
        );

        gripperIR = ReadGripper();
    }

    float ReadUltrasonic()
    {
        float minDistance = ultrasonicRange;
        int count = Mathf.Max(1, ultrasonicRays);

        for (int i = 0; i < count; i++)
        {
            float angle = count == 1
                ? 0f
                : Mathf.Lerp(
                    -coneAngle / 2f,
                    coneAngle / 2f,
                    (float)i / (count - 1)
                );

            Vector3 direction =
                Quaternion.AngleAxis(angle, transform.up) *
                transform.right;

            if (TryGetDistance(
                centerPoint.position,
                direction,
                ultrasonicRange,
                true,
                out float distance))
            {
                minDistance = Mathf.Min(
                    minDistance,
                    distance
                );
            }
        }

        return Mathf.Clamp01(
            minDistance / ultrasonicRange
        );
    }

    float ReadIR(Vector3 origin, Vector3 direction)
    {
        bool detected = TryGetDistance(
            origin,
            direction,
            irRange,
            true,
            out _
        );

        return detected ? 1f : 0f;
    }

    float ReadGripper()
    {
        Vector3 halfExtents = GetGripperHalfExtents();
        Quaternion rotation = gripperIRPoint.rotation;
        Vector3 boxCenter = gripperIRPoint.position;

        int colliderCount = Physics.OverlapBoxNonAlloc(
            boxCenter,
            halfExtents,
            gripperOverlapBuffer,
            rotation,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        Quaternion inverseRotation = Quaternion.Inverse(rotation);

        for (int i = 0; i < colliderCount; i++)
        {
            Collider c = gripperOverlapBuffer[i];

            if (IsTargetBallCenterInside(
                c,
                boxCenter,
                inverseRotation,
                halfExtents))
            {
                return 1f;
            }
        }

        Transform holdPoint = gripperIRPoint.parent;

        if (holdPoint != null)
        {
            gripperChildColliderBuffer.Clear();
            holdPoint.GetComponentsInChildren(
                false,
                gripperChildColliderBuffer
            );

            foreach (Collider c in gripperChildColliderBuffer)
            {
                if (IsTargetBallCenterInside(
                    c,
                    boxCenter,
                    inverseRotation,
                    halfExtents))
                {
                    return 1f;
                }
            }
        }

        return 0f;
    }

    bool IsTargetBallCenterInside(
        Collider c,
        Vector3 boxCenter,
        Quaternion inverseRotation,
        Vector3 halfExtents)
    {
        if (!IsTargetBall(c))
            return false;

        Rigidbody attachedRigidbody = c.attachedRigidbody;
        Vector3 ballCenter = attachedRigidbody != null
            ? attachedRigidbody.worldCenterOfMass
            : c.bounds.center;

        Vector3 localCenter =
            inverseRotation * (ballCenter - boxCenter);

        return
            Mathf.Abs(localCenter.x) <= halfExtents.x &&
            Mathf.Abs(localCenter.y) <= halfExtents.y &&
            Mathf.Abs(localCenter.z) <= halfExtents.z;
    }

    Vector3 GetGripperHalfExtents()
    {
        return new Vector3(
            Mathf.Abs(gripperHalfExtents.x),
            Mathf.Abs(gripperHalfExtents.y),
            Mathf.Abs(gripperHalfExtents.z)
        );
    }

    bool TryGetDistance(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        bool ignoreTargetBall,
        out float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction.normalized,
            maxDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        distance = maxDistance;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.transform.IsChildOf(transform))
                continue;

            if (ignoreTargetBall && IsTargetBall(hit.collider))
                continue;

            if (hit.distance < distance)
            {
                distance = hit.distance;
                found = true;
            }
        }

        return found;
    }

    bool IsTargetBall(Collider c)
    {
        if (c == null)
            return false;

        if (c.gameObject.CompareTag("TargetBall"))
            return true;

        Rigidbody attachedRigidbody = c.attachedRigidbody;

        return
            attachedRigidbody != null &&
            attachedRigidbody.gameObject.CompareTag("TargetBall");
    }

    void OnDrawGizmosSelected()
    {
        DrawUltrasonicGizmos();
        DrawIRGizmos();
        DrawGripperGizmo();
    }

    void DrawUltrasonicGizmos()
    {
        if (centerPoint == null)
            return;

        Gizmos.color = Color.cyan;

        int count = Mathf.Max(1, ultrasonicRays);

        for (int i = 0; i < count; i++)
        {
            float angle = count == 1
                ? 0f
                : Mathf.Lerp(
                    -coneAngle / 2f,
                    coneAngle / 2f,
                    (float)i / (count - 1)
                );

            Vector3 direction =
                Quaternion.AngleAxis(angle, transform.up) *
                transform.right;

            Gizmos.DrawRay(
                centerPoint.position,
                direction * ultrasonicRange
            );
        }
    }

    void DrawIRGizmos()
    {
        Gizmos.color = Color.yellow;

        if (leftIRPoint != null)
        {
            Gizmos.DrawRay(
                leftIRPoint.position,
                transform.forward * irRange
            );
        }

        if (rightIRPoint != null)
        {
            Gizmos.DrawRay(
                rightIRPoint.position,
                -transform.forward * irRange
            );
        }
    }

    void DrawGripperGizmo()
    {
        if (gripperIRPoint == null)
            return;

        Gizmos.color = Color.green;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            gripperIRPoint.position,
            gripperIRPoint.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(
            Vector3.zero,
            GetGripperHalfExtents() * 2f
        );

        Gizmos.matrix = previousMatrix;
    }
}
