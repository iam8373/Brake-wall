using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MobileVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public MobileFirstPersonController playerController;

    [Header("Settings")]
    public float handleRange = 75f;

    private Vector2 inputVector = Vector2.zero;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!joystickBackground) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
            inputVector = clamped / Mathf.Max(1f, handleRange);

            if (joystickHandle)
                joystickHandle.anchoredPosition = clamped;

            if (playerController)
                playerController.SetMoveJoystickInput(inputVector);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (joystickHandle)
            joystickHandle.anchoredPosition = Vector2.zero;

        if (playerController)
            playerController.SetMoveJoystickInput(Vector2.zero);
    }
}

public class UIHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UnityEvent onPointerDown = new UnityEvent();
    public UnityEvent onPointerUp = new UnityEvent();

    public void OnPointerDown(PointerEventData eventData)
    {
        onPointerDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onPointerUp?.Invoke();
    }
}
