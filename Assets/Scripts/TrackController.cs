using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TrackController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 0.57f;
    [SerializeField] float turnSpeed = 120f;
    [SerializeField] float turnK = 0.30f;
    [SerializeField] float maxLinearCmd = 0.25f;

    [Header("Motors")]
    [SerializeField] float motorDeadzone = 10f;
    [SerializeField] float minMotorPwm = 35f;
    [SerializeField] float maxPwmStep = 15f;

    Rigidbody rb;

    float gas;
    float steer;
    float leftPwm;
    float rightPwm;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = 2.5f;
        rb.linearDamping = 8f;
        rb.angularDamping = 8f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        Keyboard k = Keyboard.current;

        if (k == null)
        {
            gas = 0f;
            steer = 0f;
            return;
        }

        gas = 0f;
        steer = 0f;

        if (k.wKey.isPressed || k.upArrowKey.isPressed)
            gas += 1f;

        if (k.sKey.isPressed || k.downArrowKey.isPressed)
            gas -= 1f;

        if (k.aKey.isPressed || k.leftArrowKey.isPressed)
            steer -= 1f;

        if (k.dKey.isPressed || k.rightArrowKey.isPressed)
            steer += 1f;
    }

    void FixedUpdate()
    {
        float v = Mathf.Clamp(
            gas * moveSpeed,
            -maxLinearCmd,
            maxLinearCmd
        );

        float turn = steer * turnK * maxLinearCmd;

        float targetLeft = ToPwm(v + turn);
        float targetRight = ToPwm(v - turn);

        leftPwm = Mathf.MoveTowards(
            leftPwm,
            targetLeft,
            maxPwmStep
        );

        rightPwm = Mathf.MoveTowards(
            rightPwm,
            targetRight,
            maxPwmStep
        );

        float leftSpeed = leftPwm / 200f;
        float rightSpeed = rightPwm / 200f;

        float linearSpeed =
            (leftSpeed + rightSpeed) / 2f;

        float angularSpeed =
            (leftSpeed - rightSpeed) /
            (2f * maxLinearCmd) *
            turnSpeed;

        Vector3 forward = transform.right;

        rb.MovePosition(
            rb.position +
            forward *
            linearSpeed *
            Time.fixedDeltaTime
        );

        Quaternion rotation = Quaternion.Euler(
            0f,
            angularSpeed * Time.fixedDeltaTime,
            0f
        );

        rb.MoveRotation(rb.rotation * rotation);
    }

    float ToPwm(float speed)
    {
        float pwm = Mathf.Abs(speed) * 200f;

        if (pwm < motorDeadzone)
            return 0f;

        pwm = Mathf.Clamp(
            pwm,
            minMotorPwm,
            100f
        );

        return Mathf.Sign(speed) * pwm;
    }
}