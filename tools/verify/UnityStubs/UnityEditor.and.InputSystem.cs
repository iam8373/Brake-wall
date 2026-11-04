// Type-check stubs: UnityEditor (compiled only by the editor harness) and the
// Input System package (compiled only by the ENABLE_INPUT_SYSTEM harness).
// Declarations only; see UnityEngine.Core.cs for the purpose of these files.
#pragma warning disable CS0067, CS1591, CS8618, CS8625, CS0108
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
        public MenuItem(string itemName, bool isValidateFunction, int priority) { }
    }

    public static class AssetDatabase
    {
        public static void CreateAsset(UnityEngine.Object asset, string path) { }
        public static void SaveAssets() { }
        public static void Refresh() { }
        public static bool IsValidFolder(string path) => false;
        public static string CreateFolder(string parentFolder, string newFolderName) => "";
        public static T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object => default;
        public static UnityEngine.Object[] LoadAllAssetsAtPath(string assetPath) => null;
        public static bool CopyAsset(string path, string newPath) => false;
        public static string GetAssetPath(UnityEngine.Object assetObject) => "";
        public static bool DeleteAsset(string path) => false;
    }

    public static class PrefabUtility
    {
        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath) => null;
        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath, out bool success) { success = false; return null; }
    }

    public static class EditorUtility
    {
        public static void SetDirty(UnityEngine.Object target) { }
        public static bool DisplayDialog(string title, string message, string ok) => false;
        public static bool DisplayDialog(string title, string message, string ok, string cancel) => false;
    }

    public static class Selection { public static GameObject activeGameObject { get; set; } }

    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object obj) { }
        public SerializedProperty FindProperty(string propertyPath) => null;
        public bool ApplyModifiedProperties() => false;
    }

    public class SerializedProperty
    {
        public string stringValue { get; set; }
        public int arraySize { get; set; }
        public SerializedProperty GetArrayElementAtIndex(int index) => null;
    }

    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get; set; } }
    public class EditorBuildSettingsScene { public EditorBuildSettingsScene(string path, bool enable) { } public string path { get; set; } }

    public static class PlayerSettings
    {
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static bool allowedAutorotateToPortrait { get; set; }
        public static bool allowedAutorotateToPortraitUpsideDown { get; set; }
        public static bool allowedAutorotateToLandscapeLeft { get; set; }
        public static bool allowedAutorotateToLandscapeRight { get; set; }
    }

    public enum UIOrientation { Portrait = 0, PortraitUpsideDown = 1, LandscapeRight = 2, LandscapeLeft = 3, AutoRotation = 4 }
}

namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene = 0, DefaultGameObjects = 1 }
    public enum NewSceneMode { Single = 0, Additive = 1 }

    public static class EditorSceneManager
    {
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup setup, NewSceneMode mode) => default;
        public static bool SaveScene(UnityEngine.SceneManagement.Scene scene, string dstScenePath) => false;
        public static bool MarkSceneDirty(UnityEngine.SceneManagement.Scene scene) => false;
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => false;
    }
}

namespace UnityEditor.Events
{
    public static class UnityEventTools
    {
        public static void AddPersistentListener(UnityEventBase unityEvent, UnityAction call) { }
        public static void AddPersistentListener<T0>(UnityEvent<T0> unityEvent, UnityAction<T0> call) { }
        public static void AddBoolPersistentListener(UnityEventBase unityEvent, UnityAction<bool> call, bool argument) { }
        public static void AddIntPersistentListener(UnityEventBase unityEvent, UnityAction<int> call, int argument) { }
        public static void AddFloatPersistentListener(UnityEventBase unityEvent, UnityAction<float> call, float argument) { }
        public static void AddStringPersistentListener(UnityEventBase unityEvent, UnityAction<string> call, string argument) { }
        public static void AddObjectPersistentListener<T>(UnityEventBase unityEvent, UnityAction<T> call, T argument) where T : UnityEngine.Object { }
    }
}
#endif

#if ENABLE_INPUT_SYSTEM
namespace UnityEngine.InputSystem
{
    public enum TouchPhase { None = 0, Began = 1, Moved = 2, Ended = 3, Canceled = 4, Stationary = 5 }

    public class InputControl { }
    public class ButtonControl : InputControl { public bool isPressed => false; public bool wasPressedThisFrame => false; public bool wasReleasedThisFrame => false; }
    public class Vector2Control : InputControl { public Vector2 ReadValue() => default; }

    public class Keyboard
    {
        public static Keyboard current => null;
        public ButtonControl wKey => null;
        public ButtonControl aKey => null;
        public ButtonControl sKey => null;
        public ButtonControl dKey => null;
        public ButtonControl eKey => null;
        public ButtonControl fKey => null;
        public ButtonControl mKey => null;
        public ButtonControl spaceKey => null;
        public ButtonControl escapeKey => null;
        public ButtonControl leftShiftKey => null;
        public ButtonControl digit1Key => null;
        public ButtonControl digit2Key => null;
        public ButtonControl digit3Key => null;
        public ButtonControl digit4Key => null;
    }

    public class Mouse
    {
        public static Mouse current => null;
        public ButtonControl leftButton => null;
        public ButtonControl rightButton => null;
        public Vector2Control delta => null;
        public Vector2Control position => null;
    }
}

namespace UnityEngine.InputSystem.EnhancedTouch
{
    public static class EnhancedTouchSupport
    {
        public static bool enabled => false;
        public static void Enable() { }
    }

    public class Finger { public int index => 0; }

    public struct Touch
    {
        public static IReadOnlyList<Touch> activeTouches => null;
        public Finger finger => null;
        public int touchId => 0;
        public UnityEngine.InputSystem.TouchPhase phase => default;
        public Vector2 screenPosition => default;
        public Vector2 delta => default;
    }
}

namespace UnityEngine.InputSystem.UI
{
    public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule { }
}
#endif
