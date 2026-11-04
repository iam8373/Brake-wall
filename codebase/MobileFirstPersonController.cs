using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class MobileFirstPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot;

    [Header("Movement Settings")]
    public float moveSpeed = 3.6f;
    public float gravity = -15f;

    [Header("Touch Look Settings")]
    public float lookSensitivity = 0.16f;
    public float maxPitchAngle = 82f;

    private CharacterController controller;
    private Vector2 joystickInput;
    private float cameraPitch = 0f;
    private float verticalVelocity = 0f;
    private int activeLookFingerId = -1;

    void Awake()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Application.targetFrameRate = 60;

        controller = GetComponent<CharacterController>();
    }

    public void SetMoveJoystickInput(Vector2 input)
    {
        joystickInput = Vector2.ClampMagnitude(input, 1f);
    }

    void Update()
    {
        HandleRightThumbLook();
        HandleCharacterMovement();
    }

    private void HandleRightThumbLook()
    {
        if (Input.touchCount == 0)
        {
            activeLookFingerId = -1;
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);

            if (t.phase == TouchPhase.Began && t.position.x > Screen.width * 0.42f)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                    continue;

                if (activeLookFingerId == -1) activeLookFingerId = t.fingerId;
            }

            if (t.fingerId == activeLookFingerId)
            {
                if (t.phase == TouchPhase.Moved)
                {
                    float yawDelta = t.deltaPosition.x * lookSensitivity;
                    float pitchDelta = t.deltaPosition.y * lookSensitivity;

                    transform.Rotate(Vector3.up * yawDelta);
                    cameraPitch = Mathf.Clamp(cameraPitch - pitchDelta, -maxPitchAngle, maxPitchAngle);
                    if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    activeLookFingerId = -1;
                }
            }
        }
    }

    private void HandleCharacterMovement()
    {
        Vector2 move = joystickInput;
        if (move.sqrMagnitude < 0.01f)
        {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        }

        Vector3 worldMove = (transform.right * move.x + transform.forward * move.y) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        worldMove.y = verticalVelocity;
        controller.Move(worldMove * Time.deltaTime);
    }
}
