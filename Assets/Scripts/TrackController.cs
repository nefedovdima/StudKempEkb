using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TrackController : MonoBehaviour
{
    const float PwmScale = 200f;

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

        float leftCommand = v + turn;
        float rightCommand = v - turn;

        float maxMagnitude = Mathf.Max(
            Mathf.Abs(leftCommand),
            Mathf.Abs(rightCommand)
        );

        if (maxMagnitude > maxLinearCmd)
        {
            // Scale both tracks together to preserve their speed ratio.
            float scale = maxLinearCmd / maxMagnitude;
            leftCommand *= scale;
            rightCommand *= scale;
        }

        float targetLeft = ToPwm(leftCommand);
        float targetRight = ToPwm(rightCommand);

        leftPwm = StepPwm(leftPwm, targetLeft);
        rightPwm = StepPwm(rightPwm, targetRight);

        float leftSpeed = leftPwm / PwmScale;
        float rightSpeed = rightPwm / PwmScale;

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
        float rawPwm = Mathf.Clamp(
            speed * PwmScale,
            -100f,
            100f
        );

        float magnitude = Mathf.Abs(rawPwm);

        if (magnitude < motorDeadzone)
            return 0f;

        magnitude = Mathf.Clamp(
            magnitude,
            minMotorPwm,
            100f
        );

        return Mathf.Sign(rawPwm) * magnitude;
    }

    float StepPwm(float current, float target)
    {
        if (Mathf.Approximately(current, 0f))
        {
            // A stopped motor starts at its minimum working PWM.
            return Mathf.Approximately(target, 0f)
                ? 0f
                : Mathf.Sign(target) * minMotorPwm;
        }

        if (Mathf.Approximately(target, 0f))
            return StepPwmTowardZero(current);

        if (Mathf.Sign(current) != Mathf.Sign(target))
        {
            // Brake to zero before starting in the opposite direction.
            return StepPwmTowardZero(current);
        }

        float next = Mathf.MoveTowards(
            current,
            target,
            maxPwmStep
        );

        if (Mathf.Abs(next) < minMotorPwm)
            return Mathf.Sign(target) * minMotorPwm;

        return next;
    }

    float StepPwmTowardZero(float current)
    {
        float nextMagnitude =
            Mathf.Abs(current) - maxPwmStep;

        if (nextMagnitude < minMotorPwm)
            return 0f;

        return Mathf.Sign(current) * nextMagnitude;
    }
}
