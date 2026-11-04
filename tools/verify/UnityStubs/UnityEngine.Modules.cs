// Type-check stubs: rendering, physics, audio, particles, lights, cameras.
// Declarations only; see UnityEngine.Core.cs for the purpose of these files.
#pragma warning disable CS0067, CS1591, CS8618, CS8625, CS0108
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum MeshTopology { Triangles = 0, Quads = 2, Lines = 3, LineStrip = 4, Points = 5 }

    public sealed class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public int[] triangles { get; set; }
        public Bounds bounds { get; set; }
        public int vertexCount { get; }
        public Rendering.IndexFormat indexFormat { get; set; }
        public void Clear() { }
        public void Clear(bool keepVertexLayout) { }
        public void MarkDynamic() { }
        public void SetVertices(List<Vector3> inVertices) { }
        public void SetNormals(List<Vector3> inNormals) { }
        public void SetColors(List<Color> inColors) { }
        public void SetColors(List<Color32> inColors) { }
        public void SetUVs(int channel, List<Vector2> uvs) { }
        public void SetTriangles(List<int> triangles, int submesh) { }
        public void SetTriangles(List<int> triangles, int submesh, bool calculateBounds) { }
        public void SetIndices(int[] indices, MeshTopology topology, int submesh) { }
        public void SetIndices(List<int> indices, MeshTopology topology, int submesh, bool calculateBounds = true, int baseVertex = 0) { }
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
        public void UploadMeshData(bool markNoLongerReadable) { }
    }

    public class Shader : Object
    {
        public static Shader Find(string name) => null;
        public static int PropertyToID(string name) => 0;
        public bool isSupported => false;
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Material(Material source) { }
        public Color color { get; set; }
        public Shader shader { get; set; }
        public bool enableInstancing { get; set; }
        public int renderQueue { get; set; }
        public bool HasProperty(string name) => false;
        public bool HasProperty(int nameID) => false;
        public void SetColor(string name, Color value) { }
        public void SetColor(int nameID, Color value) { }
        public void SetFloat(string name, float value) { }
        public void SetFloat(int nameID, float value) { }
        public void EnableKeyword(string keyword) { }
        public void DisableKeyword(string keyword) { }
    }

    public sealed class MaterialPropertyBlock
    {
        public void SetColor(int nameID, Color value) { }
        public void SetColor(string name, Color value) { }
        public void SetFloat(int nameID, float value) { }
        public void Clear() { }
    }

    public class Renderer : Component
    {
        public bool enabled { get; set; }
        public Material sharedMaterial { get; set; }
        public Material material { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
        public Bounds bounds { get; }
        public void GetPropertyBlock(MaterialPropertyBlock properties) { }
        public void SetPropertyBlock(MaterialPropertyBlock properties) { }
    }

    public sealed class MeshRenderer : Renderer { }
    public sealed class MeshFilter : Component { public Mesh sharedMesh { get; set; } public Mesh mesh { get; set; } }

    public static class Graphics
    {
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer) { }
        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices) { }
    }

    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }

    public sealed class Camera : Behaviour
    {
        public static Camera main => null;
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public int cullingMask { get; set; }
        public Ray ViewportPointToRay(Vector3 pos) => default;
        public Ray ScreenPointToRay(Vector3 pos) => default;
        public Vector3 WorldToScreenPoint(Vector3 position) => default;
    }

    public enum LightType { Spot = 0, Directional = 1, Point = 2 }
    public enum LightShadows { None = 0, Hard = 1, Soft = 2 }

    public sealed class Light : Behaviour
    {
        public LightType type { get; set; }
        public float range { get; set; }
        public float spotAngle { get; set; }
        public float intensity { get; set; }
        public Color color { get; set; }
        public LightShadows shadows { get; set; }
    }

    public static class RenderSettings
    {
        public static Color ambientLight { get; set; }
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static bool fog { get; set; }
        public static Color fogColor { get; set; }
        public static float fogDensity { get; set; }
        public static FogMode fogMode { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
    }

    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }

    // ---------------------------------------------------------------- physics
    public enum ForceMode { Force = 0, Impulse = 1, VelocityChange = 2, Acceleration = 5 }
    public enum RigidbodyInterpolation { None = 0, Interpolate = 1, Extrapolate = 2 }
    public enum CollisionDetectionMode { Discrete = 0, Continuous = 1, ContinuousDynamic = 2, ContinuousSpeculative = 3 }
    public enum QueryTriggerInteraction { UseGlobal = 0, Ignore = 1, Collide = 2 }

    public class Rigidbody : Component
    {
        public Vector3 linearVelocity { get; set; }
        public Vector3 angularVelocity { get; set; }
        public bool isKinematic { get; set; }
        public bool useGravity { get; set; }
        public float mass { get; set; }
        public float linearDamping { get; set; }
        public float angularDamping { get; set; }
        public float sleepThreshold { get; set; }
        public RigidbodyInterpolation interpolation { get; set; }
        public CollisionDetectionMode collisionDetectionMode { get; set; }
        public bool IsSleeping() => false;
        public void Sleep() { }
        public void WakeUp() { }
        public void AddForce(Vector3 force, ForceMode mode) { }
        public void MovePosition(Vector3 position) { }
    }

    public class Collider : Component
    {
        public bool enabled { get; set; }
        public bool isTrigger { get; set; }
        public Rigidbody attachedRigidbody { get; }
        public Bounds bounds { get; }
    }

    public class BoxCollider : Collider { public Vector3 size { get; set; } public Vector3 center { get; set; } }
    public class SphereCollider : Collider { public float radius { get; set; } }
    public class CapsuleCollider : Collider { public float radius { get; set; } public float height { get; set; } }
    public class MeshCollider : Collider { public Mesh sharedMesh { get; set; } public bool convex { get; set; } public MeshColliderCookingOptions cookingOptions { get; set; } }
    [Flags] public enum MeshColliderCookingOptions { None = 0, CookForFasterSimulation = 2, EnableMeshCleaning = 4, WeldColocatedVertices = 8, UseFastMidphase = 16 }

    public enum CollisionFlags { None = 0, Sides = 1, Above = 2, Below = 4 }

    public class CharacterController : Collider
    {
        public float height { get; set; }
        public float radius { get; set; }
        public Vector3 center { get; set; }
        public float stepOffset { get; set; }
        public float slopeLimit { get; set; }
        public float skinWidth { get; set; }
        public bool isGrounded { get; }
        public Vector3 velocity { get; }
        public CollisionFlags Move(Vector3 motion) => default;
    }

    public struct ContactPoint { public Vector3 point { get; } public Vector3 normal { get; } }

    public class Collision
    {
        public Collider collider { get; }
        public GameObject gameObject { get; }
        public int contactCount { get; }
        public ContactPoint GetContact(int index) => default;
    }

    public struct RaycastHit
    {
        public Vector3 point { get; set; }
        public Vector3 normal { get; set; }
        public float distance { get; set; }
        public Collider collider { get; }
        public Transform transform { get; }
    }

    public static class Physics
    {
        public static Vector3 gravity { get; set; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask) { hitInfo = default; return false; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance) { hitInfo = default; return false; }
        public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, int layerMask) { hitInfo = default; return false; }
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layermask) => false;
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, int layerMask) => 0;
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction) => 0;
        public static void IgnoreLayerCollision(int layer1, int layer2, bool ignore) { }
        public static int defaultSolverIterations { get; set; }
    }

    // ---------------------------------------------------------------- audio
    public sealed class AudioClip : Object { public float length => 0f; }

    public sealed class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public bool loop { get; set; }
        public bool playOnAwake { get; set; }
        public float pitch { get; set; }
        public float volume { get; set; }
        public float spatialBlend { get; set; }
        public bool isPlaying { get; }
        public void Play() { }
        public void Stop() { }
        public void PlayOneShot(AudioClip clip) { }
        public void PlayOneShot(AudioClip clip, float volumeScale) { }
    }

    public sealed class AudioListener : Behaviour { }

    // ---------------------------------------------------------------- particles (APPROXIMATION: modules reduced to the members used)
    public sealed class ParticleSystem : Component
    {
        public MainModule main { get; }
        public EmissionModule emission { get; }
        public ShapeModule shape { get; }
        public bool isPlaying { get; }
        public void Play() { }
        public void Stop() { }
        public void Clear() { }
        public void Emit(int count) { }
        public void Emit(EmitParams emitParams, int count) { }

        public struct MainModule
        {
            public MinMaxCurve startLifetime { get; set; }
            public MinMaxCurve startSpeed { get; set; }
            public MinMaxCurve startSize { get; set; }
            public MinMaxGradient startColor { get; set; }
            public float gravityModifierMultiplier { get; set; }
            public MinMaxCurve gravityModifier { get; set; }
            public int maxParticles { get; set; }
            public bool loop { get; set; }
            public bool playOnAwake { get; set; }
            public float duration { get; set; }
            public ParticleSystemSimulationSpace simulationSpace { get; set; }
        }

        public struct EmissionModule
        {
            public bool enabled { get; set; }
            public MinMaxCurve rateOverTime { get; set; }
        }

        public struct ShapeModule
        {
            public bool enabled { get; set; }
            public ParticleSystemShapeType shapeType { get; set; }
            public float radius { get; set; }
            public float angle { get; set; }
        }

        public struct MinMaxCurve
        {
            public MinMaxCurve(float constant) { }
            public MinMaxCurve(float min, float max) { }
            public static implicit operator MinMaxCurve(float constant) => default;
        }

        public struct MinMaxGradient
        {
            public MinMaxGradient(Color color) { }
            public MinMaxGradient(Color min, Color max) { }
            public static implicit operator MinMaxGradient(Color color) => default;
        }

        public struct EmitParams
        {
            public Vector3 position { get; set; }
            public Vector3 velocity { get; set; }
            public Color32 startColor { get; set; }
            public float startSize { get; set; }
            public float startLifetime { get; set; }
            public bool applyShapeToPosition { get; set; }
        }
    }

    public enum ParticleSystemSimulationSpace { Local = 0, World = 1 }
    public enum ParticleSystemShapeType { Sphere = 0, Cone = 4, Box = 5 }

    public sealed class ParticleSystemRenderer : Renderer { }

    public class AnimationCurve
    {
        public AnimationCurve(params Keyframe[] keys) { }
        public float Evaluate(float time) => 0f;
        public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd) => null;
        public static AnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd) => null;
    }

    public struct Keyframe { public Keyframe(float time, float value) { } }

    // ---------------------------------------------------------------- legacy input (only used inside PlayerInputSource)
    public enum TouchPhase { Began = 0, Moved = 1, Stationary = 2, Ended = 3, Canceled = 4 }

    public struct Touch
    {
        public int fingerId { get; set; }
        public Vector2 position { get; set; }
        public Vector2 deltaPosition { get; set; }
        public TouchPhase phase { get; set; }
    }

    public enum KeyCode { None = 0, Space = 32, Alpha1 = 49, Alpha2 = 50, Alpha3 = 51, Alpha4 = 52, E = 101, F = 102, M = 109, Escape = 27, Mouse0 = 323, Mouse1 = 324, LeftShift = 304 }

    public static class Input
    {
        public static int touchCount => 0;
        public static Touch GetTouch(int index) => default;
        public static float GetAxisRaw(string axisName) => 0f;
        public static float GetAxis(string axisName) => 0f;
        public static bool GetKey(KeyCode key) => false;
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetKeyUp(KeyCode key) => false;
        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;
        public static Vector3 mousePosition => default;
        public static bool mousePresent => false;
        public static bool multiTouchEnabled { get; set; }
    }
}

namespace UnityEngine.Rendering
{
    public enum IndexFormat { UInt16 = 0, UInt32 = 1 }
    public enum ShadowCastingMode { Off = 0, On = 1, TwoSided = 2, ShadowsOnly = 3 }
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3 }
    public enum GraphicsDeviceType { OpenGLES3 = 11, Vulkan = 21, Null = 4 }
    public abstract class RenderPipelineAsset : ScriptableObject { }
    public static class GraphicsSettings
    {
        public static RenderPipelineAsset currentRenderPipeline => null;
        public static RenderPipelineAsset defaultRenderPipeline { get; set; }
    }
}

namespace UnityEngine.Rendering.Universal
{
    // APPROXIMATION: only renderScale and shadowDistance are used; both have public setters in URP 17.
    public class UniversalRenderPipelineAsset : RenderPipelineAsset
    {
        public float renderScale { get; set; }
        public float shadowDistance { get; set; }
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);

    public abstract class UnityEventBase { }

    public class UnityEvent : UnityEventBase
    {
        public void AddListener(UnityAction call) { }
        public void RemoveListener(UnityAction call) { }
        public void RemoveAllListeners() { }
        public void Invoke() { }
    }

    public class UnityEvent<T0> : UnityEventBase
    {
        public void AddListener(UnityAction<T0> call) { }
        public void RemoveListener(UnityAction<T0> call) { }
        public void Invoke(T0 arg0) { }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name { get; } public bool IsValid() => false; }
    public static class SceneManager
    {
        public static Scene GetActiveScene() => default;
        public static void LoadScene(int sceneBuildIndex) { }
        public static void LoadScene(string sceneName) { }
    }
}

namespace UnityEngine
{
    // Android bridge (compiled only in the UNITY_ANDROID player harness).
    public class AndroidJavaObject : IDisposable
    {
        public AndroidJavaObject(string className, params object[] args) { }
        public void Call(string methodName, params object[] args) { }
        public T Call<T>(string methodName, params object[] args) => default;
        public T Get<T>(string fieldName) => default;
        public void Dispose() { }
    }

    public class AndroidJavaClass : AndroidJavaObject
    {
        public AndroidJavaClass(string className) : base(className) { }
        public T GetStatic<T>(string fieldName) => default;
        public T CallStatic<T>(string methodName, params object[] args) => default;
    }
}
