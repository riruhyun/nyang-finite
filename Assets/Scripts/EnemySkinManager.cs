using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages enemy skins by applying AnimatorOverrideController based on skin ID
/// Requires dog2 animation clips to be pre-created and placed in Resources/Animations/Dog2/
/// </summary>
public class EnemySkinManager : MonoBehaviour
{
    private static Dictionary<string, AnimationClip> dog2ClipCache = new Dictionary<string, AnimationClip>();

    /// <summary>
    /// Apply skin to an enemy GameObject based on skin ID
    /// </summary>
    /// <param name="enemyGameObject">The enemy GameObject to apply skin to</param>
    /// <param name="skinId">Skin ID (1 = default, 2+ = override)</param>
    public static void ApplySkin(GameObject enemyGameObject, int skinId)
    {
        if (enemyGameObject == null || skinId <= 1)
        {
            // skinId 1 or less means use default animations, no override needed
            return;
        }

        Animator animator = enemyGameObject.GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"EnemySkinManager: No Animator found on {enemyGameObject.name}");
            return;
        }

        // Determine enemy type based on components or tags
        string enemyType = DetermineEnemyType(enemyGameObject);
        if (string.IsNullOrEmpty(enemyType))
        {
            Debug.LogWarning($"EnemySkinManager: Could not determine enemy type for {enemyGameObject.name}");
            return;
        }

        // For Dog with skinId 2, apply dog2 animations
        if (enemyType == "Dog" && skinId == 2)
        {
            ApplyDog2Skin(animator);
        }
        // Future: Add more enemy types and skin IDs here
    }

    /// <summary>
    /// Apply dog2 skin by overriding animation clips
    /// </summary>
    private static void ApplyDog2Skin(Animator animator)
    {
        // Get the original animator controller
        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;

        // Don't create override if already applied
        if (originalController is AnimatorOverrideController)
        {
            AnimatorOverrideController existing = (AnimatorOverrideController)originalController;
            if (existing.name.Contains("Dog2"))
            {
                return; // Already applied
            }
        }

        // Load dog2 animation clips from Resources
        Dictionary<string, AnimationClip> dog2Clips = LoadDog2AnimationClips();

        if (dog2Clips.Count == 0)
        {
            Debug.LogWarning("EnemySkinManager: No dog2 animation clips found in Resources/Animations/Dog2/");
            return;
        }

        // Create new override controller
        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);
        overrideController.name = "Dog2_Override";

        // Get all animation clips from the original controller
        AnimationClip[] originalClips = originalController.animationClips;

        // Create override list
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

        foreach (AnimationClip originalClip in originalClips)
        {
            string clipName = originalClip.name;

            // Try to find matching dog2 clip
            if (dog2Clips.ContainsKey(clipName))
            {
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, dog2Clips[clipName]));
                Debug.Log($"EnemySkinManager: Overriding {clipName} with dog2 version");
            }
        }

        if (overrides.Count > 0)
        {
            // Apply all overrides at once
            overrideController.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = overrideController;
            Debug.Log($"EnemySkinManager: Applied {overrides.Count} animation overrides for dog2 skin");
        }
        else
        {
            Debug.LogWarning("EnemySkinManager: No matching animation clips found to override");
        }
    }

    /// <summary>
    /// Load dog2 animation clips from Resources folder
    /// Expected path: Resources/Animations/Dog2/
    /// </summary>
    private static Dictionary<string, AnimationClip> LoadDog2AnimationClips()
    {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

        string[] animationNames = { "Attack", "Death", "Idle", "Jump", "Walk" };

        foreach (string animName in animationNames)
        {
            // Check cache first
            string cacheKey = $"Dog2_{animName}";
            if (dog2ClipCache.ContainsKey(cacheKey))
            {
                clips[animName] = dog2ClipCache[cacheKey];
                continue;
            }

            // Try loading from Resources
            string resourcePath = $"Animations/Dog2/{animName}";
            AnimationClip clip = Resources.Load<AnimationClip>(resourcePath);

            if (clip != null)
            {
                clips[animName] = clip;
                dog2ClipCache[cacheKey] = clip;
            }
            else
            {
                // Try alternative naming with Dog2_ prefix
                resourcePath = $"Animations/Dog2/Dog2_{animName}";
                clip = Resources.Load<AnimationClip>(resourcePath);

                if (clip != null)
                {
                    clips[animName] = clip;
                    dog2ClipCache[cacheKey] = clip;
                }
            }
        }

        return clips;
    }

    /// <summary>
    /// Determine enemy type from GameObject
    /// </summary>
    private static string DetermineEnemyType(GameObject go)
    {
        // Check for specific components
        if (go.GetComponent<IntelligentDogMovement>() != null)
        {
            return "Dog";
        }
        if (go.GetComponent<PigeonMovement>() != null)
        {
            return "Pigeon";
        }

        // Fallback to name checking
        string name = go.name.ToLower();
        if (name.Contains("dog"))
        {
            return "Dog";
        }
        if (name.Contains("pigeon"))
        {
            return "Pigeon";
        }

        return null;
    }
}
