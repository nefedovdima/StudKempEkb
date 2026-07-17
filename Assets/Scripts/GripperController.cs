using UnityEngine;
using UnityEngine.InputSystem;

public class GripperController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] VirtualSensors sensors;
    [SerializeField] Transform holdPoint;
    [SerializeField] Transform gripperIRPoint;

    [Header("Settings")]
    [SerializeField] float grabRange = 0.08f;

    [Header("Current state")]
    [SerializeField] Rigidbody heldBall;

    Collider[] heldColliders;

    public bool IsHolding => heldBall != null;

    void Awake()
    {
        if (sensors == null)
        {
            sensors = GetComponentInParent<VirtualSensors>();
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.eKey.wasPressedThisFrame)
        {
            if (IsHolding)
                Release();
            else
                TryGrab();
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            Release();
        }
    }

    public void TryGrab()
    {
        if (IsHolding)
            return;

        if (sensors == null ||
            holdPoint == null ||
            gripperIRPoint == null)
        {
            return;
        }

        if (sensors.GripperIR < 0.5f)
            return;

        Collider[] colliders = Physics.OverlapSphere(
            gripperIRPoint.position,
            grabRange,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider c in colliders)
        {
            if (c.transform.IsChildOf(transform.root))
                continue;

            if (!IsTargetBall(c))
                continue;

            Rigidbody ball = c.attachedRigidbody;

            if (ball == null)
                continue;

            Grab(ball);
            return;
        }
    }

    void Grab(Rigidbody ball)
    {
        heldBall = ball;

        heldBall.linearVelocity = Vector3.zero;
        heldBall.angularVelocity = Vector3.zero;
        heldBall.isKinematic = true;

        heldColliders =
            heldBall.GetComponentsInChildren<Collider>(true);

        foreach (Collider c in heldColliders)
        {
            c.enabled = false;
        }

        heldBall.transform.SetParent(holdPoint, true);
        heldBall.transform.localPosition = Vector3.zero;
        heldBall.transform.localRotation = Quaternion.identity;
    }

    public void Release()
    {
        if (!IsHolding)
            return;

        heldBall.transform.SetParent(null, true);

        if (heldColliders != null)
        {
            foreach (Collider c in heldColliders)
            {
                if (c != null)
                    c.enabled = true;
            }
        }

        heldBall.isKinematic = false;
        heldBall.WakeUp();

        heldBall = null;
        heldColliders = null;
    }

    bool IsTargetBall(Collider c)
    {
        if (c.gameObject.tag == "TargetBall")
            return true;

        if (c.attachedRigidbody != null &&
            c.attachedRigidbody.gameObject.tag == "TargetBall")
        {
            return true;
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (gripperIRPoint == null)
            return;

        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            gripperIRPoint.position,
            grabRange
        );
    }
}