using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor script to create dog2 animation clips from dog sprites
/// Call via menu: Assets > Create Dog2 Animations
/// </summary>
public class DogSkinAnimationCreator
{
    [MenuItem("Assets/Create Dog2 Animations")]
    public static void CreateDog2Animations()
    {
        string basePath = "Assets/Animations/Dog";
        string spritePath = "Assets/Sprites/Dog";
        string resourcesPath = "Assets/Resources/Animations/Dog2";

        // Ensure Resources folder structure exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Animations"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Animations");
        }
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Animations", "Dog2");
        }

        // Animation clip names and their corresponding sprite files
        string[] animationNames = { "Attack", "Death", "Idle", "Jump", "Walk" };

        int created = 0;
        int skipped = 0;

        foreach (string animName in animationNames)
        {
            if (CreateAnimationClip(animName, basePath, spritePath, resourcesPath))
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

        Debug.Log($"Dog2 animations: {created} created, {skipped} skipped. Saved to {resourcesPath}");
        EditorUtility.DisplayDialog("Dog2 Animations",
            $"Created {created} animations\nSkipped {skipped} animations\n\nSaved to: {resourcesPath}",
            "OK");
    }

    private static bool CreateAnimationClip(string animName, string basePath, string spritePath, string outputPath)
    {
        // Check if output already exists
        string outputFilePath = $"{outputPath}/{animName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outputFilePath) != null)
        {
            Debug.Log($"Animation already exists, skipping: {outputFilePath}");
            return false;
        }

        // Load original animation clip
        string originalPath = $"{basePath}/{animName}.anim";
        AnimationClip originalClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(originalPath);

        if (originalClip == null)
        {
            Debug.LogError($"Original animation clip not found: {originalPath}");
            return false;
        }

        // Load dog2 sprites
        string dog2SpritePath = $"{spritePath}/dog2_{animName}.png";
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(dog2SpritePath);

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"Dog2 sprites not found: {dog2SpritePath}");
            return false;
        }

        // Get sprite references (filter out non-Sprite objects)
        Sprite[] spriteFrames = System.Array.ConvertAll(
            System.Array.FindAll(sprites, obj => obj is Sprite),
            obj => obj as Sprite
        );

        if (spriteFrames.Length == 0)
        {
            Debug.LogError($"No sprite frames found in: {dog2SpritePath}");
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
}
