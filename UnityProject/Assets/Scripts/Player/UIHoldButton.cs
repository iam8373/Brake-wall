using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// A button that reports press and release separately (PUNCH: tap or hold to charge; VACUUM: hold).
/// Lives in its own file because Unity can only save a MonoBehaviour in a scene when the file name
/// matches the class name.
/// </summary>
public class UIHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UnityEvent onPointerDown = new UnityEvent();
    public UnityEvent onPointerUp = new UnityEvent();

    public bool IsHeld { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsHeld = true;
        onPointerDown.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsHeld) return;
        IsHeld = false;
        onPointerUp.Invoke();
    }

    /// <summary>Releases the button if the app loses focus while it is held.</summary>
    void OnDisable()
    {
        if (!IsHeld) return;
        IsHeld = false;
        onPointerUp.Invoke();
    }
}
