using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !BTE_FORCE_LEGACY_INPUT
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

/// <summary>Phase of a pointer sample, independent of the input backend.</summary>
public enum PointerPhase
{
    Began,
    Moved,
    Stationary,
    Ended
}

/// <summary>One touch (or the mouse, in the editor) for this frame.</summary>
public struct PointerSample
{
    public int id;
    public Vector2 position;
    public Vector2 delta;
    public PointerPhase phase;
}

/// <summary>
/// The ONLY place in the game that reads an input API. Everything else asks this class.
///
/// Two backends, chosen at compile time:
///  - New Input System (when the package is installed and Active Input Handling is
///    "Input System Package" or "Both"): enhanced touch, keyboard and mouse.
///  - Legacy Input Manager (Active Input Handling = "Input Manager (Old)", or define
///    BTE_FORCE_LEGACY_INPUT): Input.touches, keys and mouse.
/// On-screen controls (joystick, PUNCH, VACUUM, hotbar) are uGUI and use EventSystem pointer
/// events, so they work with either backend as long as the matching UI input module is in the
/// scene (the scene builder adds the right one).
/// Keyboard and mouse are an editor convenience for testing without a phone.
/// </summary>
public static class PlayerInputSource
{
    /// <summary>Editor and desktop test keys.</summary>
    public enum DebugKey
    {
        Punch,
        Vacuum,
        BlowToggle,
        Interact,
        Map,
        Pause,
        Slot1,
        Slot2,
        Slot3,
        Slot4
    }

    private static bool initialized;
    private static Vector2 lastMousePosition;

    public static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
#if ENABLE_INPUT_SYSTEM && !BTE_FORCE_LEGACY_INPUT
        if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
#else
        Input.multiTouchEnabled = true;
#endif
    }

    /// <summary>Fills <paramref name="output"/> with this frame's touches. In the editor the right mouse button acts as a look touch.</summary>
    public static void ReadPointers(List<PointerSample> output)
    {
        EnsureInitialized();
        output.Clear();
#if ENABLE_INPUT_SYSTEM && !BTE_FORCE_LEGACY_INPUT
        var touches = ETouch.activeTouches;
        for (int i = 0; i < touches.Count; i++)
        {
            ETouch t = touches[i];
            output.Add(new PointerSample { id = t.finger.index, position = t.screenPosition, delta = t.delta, phase = MapPhase(t.phase) });
        }
        if (output.Count == 0 && Mouse.current != null)
        {
            var right = Mouse.current.rightButton;
            Vector2 pos = Mouse.current.position.ReadValue();
            if (right.wasPressedThisFrame) output.Add(new PointerSample { id = 100, position = pos, delta = Vector2.zero, phase = PointerPhase.Began });
            else if (right.isPressed) output.Add(new PointerSample { id = 100, position = pos, delta = Mouse.current.delta.ReadValue(), phase = PointerPhase.Moved });
            else if (right.wasReleasedThisFrame) output.Add(new PointerSample { id = 100, position = pos, delta = Vector2.zero, phase = PointerPhase.Ended });
        }
#else
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            output.Add(new PointerSample { id = t.fingerId, position = t.position, delta = t.deltaPosition, phase = MapPhase(t.phase) });
        }
        if (output.Count == 0 && Input.mousePresent)
        {
            Vector2 pos = Input.mousePosition;
            if (Input.GetMouseButtonDown(1)) output.Add(new PointerSample { id = 100, position = pos, delta = Vector2.zero, phase = PointerPhase.Began });
            else if (Input.GetMouseButton(1)) output.Add(new PointerSample { id = 100, position = pos, delta = pos - lastMousePosition, phase = PointerPhase.Moved });
            else if (Input.GetMouseButtonUp(1)) output.Add(new PointerSample { id = 100, position = pos, delta = Vector2.zero, phase = PointerPhase.Ended });
            lastMousePosition = pos;
        }
#endif
    }

    /// <summary>WASD / arrow keys as a movement vector (editor testing). Zero on phones.</summary>
    public static Vector2 KeyboardMove()
    {
#if ENABLE_INPUT_SYSTEM && !BTE_FORCE_LEGACY_INPUT
        Keyboard k = Keyboard.current;
        if (k == null) return Vector2.zero;
        float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
        float y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#else
        return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#endif
    }

    public static bool DebugKeyDown(DebugKey key)
    {
#if ENABLE_INPUT_SYSTEM && !BTE_FORCE_LEGACY_INPUT
        Keyboard k = Keyboard.current;
        if (k == null) return false;
        switch (key)
        {
            case DebugKey.Punch: return k.spaceKey.wasPressedThisFrame;
            case DebugKey.Vacuum: return k.leftShiftKey.wasPressedThisFrame;
            case DebugKey.BlowToggle: return k.fKey.wasPressedThisFrame;
            case DebugKey.Interact: return k.eKey.wasPressedThisFrame;
            case DebugKey.Map: return k.mKey.wasPressedThisFrame;
            case DebugKey.Pause: return k.escapeKey.wasPressedThisFrame;
            case DebugKey.Slot1: return k.digit1Key.wasPressedThisFrame;
            case DebugKey.Slot2: return k.digit2Key.wasPressedThisFrame;
            case DebugKey.Slot3: return k.digit3Key.wasPressedThisFrame;
            default: return k.digit4Key.wasPressedThisFrame;
        }
#else
        return Input.GetKeyDown(LegacyKey(key));
#endif
    }

    public static bool DebugKeyUp(DebugKey key)
    {
#if ENABLE_INPUT_SYSTEM && !BTE_FORCE_LEGACY_INPUT
        Keyboard k = Keyboard.current;
        if (k == null) return false;
        if (key == DebugKey.Punch) return k.spaceKey.wasReleasedThisFrame;
        if (key == DebugKey.Vacuum) return k.leftShiftKey.wasReleasedThisFrame;
        return false;
#else
        return Input.GetKeyUp(LegacyKey(key));
#endif
    }

    /// <summary>True if a pointer is over a UI element (so a drag on a button never turns the camera).</summary>
    public static bool IsPointerOverUI(int pointerId)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;
        return pointerId == 100 ? es.IsPointerOverGameObject() : es.IsPointerOverGameObject(pointerId);
    }

#if ENABLE_INPUT_SYSTEM && !BTE_FORCE_LEGACY_INPUT
    private static PointerPhase MapPhase(UnityEngine.InputSystem.TouchPhase phase)
    {
        switch (phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began: return PointerPhase.Began;
            case UnityEngine.InputSystem.TouchPhase.Moved: return PointerPhase.Moved;
            case UnityEngine.InputSystem.TouchPhase.Ended:
            case UnityEngine.InputSystem.TouchPhase.Canceled: return PointerPhase.Ended;
            default: return PointerPhase.Stationary;
        }
    }
#else
    private static PointerPhase MapPhase(TouchPhase phase)
    {
        switch (phase)
        {
            case TouchPhase.Began: return PointerPhase.Began;
            case TouchPhase.Moved: return PointerPhase.Moved;
            case TouchPhase.Ended:
            case TouchPhase.Canceled: return PointerPhase.Ended;
            default: return PointerPhase.Stationary;
        }
    }

    private static KeyCode LegacyKey(DebugKey key)
    {
        switch (key)
        {
            case DebugKey.Punch: return KeyCode.Space;
            case DebugKey.Vacuum: return KeyCode.LeftShift;
            case DebugKey.BlowToggle: return KeyCode.F;
            case DebugKey.Interact: return KeyCode.E;
            case DebugKey.Map: return KeyCode.M;
            case DebugKey.Pause: return KeyCode.Escape;
            case DebugKey.Slot1: return KeyCode.Alpha1;
            case DebugKey.Slot2: return KeyCode.Alpha2;
            case DebugKey.Slot3: return KeyCode.Alpha3;
            default: return KeyCode.Alpha4;
        }
    }
#endif
}
