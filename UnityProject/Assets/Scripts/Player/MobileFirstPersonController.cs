using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// First-person walk and look for landscape touch. The left-thumb joystick drives movement;
/// a drag in the look zone (right part of the screen, mirrored in left-handed mode) turns the
/// camera. Input is read only through <see cref="PlayerInputSource"/>. This class does not set the
/// frame rate (DeviceTierScaler is the single owner of that).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class MobileFirstPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot;

    [Header("Movement")]
    public float moveSpeed = 3.6f;
    public float gravity = -15f;

    [Header("Look")]
    [Range(0.05f, 0.45f)] public float lookSensitivity = 0.16f;
    public float maxPitchAngle = 82f;
    [Tooltip("Look zone = screen x from this fraction to the right edge (or mirrored in left-handed mode). Matches the wireframe's DRAG TO LOOK zone.")]
    [Range(0.2f, 0.8f)] public float lookZoneStart = 0.42f;

    public bool LeftHanded { get; private set; }
    public float Pitch => cameraPitch;
    public float Yaw => transform.eulerAngles.y;
    public Vector3 HorizontalVelocity { get; private set; }
    public bool IsGrounded => controller && controller.isGrounded;
    public bool InputLocked { get; set; }

    private CharacterController controller;
    private Vector2 joystickInput;
    private float cameraPitch;
    private float verticalVelocity;
    private int activeLookPointer = -1;
    private readonly List<PointerSample> pointers = new List<PointerSample>(10);

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!cameraPivot) Debug.LogError("MobileFirstPersonController: cameraPivot is not assigned.", this);
    }

    public void SetMoveJoystickInput(Vector2 input) => joystickInput = Vector2.ClampMagnitude(input, 1f);

    public void SetLeftHanded(bool leftHanded)
    {
        LeftHanded = leftHanded;
        activeLookPointer = -1;
    }

    public void SetLookSensitivity(float value) => lookSensitivity = Mathf.Clamp(value, 0.05f, 0.45f);

    /// <summary>Moves the player without the CharacterController fighting the change.</summary>
    public void TeleportTo(Vector3 feetPosition, float yaw, float pitch)
    {
        if (!controller) controller = GetComponent<CharacterController>();
        bool wasEnabled = controller.enabled;
        controller.enabled = false;
        // The controller's centre sits at half height above the feet.
        transform.SetPositionAndRotation(feetPosition + Vector3.up * (controller.height * 0.5f + controller.skinWidth), Quaternion.Euler(0f, yaw, 0f));
        controller.enabled = wasEnabled;
        cameraPitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);
        if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        verticalVelocity = 0f;
    }

    /// <summary>Feet position (bottom of the capsule).</summary>
    public Vector3 FeetPosition
    {
        get
        {
            if (!controller) controller = GetComponent<CharacterController>();
            return transform.position - Vector3.up * (controller.height * 0.5f + controller.skinWidth);
        }
    }

    void Update()
    {
        if (InputLocked)
        {
            HorizontalVelocity = Vector3.zero;
            ApplyGravityOnly();
            return;
        }
        HandleLook();
        HandleMovement();
    }

    private bool InLookZone(Vector2 screenPos)
    {
        float x = screenPos.x / Mathf.Max(1, Screen.width);
        return LeftHanded ? x < 1f - lookZoneStart : x > lookZoneStart;
    }

    private void HandleLook()
    {
        PlayerInputSource.ReadPointers(pointers);
        if (pointers.Count == 0)
        {
            activeLookPointer = -1;
            return;
        }

        for (int i = 0; i < pointers.Count; i++)
        {
            PointerSample p = pointers[i];
            if (p.phase == PointerPhase.Began && activeLookPointer == -1 && InLookZone(p.position) && !PlayerInputSource.IsPointerOverUI(p.id))
                activeLookPointer = p.id;

            if (p.id != activeLookPointer) continue;
            if (p.phase == PointerPhase.Moved)
            {
                transform.Rotate(0f, p.delta.x * lookSensitivity, 0f);
                cameraPitch = Mathf.Clamp(cameraPitch - p.delta.y * lookSensitivity, -maxPitchAngle, maxPitchAngle);
                if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }
            else if (p.phase == PointerPhase.Ended)
            {
                activeLookPointer = -1;
            }
        }
    }

    private void HandleMovement()
    {
        Vector2 move = joystickInput;
        if (move.sqrMagnitude < 0.01f) move = PlayerInputSource.KeyboardMove();
        Vector3 horizontal = (transform.right * move.x + transform.forward * move.y) * moveSpeed;
        HorizontalVelocity = horizontal;

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        else verticalVelocity += gravity * Time.deltaTime;

        horizontal.y = verticalVelocity;
        controller.Move(horizontal * Time.deltaTime);
    }

    private void ApplyGravityOnly()
    {
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        else verticalVelocity += gravity * Time.deltaTime;
        controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }
}
