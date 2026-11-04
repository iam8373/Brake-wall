// Type-check stubs for the UnityEngine API surface used by the game scripts.
// These are declarations only. They exist so the C# compiler can check syntax,
// identifiers, argument counts and types outside the Unity editor.
// They do NOT behave like Unity and nothing here is executed.
// Members whose exact Unity signature was not certain are marked APPROXIMATION
// and listed in tools/verify/README.md.
#pragma warning disable CS0067, CS0626, CS0660, CS0661, CS1591, CS8618, CS8625, CS0108
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }
        public int GetInstanceID() => 0;
        public static void Destroy(Object obj) { }
        public static void Destroy(Object obj, float t) { }
        public static void DestroyImmediate(Object obj) { }
        public static void DontDestroyOnLoad(Object target) { }
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object => original;
        public static T FindFirstObjectByType<T>() where T : Object => default;
        public static T FindAnyObjectByType<T>() where T : Object => default;
        public static implicit operator bool(Object exists) => exists != null;
    }

    [Flags] public enum HideFlags { None = 0, HideInHierarchy = 1, HideInInspector = 2, DontSave = 52, HideAndDontSave = 61 }

    public class Component : Object
    {
        public Transform transform { get; }
        public GameObject gameObject { get; }
        public string tag { get; set; }
        public T GetComponent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public T GetComponentInChildren<T>(bool includeInactive) => default;
        public T[] GetComponentsInChildren<T>(bool includeInactive) => default;
        public T GetComponentInParent<T>() => default;
        public bool TryGetComponent<T>(out T component) { component = default; return false; }
        public bool CompareTag(string tag) => false;
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get; }
    }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) => null;
        public void StopCoroutine(Coroutine routine) { }
        public void StopAllCoroutines() { }
        public void Invoke(string methodName, float time) { }
        public void CancelInvoke() { }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject => default;
    }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public Transform transform { get; }
        public int layer { get; set; }
        public string tag { get; set; }
        public bool activeSelf { get; }
        public bool activeInHierarchy { get; }
        public bool isStatic { get; set; }
        public void SetActive(bool value) { }
        public T AddComponent<T>() where T : Component => default;
        public Component AddComponent(Type componentType) => null;
        public T GetComponent<T>() => default;
        public T[] GetComponents<T>() => null;
        public T GetComponentInChildren<T>() => default;
        public T GetComponentInChildren<T>(bool includeInactive) => default;
        public bool TryGetComponent<T>(out T component) { component = default; return false; }
        public static GameObject CreatePrimitive(PrimitiveType type) => null;
        public static GameObject Find(string name) => null;
    }

    public enum PrimitiveType { Sphere = 0, Capsule = 1, Cylinder = 2, Cube = 3, Plane = 4, Quad = 5 }
    public enum Space { World = 0, Self = 1 }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale { get; }
        public Vector3 forward { get; set; }
        public Vector3 right { get; set; }
        public Vector3 up { get; set; }
        public Transform parent { get; set; }
        public int childCount { get; }
        public Transform GetChild(int index) => null;
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation) { }
        public void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation) { }
        public void Rotate(Vector3 eulers) { }
        public void Rotate(Vector3 eulers, Space relativeTo) { }
        public void Rotate(float xAngle, float yAngle, float zAngle) { }
        public void LookAt(Vector3 worldPosition) { }
        public Vector3 TransformPoint(Vector3 position) => default;
        public Vector3 InverseTransformPoint(Vector3 position) => default;
        public Vector3 TransformDirection(Vector3 direction) => default;
        public void SetAsLastSibling() { }
        public void SetAsFirstSibling() { }
        public int GetSiblingIndex() => 0;
        public void SetSiblingIndex(int index) { }
        public Transform Find(string n) => null;
        public IEnumerator GetEnumerator() => null;
    }

    public sealed class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Rect rect { get; }
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height) : this() { this.x = x; this.y = y; this.width = width; this.height = height; }
        public float x, y, width, height;
        public Vector2 position { get; set; }
        public Vector2 size { get; set; }
        public float xMin { get; set; }
        public float yMin { get; set; }
        public float xMax { get; set; }
        public float yMax { get; set; }
        public bool Contains(Vector2 point) => false;
        public static bool operator ==(Rect a, Rect b) => true;
        public static bool operator !=(Rect a, Rect b) => false;
        public override bool Equals(object o) => false;
        public override int GetHashCode() => 0;
    }

    // ---------------------------------------------------------------- math
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => default;
        public static Vector2 one => default;
        public static Vector2 up => default;
        public static Vector2 right => default;
        public float magnitude => 0f;
        public float sqrMagnitude => 0f;
        public Vector2 normalized => default;
        public static Vector2 ClampMagnitude(Vector2 vector, float maxLength) => default;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => default;
        public static float Distance(Vector2 a, Vector2 b) => 0f;
        public static Vector2 operator +(Vector2 a, Vector2 b) => default;
        public static Vector2 operator -(Vector2 a, Vector2 b) => default;
        public static Vector2 operator -(Vector2 a) => default;
        public static Vector2 operator *(Vector2 a, float d) => default;
        public static Vector2 operator *(float d, Vector2 a) => default;
        public static Vector2 operator /(Vector2 a, float d) => default;
        public static bool operator ==(Vector2 a, Vector2 b) => true;
        public static bool operator !=(Vector2 a, Vector2 b) => false;
        public static implicit operator Vector3(Vector2 v) => default;
        public static implicit operator Vector2(Vector3 v) => default;
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0f; }
        public static Vector3 zero => default;
        public static Vector3 one => default;
        public static Vector3 up => default;
        public static Vector3 down => default;
        public static Vector3 left => default;
        public static Vector3 right => default;
        public static Vector3 forward => default;
        public static Vector3 back => default;
        public float magnitude => 0f;
        public float sqrMagnitude => 0f;
        public Vector3 normalized => default;
        public float this[int index] { get => 0f; set { } }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => default;
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => default;
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta) => default;
        public static Vector3 ClampMagnitude(Vector3 vector, float maxLength) => default;
        public static float Distance(Vector3 a, Vector3 b) => 0f;
        public static float Dot(Vector3 a, Vector3 b) => 0f;
        public static Vector3 Cross(Vector3 a, Vector3 b) => default;
        public static float Angle(Vector3 from, Vector3 to) => 0f;
        public static Vector3 Scale(Vector3 a, Vector3 b) => default;
        public static Vector3 Normalize(Vector3 value) => default;
        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal) => default;
        public static Vector3 operator +(Vector3 a, Vector3 b) => default;
        public static Vector3 operator -(Vector3 a, Vector3 b) => default;
        public static Vector3 operator -(Vector3 a) => default;
        public static Vector3 operator *(Vector3 a, float d) => default;
        public static Vector3 operator *(float d, Vector3 a) => default;
        public static Vector3 operator /(Vector3 a, float d) => default;
        public static bool operator ==(Vector3 a, Vector3 b) => true;
        public static bool operator !=(Vector3 a, Vector3 b) => false;
    }

    public struct Vector3Int
    {
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }
        public Vector3Int(int x, int y, int z) : this() { this.x = x; this.y = y; this.z = z; }
        public static Vector3Int zero => default;
        public static Vector3Int one => default;
        public static Vector3Int up => default;
        public static Vector3Int right => default;
        public static Vector3Int forward => default;
        public static Vector3Int operator +(Vector3Int a, Vector3Int b) => default;
        public static Vector3Int operator -(Vector3Int a, Vector3Int b) => default;
        public static Vector3Int operator -(Vector3Int a) => default;
        public static Vector3Int operator *(Vector3Int a, int b) => default;
        public static bool operator ==(Vector3Int a, Vector3Int b) => true;
        public static bool operator !=(Vector3Int a, Vector3Int b) => false;
        public static implicit operator Vector3(Vector3Int v) => default;
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) : this() { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => default;
        public Vector3 eulerAngles { get; set; }
        public static Quaternion Euler(Vector3 euler) => default;
        public static Quaternion Euler(float x, float y, float z) => default;
        public static Quaternion LookRotation(Vector3 forward) => default;
        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards) => default;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => default;
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) => default;
        public static Quaternion AngleAxis(float angle, Vector3 axis) => default;
        public static Quaternion Inverse(Quaternion rotation) => default;
        public static Quaternion operator *(Quaternion lhs, Quaternion rhs) => default;
        public static Vector3 operator *(Quaternion rotation, Vector3 point) => default;
    }

    public struct Matrix4x4
    {
        public static Matrix4x4 identity => default;
        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s) => default;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => default;
        public static Color black => default;
        public static Color clear => default;
        public static Color gray => default;
        public static Color grey => default;
        public static Color red => default;
        public static Color green => default;
        public static Color yellow => default;
        public static Color cyan => default;
        public static Color Lerp(Color a, Color b, float t) => default;
        public static Color operator *(Color a, float b) => default;
        public static Color operator *(Color a, Color b) => default;
        public static Color operator +(Color a, Color b) => default;
        public static implicit operator Color32(Color c) => default;
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color(Color32 c) => default;
    }

    public struct Bounds
    {
        public Bounds(Vector3 center, Vector3 size) : this() { this.center = center; this.size = size; }
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
        public Vector3 extents { get; set; }
        public Vector3 min { get; set; }
        public Vector3 max { get; set; }
        public bool Contains(Vector3 point) => false;
        public void Encapsulate(Vector3 point) { }
    }

    public struct Ray
    {
        public Ray(Vector3 origin, Vector3 direction) : this() { this.origin = origin; this.direction = direction; }
        public Vector3 origin { get; set; }
        public Vector3 direction { get; set; }
        public Vector3 GetPoint(float distance) => default;
    }

    public static class Mathf
    {
        public const float PI = 3.14159274f;
        public const float Infinity = float.PositiveInfinity;
        public const float Epsilon = 1.401298E-45f;
        public const float Deg2Rad = 0.0174532924f;
        public const float Rad2Deg = 57.29578f;
        public static float Sin(float f) => 0f;
        public static float Atan2(float y, float x) => 0f;
        public static float Cos(float f) => 0f;
        public static float Abs(float f) => 0f;
        public static int Abs(int v) => 0;
        public static float Sqrt(float f) => 0f;
        public static float Pow(float f, float p) => 0f;
        public static float Exp(float p) => 0f;
        public static float Log10(float f) => 0f;
        public static float Min(float a, float b) => 0f;
        public static int Min(int a, int b) => 0;
        public static float Max(float a, float b) => 0f;
        public static int Max(int a, int b) => 0;
        public static float Clamp(float value, float min, float max) => 0f;
        public static int Clamp(int value, int min, int max) => 0;
        public static float Clamp01(float value) => 0f;
        public static float Lerp(float a, float b, float t) => 0f;
        public static float LerpUnclamped(float a, float b, float t) => 0f;
        public static float InverseLerp(float a, float b, float value) => 0f;
        public static float SmoothStep(float from, float to, float t) => 0f;
        public static float MoveTowards(float current, float target, float maxDelta) => 0f;
        public static float PerlinNoise(float x, float y) => 0f;
        public static float Repeat(float t, float length) => 0f;
        public static float Sign(float f) => 0f;
        public static float Round(float f) => 0f;
        public static float Floor(float f) => 0f;
        public static int FloorToInt(float f) => 0;
        public static int CeilToInt(float f) => 0;
        public static int RoundToInt(float f) => 0;
        public static bool Approximately(float a, float b) => false;
    }

    public static class Random
    {
        public static float value => 0f;
        public static Vector3 insideUnitSphere => default;
        public static Vector2 insideUnitCircle => default;
        public static Vector3 onUnitSphere => default;
        public static Quaternion rotation => default;
        public static float Range(float minInclusive, float maxInclusive) => 0f;
        public static int Range(int minInclusive, int maxExclusive) => 0;
        public static void InitState(int seed) { }
    }

    public struct LayerMask
    {
        public int value { get; set; }
        public static implicit operator int(LayerMask mask) => 0;
        public static implicit operator LayerMask(int intVal) => default;
        public static int NameToLayer(string layerName) => 0;
        public static int GetMask(params string[] layerNames) => 0;
    }

    // ---------------------------------------------------------------- time / app / debug
    public static class Time
    {
        public static float time => 0f;
        public static float unscaledTime => 0f;
        public static float deltaTime => 0f;
        public static float unscaledDeltaTime => 0f;
        public static float fixedDeltaTime { get; set; }
        public static float timeScale { get; set; }
        public static float realtimeSinceStartup => 0f;
        public static int frameCount => 0;
    }

    public enum RuntimePlatform { Android = 11, WindowsEditor = 7, OSXEditor = 0, LinuxEditor = 16 }
    public enum SystemLanguage { English = 10 }

    public static class Application
    {
        public static int targetFrameRate { get; set; }
        public static string persistentDataPath => "";
        public static string dataPath => "";
        public static bool isEditor => false;
        public static bool isPlaying => false;
        public static RuntimePlatform platform => default;
        public static string version => "";
        public static void Quit() { }
        public static event Action<string, string, LogType> logMessageReceived;
    }

    public enum LogType { Error = 0, Assert = 1, Warning = 2, Log = 3, Exception = 4 }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void Log(object message, Object context) { }
        public static void LogWarning(object message) { }
        public static void LogWarning(object message, Object context) { }
        public static void LogError(object message) { }
        public static void LogError(object message, Object context) { }
        public static void LogException(Exception exception) { }
    }

    public enum ScreenOrientation { Portrait = 1, PortraitUpsideDown = 2, LandscapeLeft = 3, LandscapeRight = 4, AutoRotation = 5 }

    public static class Screen
    {
        public static int width => 0;
        public static int height => 0;
        public static float dpi => 0f;
        public static Rect safeArea => default;
        public static ScreenOrientation orientation { get; set; }
        public static bool autorotateToPortrait { get; set; }
        public static bool autorotateToPortraitUpsideDown { get; set; }
        public static bool autorotateToLandscapeLeft { get; set; }
        public static bool autorotateToLandscapeRight { get; set; }
        public static int sleepTimeout { get; set; }
    }

    public static class SleepTimeout { public const int NeverSleep = -1; public const int SystemSetting = -2; }

    public enum DeviceType { Unknown = 0, Handheld = 1, Console = 2, Desktop = 3 }

    public static class SystemInfo
    {
        public static int systemMemorySize => 0;
        public static int processorCount => 0;
        public static int graphicsMemorySize => 0;
        public static string graphicsDeviceName => "";
        public static string deviceModel => "";
        public static DeviceType deviceType => default;
        public static bool supportsInstancing => false;
        public static Rendering.GraphicsDeviceType graphicsDeviceType => default;
    }

    public enum ShadowQuality { Disable = 0, HardOnly = 1, All = 2 }

    public static class QualitySettings
    {
        public static ShadowQuality shadows { get; set; }
        public static int vSyncCount { get; set; }
        public static string[] names => null;
        public static int GetQualityLevel() => 0;
        public static void SetQualityLevel(int index) { }
        public static void SetQualityLevel(int index, bool applyExpensiveChanges) { }
        public static Rendering.RenderPipelineAsset renderPipeline { get; set; }
        public static Rendering.RenderPipelineAsset GetRenderPipelineAssetAt(int index) => null;
    }

    public static class Handheld { public static void Vibrate() { } }

    public static class PlayerPrefs
    {
        public static int GetInt(string key, int defaultValue) => 0;
        public static void SetInt(string key, int value) { }
        public static float GetFloat(string key, float defaultValue) => 0f;
        public static void SetFloat(string key, float value) { }
        public static void Save() { }
    }

    // ---------------------------------------------------------------- coroutines
    public class YieldInstruction { }
    public sealed class Coroutine : YieldInstruction { }
    public sealed class WaitForSeconds : YieldInstruction { public WaitForSeconds(float seconds) { } }
    public sealed class WaitForEndOfFrame : YieldInstruction { }
    public class CustomYieldInstruction : IEnumerator
    {
        public object Current => null;
        public bool MoveNext() => false;
        public void Reset() { }
    }
    public sealed class WaitForSecondsRealtime : CustomYieldInstruction { public WaitForSecondsRealtime(float time) { } }

    // ---------------------------------------------------------------- attributes
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)] public class HeaderAttribute : Attribute { public HeaderAttribute(string header) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : Attribute { public TooltipAttribute(string tooltip) { } }
    [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    [AttributeUsage(AttributeTargets.Field)] public class MinAttribute : Attribute { public MinAttribute(float min) { } }
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HideInInspector : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class TextAreaAttribute : Attribute { public TextAreaAttribute() { } public TextAreaAttribute(int minLines, int maxLines) { } }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public class RequireComponent : Attribute
    {
        public RequireComponent(Type requiredComponent) { }
        public RequireComponent(Type requiredComponent, Type requiredComponent2) { }
        public RequireComponent(Type requiredComponent, Type requiredComponent2, Type requiredComponent3) { }
    }
    [AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class CreateAssetMenuAttribute : Attribute { public string fileName { get; set; } public string menuName { get; set; } }
    [AttributeUsage(AttributeTargets.Method)] public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute() { } public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad = 0, BeforeSceneLoad = 1, SubsystemRegistration = 4 }

    // ---------------------------------------------------------------- resources
    public class TextAsset : Object
    {
        public string text => "";
        public byte[] bytes => null;
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object => default;
        public static Object Load(string path) => null;
    }

    public static class JsonUtility
    {
        public static string ToJson(object obj) => "";
        public static T FromJson<T>(string json) => default;
    }
}
