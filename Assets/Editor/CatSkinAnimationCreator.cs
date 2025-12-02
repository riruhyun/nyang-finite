using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor helper to generate cat skin animation clips from sliced sprites.
/// Creates clips under Assets/Resources/Animations/Cat{n}/ so runtime skin switching can load them.
/// </summary>
public class CatSkinAnimationCreator
{
    private const string BaseClipPath = "Assets/Animations/Cat";
    private const string BaseSpritePath = "Assets/Sprites/Cat";
    private static readonly string[] AnimationNames = { "Attack", "Death", "Idle", "Jump", "Walk" };

    [MenuItem("Assets/Create Cat2 Animations")]
    public static void CreateCat2Animations() => CreateCatAnimations(2);

    [MenuItem("Assets/Create Cat3 Animations")]
    public static void CreateCat3Animations() => CreateCatAnimations(3);

    private static void CreateCatAnimations(int skinId)
    {
        string folderName = $"Cat{skinId}";
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
        string outputFilePath = $"{outputPath}/{animName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outputFilePath) != null)
        {
            Debug.Log($"Animation already exists, skipping: {outputFilePath}");
            return false;
        }

        // Cat은 Attack 대신 Punch를 사용 (원본 클립과 스프라이트 모두)
        string baseAnimName = animName == "Attack" ? "Punch" : animName;
        string originalPath = $"{BaseClipPath}/{baseAnimName}.anim";
        AnimationClip originalClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(originalPath);
        if (originalClip == null)
        {
            Debug.LogError($"Original cat animation clip not found: {originalPath}");
            return false;
        }

        string spriteFilePath = $"{BaseSpritePath}/cat{skinId}_{baseAnimName}.png";
        string referencePath = $"{BaseSpritePath}/{baseAnimName}.png";

        if (!EnsureVariantSpritesSliced(spriteFilePath, referencePath))
        {
            Debug.LogError($"Failed to copy slice data for {spriteFilePath}");
            return false;
        }

        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spriteFilePath);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"Cat{skinId} sprites not found: {spriteFilePath}");
            return false;
        }

        Sprite[] spriteFrames = System.Array.ConvertAll(
            System.Array.FindAll(sprites, obj => obj is Sprite),
            obj => obj as Sprite
        );

        if (spriteFrames.Length == 0)
        {
            Debug.LogError($"No sprite frames found in: {spriteFilePath}");
            return false;
        }

        AnimationClip newClip = new AnimationClip
        {
            frameRate = originalClip.frameRate
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[spriteFrames.Length];
        for (int i = 0; i < spriteFrames.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / newClip.frameRate,
                value = spriteFrames[i]
            };
        }

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(newClip, spriteBinding, keyframes);

        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(originalClip);
        if (events != null && events.Length > 0)
        {
            AnimationUtility.SetAnimationEvents(newClip, events);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(originalClip);
        AnimationUtility.SetAnimationClipSettings(newClip, settings);

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
