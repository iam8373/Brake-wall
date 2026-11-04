// Type-check stubs: uGUI, EventSystems and TextMeshPro.
// Declarations only; see UnityEngine.Core.cs for the purpose of these files.
#pragma warning disable CS0067, CS1591, CS8618, CS8625, CS0108
using System;
using UnityEngine.Events;

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }

    public class BaseEventData { }

    public class PointerEventData : BaseEventData
    {
        public Vector2 position { get; set; }
        public Vector2 delta { get; set; }
        public int pointerId { get; set; }
        public Camera pressEventCamera { get; }
        public Camera enterEventCamera { get; }
    }

    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData eventData); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData eventData); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData eventData); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData eventData); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData eventData); }
    public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData eventData); }
    public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData eventData); }

    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get; set; }
        public bool IsPointerOverGameObject() => false;
        public bool IsPointerOverGameObject(int pointerId) => false;
    }

    public abstract class BaseInputModule : UIBehaviour { }
    public class StandaloneInputModule : BaseInputModule { }
}

namespace UnityEngine.UI
{
    public abstract class Graphic : EventSystems.UIBehaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform { get; }
    }

    public abstract class MaskableGraphic : Graphic { }

    public class Image : MaskableGraphic
    {
        public enum Type { Simple = 0, Sliced = 1, Tiled = 2, Filled = 3 }
        public enum FillMethod { Horizontal = 0, Vertical = 1, Radial360 = 4 }
        public Sprite sprite { get; set; }
        public Type type { get; set; }
        public FillMethod fillMethod { get; set; }
        public float fillAmount { get; set; }
    }

    public class Selectable : EventSystems.UIBehaviour
    {
        public bool interactable { get; set; }
        public Graphic targetGraphic { get; set; }
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get; set; }
    }

    public class Toggle : Selectable
    {
        public class ToggleEvent : UnityEvent<bool> { }
        public bool isOn { get; set; }
        public ToggleEvent onValueChanged { get; set; }
        public Graphic graphic { get; set; }
        public void SetIsOnWithoutNotify(bool value) { }
    }

    public class Slider : Selectable
    {
        public class SliderEvent : UnityEvent<float> { }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public float value { get; set; }
        public bool wholeNumbers { get; set; }
        public SliderEvent onValueChanged { get; set; }
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public void SetValueWithoutNotify(float input) { }
    }

    public class CanvasScaler : EventSystems.UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize = 0, ScaleWithScreenSize = 1, ConstantPhysicalSize = 2 }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public float matchWidthOrHeight { get; set; }
    }

    public class GraphicRaycaster : EventSystems.UIBehaviour { }

    public class CanvasGroup : Behaviour
    {
        public float alpha { get; set; }
        public bool interactable { get; set; }
        public bool blocksRaycasts { get; set; }
    }

    public class LayoutElement : EventSystems.UIBehaviour { public float preferredHeight { get; set; } public float preferredWidth { get; set; } }
    public class Outline : EventSystems.UIBehaviour { public Color effectColor { get; set; } public Vector2 effectDistance { get; set; } }
}

namespace UnityEngine
{
    public enum RenderMode { ScreenSpaceOverlay = 0, ScreenSpaceCamera = 1, WorldSpace = 2 }
    public sealed class Sprite : Object { }
    public sealed class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
        public float scaleFactor { get; set; }
    }

    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector2 localPoint) { localPoint = default; return false; }
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint, Camera cam) => false;
    }
}

namespace TMPro
{
    public enum TextAlignmentOptions { TopLeft = 257, Top = 258, Left = 513, Center = 514, Right = 516, BottomLeft = 1025, Midline = 4610 }
    public enum FontStyles { Normal = 0, Bold = 1, Italic = 2, UpperCase = 16 }
    public enum TextOverflowModes { Overflow = 0, Ellipsis = 1, Truncate = 3 }

    public abstract class TMP_Text : UnityEngine.UI.MaskableGraphic
    {
        public string text { get; set; }
        public float fontSize { get; set; }
        public bool enableAutoSizing { get; set; }
        public float fontSizeMin { get; set; }
        public float fontSizeMax { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public FontStyles fontStyle { get; set; }
        public float alpha { get; set; }
        public bool enableWordWrapping { get; set; }
        public TextOverflowModes overflowMode { get; set; }
        public void SetText(string sourceText) { }
    }

    public class TextMeshProUGUI : TMP_Text { }
    public class TMP_Settings : UnityEngine.ScriptableObject { public static TMP_Settings instance => null; }
    public class TextMeshPro : TMP_Text { }
}
