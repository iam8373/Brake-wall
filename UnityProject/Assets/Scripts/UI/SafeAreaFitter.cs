using UnityEngine;

/// <summary>
/// Fits a full-screen RectTransform to the device safe area (notches, camera cut-outs, rounded
/// corners) plus an extra margin, so no control sits under a cut-out. Put all HUD controls under it.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Tooltip("Extra margin inside the safe area, as a fraction of the screen height.")]
    [Range(0f, 0.05f)] public float extraMargin = 0.015f;

    private RectTransform rect;
    private Rect lastSafeArea;
    private int lastWidth, lastHeight;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.width != lastWidth || Screen.height != lastHeight) Apply();
    }

    private void Apply()
    {
        Rect safe = Screen.safeArea;
        lastSafeArea = safe;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        float margin = Screen.height * extraMargin;
        Vector2 min = new Vector2((safe.xMin + margin) / Screen.width, (safe.yMin + margin) / Screen.height);
        Vector2 max = new Vector2((safe.xMax - margin) / Screen.width, (safe.yMax - margin) / Screen.height);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
