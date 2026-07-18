using UnityEngine;
using UnityEngine.InputSystem;

public class GripperController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] VirtualSensors sensors;
    [SerializeField] Transform holdPoint;

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

        if (sensors == null || holdPoint == null)
        {
            return;
        }

        if (!sensors.TryGetGripperTarget(out Rigidbody ball) ||
            ball == null)
        {
            return;
        }

        Grab(ball);
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
}
