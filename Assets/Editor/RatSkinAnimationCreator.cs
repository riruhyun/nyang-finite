using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates rat skin animation clips (Attack/Death/Idle/Jump/Walk) from sliced sprites
/// and saves them under Assets/Resources/Animations/Rat{n}/.
/// Cat 스킨 생성기와 동일한 방식으로 작동합니다.
/// </summary>
public static class RatSkinAnimationCreator
{
    private const string BaseClipPath = "Assets/Animations/Cat";
    private const string BaseSpritePath = "Assets/Sprites/Rat";
    private const string ReferenceSpritePath = "Assets/Sprites/Cat";
    private static readonly string[] AnimationNames = { "Attack", "Death", "Idle", "Jump", "Walk" };

    [MenuItem("Assets/Create Rat Animations...")]
    public static void OpenRatWindow() => RatSkinAnimationCreatorWindow.ShowWindow();

    [MenuItem("Assets/Create Rat2 Animations")]
    public static void CreateRat2Animations() => CreateRatAnimationsInternal(2);

    [MenuItem("Assets/Create Rat3 Animations")]
    public static void CreateRat3Animations() => CreateRatAnimationsInternal(3);

    private static void CreateRatAnimationsInternal(int skinId)
    {
        string folderName = $"Rat{skinId}";
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

        string baseAnimName = animName == "Attack" ? "Punch" : animName;
        string originalPath = $"{BaseClipPath}/{baseAnimName}.anim";
        AnimationClip originalClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(originalPath);
        if (originalClip == null)
        {
            Debug.LogError($"Original rat animation clip not found: {originalPath}");
            return false;
        }

        string spriteFilePath = $"{BaseSpritePath}/rat_{baseAnimName}.png";
        string referencePath = $"{ReferenceSpritePath}/{baseAnimName}.png";

        if (!EnsureVariantSpritesSliced(spriteFilePath, referencePath))
        {
            Debug.LogError($"Failed to copy slice data for {spriteFilePath}");
            return false;
        }

        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spriteFilePath);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"Rat{skinId} sprites not found: {spriteFilePath}");
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

        var keyframes = new ObjectReferenceKeyframe[spriteFrames.Length];
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

    private class RatSkinAnimationCreatorWindow : EditorWindow
    {
        private int skinId = 2;

        public static void ShowWindow()
        {
            var window = GetWindow<RatSkinAnimationCreatorWindow>("Create Rat Animations");
            window.minSize = new Vector2(250, 80);
        }

        private void OnGUI()
        {
            GUILayout.Label("Rat Skin ID", EditorStyles.boldLabel);
            skinId = EditorGUILayout.IntField("Skin ID", Mathf.Max(2, skinId));
            GUILayout.Space(10f);
            if (GUILayout.Button("Create Animations"))
            {
                CreateRatAnimationsInternal(skinId);
                Close();
            }
        }
    }
}
