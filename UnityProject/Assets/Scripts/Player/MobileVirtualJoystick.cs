using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Left-thumb virtual joystick (uGUI). Works with either input backend because it uses
/// EventSystem pointer events. In left-handed mode the UI manager mirrors its rectangle.
/// </summary>
public class MobileVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public MobileFirstPersonController playerController;

    [Header("Settings")]
    [Tooltip("Handle travel in canvas units.")]
    public float handleRange = 75f;
    [Tooltip("Inputs below this magnitude are ignored so a resting thumb does not drift.")]
    [Range(0f, 0.4f)] public float deadZone = 0.12f;

    public Vector2 Value { get; private set; }

    void Awake()
    {
        if (!joystickBackground) Debug.LogError("MobileVirtualJoystick: joystickBackground is not assigned.", this);
        if (!playerController) Debug.LogError("MobileVirtualJoystick: playerController is not assigned.", this);
    }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        if (!joystickBackground) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBackground, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;
        Vector2 clamped = Vector2.ClampMagnitude(local, handleRange);
        Vector2 value = clamped / Mathf.Max(1f, handleRange);
        Value = value.magnitude < deadZone ? Vector2.zero : value;
        if (joystickHandle) joystickHandle.anchoredPosition = clamped;
        if (playerController) playerController.SetMoveJoystickInput(Value);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Value = Vector2.zero;
        if (joystickHandle) joystickHandle.anchoredPosition = Vector2.zero;
        if (playerController) playerController.SetMoveJoystickInput(Vector2.zero);
    }
}
