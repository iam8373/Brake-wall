#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// BreakToEscape > Create URP Tier Assets: copies the project's active URP asset into three
/// assets (URP_Low, URP_Mid, URP_High under Assets/Generated/URP), sets render scale and shadow
/// distance from economy.json, and assigns each to the quality level of the same name if it exists.
/// Unity has no public API to create quality levels, so create "Low", "Mid" and "High" by hand first
/// (README_UNITY_SETUP.md step 7). Not tested or unverified.
/// </summary>
public static class QualityTierSetupEditor
{
    [MenuItem("BreakToEscape/Create URP Tier Assets")]
    public static void CreateTierAssets()
    {
        var source = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (!source)
        {
            EditorUtility.DisplayDialog("Break to Escape", "No URP asset is set in Project Settings > Graphics. Create the project from the Universal 3D template.", "OK");
            return;
        }
        var economyAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Data/economy.json");
        if (!economyAsset) { Debug.LogError("Assets/Resources/Data/economy.json is missing."); return; }
        var data = BreakToEscape.Core.Economy.EconomyData.FromJson(economyAsset.text);

        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder("Assets/Generated/URP")) AssetDatabase.CreateFolder("Assets/Generated", "URP");
        string sourcePath = AssetDatabase.GetAssetPath(source);
        string[] keys = { "low", "mid", "high" };
        string[] levelNames = { "Low", "Mid", "High" };
        string[] names = QualitySettings.names;
        int original = QualitySettings.GetQualityLevel();
        int assigned = 0;

        for (int i = 0; i < 3; i++)
        {
            string path = "Assets/Generated/URP/URP_" + levelNames[i] + ".asset";
            if (!AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path) && !AssetDatabase.CopyAsset(sourcePath, path))
            {
                Debug.LogError("Could not copy " + sourcePath + " to " + path);
                continue;
            }
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            var tier = data.Tier(keys[i]);
            asset.renderScale = tier.renderScale;
            asset.shadowDistance = tier.shadowDistance;
            EditorUtility.SetDirty(asset);
            for (int q = 0; q < names.Length; q++)
            {
                if (!string.Equals(names[q], levelNames[i], System.StringComparison.OrdinalIgnoreCase)) continue;
                QualitySettings.SetQualityLevel(q, false);
                QualitySettings.renderPipeline = asset;
                assigned++;
            }
        }
        QualitySettings.SetQualityLevel(original, false);
        AssetDatabase.SaveAssets();
        Debug.Log("Break to Escape: URP tier assets created; " + assigned + " of 3 quality levels assigned." +
                  (assigned < 3 ? " Create quality levels named Low, Mid and High (Project Settings > Quality) and run this again." : ""));
    }
}
#endif
