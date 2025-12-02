using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages enemy skins by applying AnimatorOverrideController based on skin ID
/// Requires dog2 animation clips to be pre-created and placed in Resources/Animations/Dog2/
/// </summary>
public class EnemySkinManager : MonoBehaviour
{
    private static readonly string[] SkinAnimationNames = { "Attack", "Death", "Idle", "Jump", "Walk" };
    private static readonly Dictionary<int, Dictionary<string, AnimationClip>> dogClipCache =
        new Dictionary<int, Dictionary<string, AnimationClip>>();
    private static readonly Dictionary<int, Dictionary<string, AnimationClip>> catClipCache =
        new Dictionary<int, Dictionary<string, AnimationClip>>();

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

        if (skinId <= 1) return;

        switch (enemyType)
        {
            case "Dog":
                ApplyDogSkin(animator, skinId);
                break;
            case "Cat":
                ApplyCatSkin(animator, skinId);
                break;
        }
    }

    /// <summary>
    /// Apply dog skin overrides (skinId >= 2).
    /// </summary>
    private static void ApplyDogSkin(Animator animator, int skinId)
    {
        // Get the original animator controller
        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;

        // Don't create override if already applied
        if (originalController is AnimatorOverrideController)
        {
            AnimatorOverrideController existing = (AnimatorOverrideController)originalController;
            if (existing.name.Contains($"Dog{skinId}_Override"))
            {
                return; // Already applied
            }
        }

        Dictionary<string, AnimationClip> dogClips = LoadDogAnimationClips(skinId);

        if (dogClips.Count == 0)
        {
            Debug.LogWarning($"EnemySkinManager: No dog{skinId} animation clips found in Resources/Animations/Dog{skinId}/");
            return;
        }

        // Create new override controller
        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);
        overrideController.name = $"Dog{skinId}_Override";

        // Get all animation clips from the original controller
        AnimationClip[] originalClips = originalController.animationClips;

        // Create override list
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

        foreach (AnimationClip originalClip in originalClips)
        {
            string clipName = originalClip.name;

            if (dogClips.TryGetValue(clipName, out AnimationClip overrideClip))
            {
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, overrideClip));
                Debug.Log($"EnemySkinManager: Overriding {clipName} with dog{skinId} version");
            }
        }

        if (overrides.Count > 0)
        {
            // Apply all overrides at once
            overrideController.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = overrideController;
            Debug.Log($"EnemySkinManager: Applied {overrides.Count} animation overrides for dog{skinId} skin");
        }
        else
        {
            Debug.LogWarning("EnemySkinManager: No matching animation clips found to override");
        }
    }

    /// <summary>
    /// Apply cat skin overrides (skinId >= 2).
    /// </summary>
    private static void ApplyCatSkin(Animator animator, int skinId)
    {
        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;

        if (originalController is AnimatorOverrideController existing && existing.name.Contains($"Cat{skinId}_Override"))
        {
            return;
        }

        Dictionary<string, AnimationClip> catClips = LoadCatAnimationClips(skinId);
        if (catClips.Count == 0)
        {
            Debug.LogWarning($"EnemySkinManager: No cat{skinId} animation clips found in Resources/Animations/Cat{skinId}/");
            return;
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController)
        {
            name = $"Cat{skinId}_Override"
        };

        AnimationClip[] originalClips = originalController.animationClips;
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

        foreach (AnimationClip originalClip in originalClips)
        {
            if (catClips.TryGetValue(originalClip.name, out AnimationClip overrideClip))
            {
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, overrideClip));
                Debug.Log($"EnemySkinManager: Overriding {originalClip.name} with cat{skinId} version");
            }
        }

        if (overrides.Count > 0)
        {
            overrideController.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = overrideController;
            Debug.Log($"EnemySkinManager: Applied {overrides.Count} animation overrides for cat{skinId} skin");
        }
        else
        {
            Debug.LogWarning("EnemySkinManager: No matching animation clips found to override (cat skin)");
        }
    }

    /// <summary>
    /// Load dog animation clips from Resources/Animations/Dog{skinId}/
    /// </summary>
    private static Dictionary<string, AnimationClip> LoadDogAnimationClips(int skinId)
    {
        if (!dogClipCache.TryGetValue(skinId, out Dictionary<string, AnimationClip> cache))
        {
            cache = new Dictionary<string, AnimationClip>();
            dogClipCache[skinId] = cache;
        }

        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

        foreach (string animName in SkinAnimationNames)
        {
            if (cache.TryGetValue(animName, out AnimationClip cachedClip) && cachedClip != null)
            {
                clips[animName] = cachedClip;
                continue;
            }

            string folder = $"Animations/Dog{skinId}";
            string resourcePath = $"{folder}/{animName}";
            AnimationClip clip = Resources.Load<AnimationClip>(resourcePath);

            if (clip == null)
            {
                resourcePath = $"{folder}/Dog{skinId}_{animName}";
                clip = Resources.Load<AnimationClip>(resourcePath);
            }

            if (clip != null)
            {
                clips[animName] = clip;
                cache[animName] = clip;
            }
        }

        return clips;
    }

    /// <summary>
    /// Load cat animation clips from Resources/Animations/Cat{skinId}/
    /// </summary>
    private static Dictionary<string, AnimationClip> LoadCatAnimationClips(int skinId)
    {
        if (!catClipCache.TryGetValue(skinId, out Dictionary<string, AnimationClip> cache))
        {
            cache = new Dictionary<string, AnimationClip>();
            catClipCache[skinId] = cache;
        }

        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

        foreach (string animName in SkinAnimationNames)
        {
            if (cache.TryGetValue(animName, out AnimationClip cachedClip) && cachedClip != null)
            {
                clips[animName] = cachedClip;
                continue;
            }

            string folder = $"Animations/Cat{skinId}";
            string resourcePath = $"{folder}/{animName}";
            AnimationClip clip = Resources.Load<AnimationClip>(resourcePath);

            if (clip == null)
            {
                resourcePath = $"{folder}/Cat{skinId}_{animName}";
                clip = Resources.Load<AnimationClip>(resourcePath);
            }

            if (clip != null)
            {
                clips[animName] = clip;
                cache[animName] = clip;
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
        if (go.GetComponent<PigeonController>() != null)
        {
            return "Pigeon";
        }
        if (go.name.ToLower().Contains("cat"))
        {
            return "Cat";
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
        if (name.Contains("cat"))
        {
            return "Cat";
        }

        return null;
    }
}
