using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor script to create dog2 animation clips from dog sprites
/// Call via menu: Assets > Create Dog2 Animations
/// </summary>
public class DogSkinAnimationCreator
{
    private const string BaseClipPath = "Assets/Animations/Dog";
    private const string BaseSpritePath = "Assets/Sprites/Dog";
    private static readonly string[] AnimationNames = { "Attack", "Death", "Idle", "Jump", "Walk" };

    [MenuItem("Assets/Create Dog2 Animations")]
    public static void CreateDog2Animations() => CreateDogAnimations(2);

    [MenuItem("Assets/Create Dog3 Animations")]
    public static void CreateDog3Animations() => CreateDogAnimations(3);

    [MenuItem("Assets/Create Dog4 Animations")]
    public static void CreateDog4Animations() => CreateDogAnimations(4);

    private static void CreateDogAnimations(int skinId)
    {
        string folderName = $"Dog{skinId}";
        string resourcesPath = $"Assets/Resources/Animations/{folderName}";

        EnsureFolder(resourcesPath, folderName);

        int created = 0;
        int skipped = 0;

        foreach (string animName in AnimationNames)
        {
            if (CreateAnimationClip(animName, skinId, resourcesPath))
            {
                created++;
            }
            else
            {
                skipped++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"{folderName} animations: {created} created, {skipped} skipped. Saved to {resourcesPath}");
        EditorUtility.DisplayDialog($"{folderName} Animations",
            $"Created {created} animations\nSkipped {skipped} animations\n\nSaved to: {resourcesPath}",
            "OK");
    }

    private static void EnsureFolder(string fullPath, string folderName)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Animations"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Animations");
        }
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Animations", folderName);
        }
    }

    private static bool CreateAnimationClip(string animName, int skinId, string outputPath)
    {
        // Check if output already exists
        string outputFilePath = $"{outputPath}/{animName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outputFilePath) != null)
        {
            Debug.Log($"Animation already exists, skipping: {outputFilePath}");
            return false;
        }

        // Load original animation clip
        string originalPath = $"{BaseClipPath}/{animName}.anim";
        AnimationClip originalClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(originalPath);

        if (originalClip == null)
        {
            Debug.LogError($"Original animation clip not found: {originalPath}");
            return false;
        }

        string spriteFilePath = $"{BaseSpritePath}/dog{skinId}_{animName}.png";
        string baseSpritePath = $"{BaseSpritePath}/{animName}.png";

        if (!EnsureVariantSpritesSliced(spriteFilePath, baseSpritePath))
        {
            Debug.LogError($"Failed to copy slice data for {spriteFilePath}");
            return false;
        }

        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spriteFilePath);

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"Dog{skinId} sprites not found: {spriteFilePath}");
            return false;
        }

        // Get sprite references (filter out non-Sprite objects)
        Sprite[] spriteFrames = System.Array.ConvertAll(
            System.Array.FindAll(sprites, obj => obj is Sprite),
            obj => obj as Sprite
        );

        if (spriteFrames.Length == 0)
        {
            Debug.LogError($"No sprite frames found in: {spriteFilePath}");
            return false;
        }

        // Create new animation clip
        AnimationClip newClip = new AnimationClip();
        newClip.frameRate = originalClip.frameRate;

        // Create keyframes for sprite animation
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[spriteFrames.Length];
        for (int i = 0; i < spriteFrames.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / newClip.frameRate,
                value = spriteFrames[i]
            };
        }

        // Create animation curve binding
        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        // Apply the keyframes to the animation clip
        AnimationUtility.SetObjectReferenceCurve(newClip, spriteBinding, keyframes);

        // Copy animation events from original clip
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(originalClip);
        if (events != null && events.Length > 0)
        {
            AnimationUtility.SetAnimationEvents(newClip, events);
        }

        // Set loop settings
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(originalClip);
        AnimationUtility.SetAnimationClipSettings(newClip, settings);

        // Save the new animation clip to Resources folder
        AssetDatabase.CreateAsset(newClip, outputFilePath);
        Debug.Log($"Created: {outputFilePath}");

        return true;
    }

    private static bool EnsureVariantSpritesSliced(string variantPath, string referencePath)
    {
        var variantImporter = AssetImporter.GetAtPath(variantPath) as TextureImporter;
        if (variantImporter == null)
        {
            Debug.LogError($"Variant sprite not found: {variantPath}");
            return false;
        }

        var referenceImporter = AssetImporter.GetAtPath(referencePath) as TextureImporter;
        if (referenceImporter == null)
        {
            Debug.LogError($"Reference sprite not found for slicing: {referencePath}");
            return false;
        }

        variantImporter.spriteImportMode = referenceImporter.spriteImportMode;
        variantImporter.spritePixelsPerUnit = referenceImporter.spritePixelsPerUnit;
        variantImporter.filterMode = referenceImporter.filterMode;
        variantImporter.wrapMode = referenceImporter.wrapMode;

        SpriteMetaData[] referenceSheet = referenceImporter.spritesheet;
        if (referenceSheet != null && referenceSheet.Length > 0)
        {
            SpriteMetaData[] sheetCopy = new SpriteMetaData[referenceSheet.Length];
            for (int i = 0; i < referenceSheet.Length; i++)
            {
                sheetCopy[i] = referenceSheet[i];
            }
            variantImporter.spritesheet = sheetCopy;
        }

        variantImporter.SaveAndReimport();
        return true;
    }
}
