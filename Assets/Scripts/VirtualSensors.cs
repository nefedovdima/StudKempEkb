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
    [SerializeField] float gripperRange = 0.08f;

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
        Collider[] colliders = Physics.OverlapSphere(
            gripperIRPoint.position,
            gripperRange,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider c in colliders)
        {
            if (c.transform.IsChildOf(transform))
                continue;

            if (IsTargetBall(c))
                return 1f;
        }

        return 0f;
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
        return
            c.gameObject.tag == "TargetBall" ||
            c.transform.root.gameObject.tag == "TargetBall";
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

        Gizmos.DrawWireSphere(
            gripperIRPoint.position,
            gripperRange
        );
    }
}