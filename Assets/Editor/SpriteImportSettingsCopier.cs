using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/// <summary>
/// Copies sprite slice settings (rect, pivot, border) between sprites.
/// Usage: Right-click sprite → Copy Sprite Slices, then Right-click target → Paste Sprite Slices
/// </summary>
public class SpriteImportSettingsCopier
{
    private static SpriteMetaData[] copiedSlices = null;
    private static TextureImporterSettings copiedSettings = null;
    private static string copiedSourcePath = null;

    [MenuItem("Assets/Copy Sprite Slices")]
    public static void CopySlices()
    {
        if (Selection.objects.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select a sprite to copy from", "OK");
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(Selection.objects[0]);
        if (string.IsNullOrEmpty(sourcePath) || !sourcePath.EndsWith(".png"))
        {
            EditorUtility.DisplayDialog("Error", "Please select a PNG sprite", "OK");
            return;
        }

        TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (sourceImporter == null || sourceImporter.spriteImportMode != SpriteImportMode.Multiple)
        {
            EditorUtility.DisplayDialog("Error", "Source must be a Multiple sprite (sliced)", "OK");
            return;
        }

        var sourceMeta = sourceImporter.spritesheet;
        if (sourceMeta == null || sourceMeta.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Source has no sprite slices", "OK");
            return;
        }

        // Copy slices
        copiedSlices = new SpriteMetaData[sourceMeta.Length];
        for (int i = 0; i < sourceMeta.Length; i++)
        {
            copiedSlices[i] = new SpriteMetaData
            {
                rect = sourceMeta[i].rect,
                alignment = sourceMeta[i].alignment,
                pivot = sourceMeta[i].pivot,
                border = sourceMeta[i].border
            };
        }

        // Copy general settings
        copiedSettings = new TextureImporterSettings();
        sourceImporter.ReadTextureSettings(copiedSettings);
        copiedSourcePath = sourcePath;

        Debug.Log($"✓ Copied {copiedSlices.Length} sprite slices from: {Path.GetFileName(sourcePath)}");
        EditorUtility.DisplayDialog("Copied",
            $"Copied {copiedSlices.Length} sprite slices from:\n{Path.GetFileName(sourcePath)}",
            "OK");
    }

    [MenuItem("Assets/Paste Sprite Slices")]
    public static void PasteSlices()
    {
        if (copiedSlices == null || copiedSlices.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No sprite slices copied. Use 'Copy Sprite Slices' first.", "OK");
            return;
        }

        if (Selection.objects.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select target sprite(s) to paste to", "OK");
            return;
        }

        int successCount = 0;
        int failCount = 0;

        foreach (var obj in Selection.objects)
        {
            string targetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(targetPath) || !targetPath.EndsWith(".png"))
            {
                failCount++;
                continue;
            }

            if (PasteToSprite(targetPath))
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"Pasted sprite slices:\n✓ Success: {successCount}\n✗ Failed: {failCount}";
        Debug.Log(message);
        EditorUtility.DisplayDialog("Complete", message, "OK");
    }

    [MenuItem("Assets/Copy Sprite Slices to Pattern")]
    public static void CopyToPattern()
    {
        if (Selection.objects.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select a source sprite image (e.g., dog2_Attack.png)", "OK");
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(Selection.objects[0]);
        if (string.IsNullOrEmpty(sourcePath) || !sourcePath.EndsWith(".png"))
        {
            EditorUtility.DisplayDialog("Error", "Please select a PNG sprite as source", "OK");
            return;
        }

        TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (sourceImporter == null || sourceImporter.spriteImportMode != SpriteImportMode.Multiple)
        {
            EditorUtility.DisplayDialog("Error", "Source must be a Multiple sprite (sliced into multiple sprites)", "OK");
            return;
        }

        // Extract pattern: dog2_Attack.png → pattern = "dog{N}_Attack"
        string fileName = Path.GetFileNameWithoutExtension(sourcePath);
        string sourceFolder = Path.GetDirectoryName(sourcePath);

        // Find digit in filename (e.g., "2" in "dog2_Attack")
        var match = Regex.Match(fileName, @"^(\D+?)(\d+)(.*)$");
        if (!match.Success)
        {
            EditorUtility.DisplayDialog("Error",
                $"Could not find number pattern in '{fileName}'.\nExpected format: prefix + number + suffix (e.g., dog2_Attack)",
                "OK");
            return;
        }

        string prefix = match.Groups[1].Value;    // "dog"
        string number = match.Groups[2].Value;    // "2"
        string suffix = match.Groups[3].Value;    // "_Attack"

        // Find all matching files in the same folder
        string[] allFiles = Directory.GetFiles(sourceFolder, "*.png");
        var targetPaths = new List<string>();

        foreach (string filePath in allFiles)
        {
            string targetFileName = Path.GetFileNameWithoutExtension(filePath);
            if (targetFileName == fileName) continue; // Skip source

            // Check if matches pattern (same prefix/suffix, different number)
            var targetMatch = Regex.Match(targetFileName, $@"^{Regex.Escape(prefix)}(\d+){Regex.Escape(suffix)}$");
            if (targetMatch.Success && targetMatch.Groups[1].Value != number)
            {
                // Convert absolute path to Unity asset path
                string relativePath = filePath.Replace('\\', '/');
                if (relativePath.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                }
                targetPaths.Add(relativePath);
            }
        }

        if (targetPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("No Targets Found",
                $"No files found matching pattern:\n{prefix}<N>{suffix}.png\n\nSource folder: {sourceFolder}",
                "OK");
            return;
        }

        // Show confirmation
        string targetList = string.Join("\n", targetPaths);
        if (!EditorUtility.DisplayDialog("Confirm Copy",
            $"Copy sprite slices from:\n{sourcePath}\n\nTo {targetPaths.Count} file(s):\n{targetList}\n\nContinue?",
            "Yes", "Cancel"))
        {
            return;
        }

        // Copy to all targets
        int copied = 0;
        foreach (string targetPath in targetPaths)
        {
            if (CopySpriteSettings(sourcePath, targetPath))
            {
                copied++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete",
            $"Successfully copied sprite slices to {copied}/{targetPaths.Count} files!",
            "OK");
    }

    private static bool PasteToSprite(string targetPath)
    {
        TextureImporter targetImporter = AssetImporter.GetAtPath(targetPath) as TextureImporter;

        if (targetImporter == null)
        {
            Debug.LogError($"Failed to get importer for {targetPath}");
            return false;
        }

        // Apply general settings
        if (copiedSettings != null)
        {
            targetImporter.SetTextureSettings(copiedSettings);
        }

        // Apply sprite sheet slices
        var targetMeta = targetImporter.spritesheet;

        if (targetMeta == null || targetMeta.Length != copiedSlices.Length)
        {
            Debug.LogWarning($"Target sprite count mismatch: copied {copiedSlices.Length} slices, target has {targetMeta?.Length ?? 0}");
            return false;
        }

        // Copy ONLY rect, pivot, border - keep original names
        for (int i = 0; i < copiedSlices.Length; i++)
        {
            targetMeta[i].rect = copiedSlices[i].rect;           // Slice position and size
            targetMeta[i].alignment = copiedSlices[i].alignment; // Pivot alignment type
            targetMeta[i].pivot = copiedSlices[i].pivot;         // Pivot position
            targetMeta[i].border = copiedSlices[i].border;       // 9-slice border/padding
            // targetMeta[i].name - 원래 이름 유지!
        }

        targetImporter.spritesheet = targetMeta;

        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"✓ Pasted {copiedSlices.Length} sprite slices to: {Path.GetFileName(targetPath)}");
        return true;
    }

    private static bool CopySpriteSettings(string sourcePath, string targetPath)
    {
        TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        TextureImporter targetImporter = AssetImporter.GetAtPath(targetPath) as TextureImporter;

        if (sourceImporter == null || targetImporter == null)
        {
            Debug.LogError($"Failed to get importer for {sourcePath} or {targetPath}");
            return false;
        }

        // Copy general texture settings
        targetImporter.textureType = sourceImporter.textureType;
        targetImporter.spriteImportMode = sourceImporter.spriteImportMode;
        targetImporter.spritePixelsPerUnit = sourceImporter.spritePixelsPerUnit;
        targetImporter.spritePivot = sourceImporter.spritePivot;
        targetImporter.filterMode = sourceImporter.filterMode;
        targetImporter.wrapMode = sourceImporter.wrapMode;
        targetImporter.alphaIsTransparency = sourceImporter.alphaIsTransparency;
        targetImporter.mipmapEnabled = sourceImporter.mipmapEnabled;

        // Copy sprite sheet data (for multiple sprites per sheet)
        if (sourceImporter.spriteImportMode == SpriteImportMode.Multiple)
        {
            var sourceMeta = sourceImporter.spritesheet;
            var targetMeta = targetImporter.spritesheet;

            if (sourceMeta == null || sourceMeta.Length == 0)
            {
                Debug.LogWarning($"Source has no sprite slices: {sourcePath}");
                return false;
            }

            if (targetMeta == null || targetMeta.Length != sourceMeta.Length)
            {
                Debug.LogWarning($"Target sprite count mismatch: source has {sourceMeta.Length} slices, target has {targetMeta?.Length ?? 0}");
                return false;
            }

            // Copy ONLY rect, pivot, border - keep original names
            for (int i = 0; i < sourceMeta.Length; i++)
            {
                targetMeta[i].rect = sourceMeta[i].rect;           // Slice position and size
                targetMeta[i].alignment = sourceMeta[i].alignment; // Pivot alignment type
                targetMeta[i].pivot = sourceMeta[i].pivot;         // Pivot position
                targetMeta[i].border = sourceMeta[i].border;       // 9-slice border/padding
                // targetMeta[i].name - 원래 이름 유지!
            }

            targetImporter.spritesheet = targetMeta;
        }

        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"✓ Copied {sourceImporter.spritesheet?.Length ?? 0} sprite slices: {sourcePath} → {targetPath}");
        return true;
    }
}
