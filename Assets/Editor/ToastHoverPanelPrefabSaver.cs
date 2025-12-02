#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ToastHoverPanelPrefabSaver
{
    private const string PrefabPath = "Assets/Prefabs/ToastHoverPanel.prefab";

    [MenuItem("Tools/Save ToastHoverPanel Prefab")]
    public static void SaveToastHoverPanelPrefab()
    {
        var panel = GameObject.Find("ToastHoverPanel");
        if (panel == null)
        {
            Debug.LogWarning("ToastHoverPanel GameObject not found in the scene.");
            return;
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(panel, PrefabPath, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();
        Debug.Log($"Saved prefab to {PrefabPath}");
    }
}
#endif
