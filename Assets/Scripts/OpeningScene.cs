using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class OpeningScene
{
    public string sceneName;
    public float duration;
    public GameObject sceneObject;
    public bool clearAllOnNext;
    public float soundDelay;
    // Per-scene interaction overrides
    public bool? requireClickAfterCompleteOverride; // null = use manager default
    public float postAdvanceDelay = 0f; // extra wait after scene completes when not requiring click
    private string soundKey;
    private float soundVolume = 1f;
    private bool soundLoop = false;
    private readonly List<OpeningImageAction> imageActions = new List<OpeningImageAction>();
    private readonly List<TrackedObject> activatedObjects = new List<TrackedObject>();
    private MonoBehaviour runner;

    public OpeningScene(string name, float time, GameObject obj = null)
    {
        sceneName = name;
        duration = time;
        sceneObject = obj;
    }

    // Convenience overload used by OpeningSceneManager.AddScene(int)
    public OpeningScene(int id)
    {
        sceneName = id.ToString();
        duration = 0f;
        sceneObject = null;
    }

    public void SetRunner(MonoBehaviour mono)
    {
        runner = mono;
    }

    // Fluent-like configuration API expected by OpeningSceneSetup
    public void SetDuration(float time)
    {
        duration = time;
    }

    public void ClearAllOnNext(bool value)
    {
        clearAllOnNext = value;
    }

    public void SetSoundDelay(float delay)
    {
        soundDelay = delay;
    }

    public void SetSound(string key)
    {
        soundKey = key;
    }

    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
    }

    public void SetSoundLoop(bool loop)
    {
        soundLoop = loop;
    }

    // Convenience: schedule fade-out for all images in this scene when a given scene number starts
    public void FadeOutAllAtScene(int sceneNumber, float duration)
    {
        for (int i = 0; i < imageActions.Count; i++)
        {
            imageActions[i].FadeOutAtScene(sceneNumber, duration);
        }
    }

    // Interaction control per scene
    // If set, overrides OpeningSceneManager.requireClickAfterSceneComplete just for this scene
    public void RequireClickAfterComplete(bool value)
    {
        requireClickAfterCompleteOverride = value;
    }

    // Do not require click; instead wait for given seconds AFTER the scene's duration completes
    public void AutoAdvanceAfterComplete(float seconds)
    {
        requireClickAfterCompleteOverride = false;
        postAdvanceDelay = Mathf.Max(0f, seconds);
    }

    // Minimal placeholder for image actions to satisfy compile-time usage.
    public OpeningImageAction AddImage(string resourceKey, Vector3 position, int sortingOrder)
    {
        var action = new OpeningImageAction(resourceKey, position, sortingOrder);
        imageActions.Add(action);
        return action;
    }

    // Nested helper with chainable no-op methods
    public class OpeningImageAction
    {
        internal string key;
        internal Vector3 startPos;
        internal int sortingOrder;
        internal float startDelay;
        internal float moveDelay;
        internal float fadeDuration;
        internal Vector3? moveTarget;
        internal float moveDuration;
        internal int? destroyAtSeconds;
        internal GameObject target;
        internal bool createdByUs;
        internal Vector3? scaleOverride;
        internal int? fadeOutAtSceneIndex;
        internal float fadeOutDuration;
        internal bool fadeOutScheduled;

        public OpeningImageAction(string key, Vector3 startPos, int sortingOrder)
        {
            this.key = key;
            this.startPos = startPos;
            this.sortingOrder = sortingOrder;
        }

        public OpeningImageAction StartDelay(float seconds) { startDelay = seconds; return this; }
        public OpeningImageAction MoveDelay(float seconds) { moveDelay = seconds; return this; }
        public OpeningImageAction ImageFadeIn(float seconds) { fadeDuration = seconds; return this; }
        public OpeningImageAction MoveTo(Vector3 target, float seconds) { moveTarget = target; moveDuration = seconds; return this; }
        public OpeningImageAction DestroyAt(int seconds) { destroyAtSeconds = seconds; return this; }
        // Scale chainers
        public OpeningImageAction Scale(float uniform)
        {
            scaleOverride = new Vector3(uniform, uniform, 1f);
            return this;
        }
        public OpeningImageAction Scale(Vector2 xy)
        {
            scaleOverride = new Vector3(xy.x, xy.y, 1f);
            return this;
        }
        public OpeningImageAction FadeOutAtScene(int sceneNumber, float duration)
        {
            fadeOutAtSceneIndex = Mathf.Max(1, sceneNumber); // use 1-based scene numbering
            fadeOutDuration = Mathf.Max(0f, duration);
            return this;
        }
    }

    // Returns the time when all fade-ins would be completed (max of startDelay+fadeDuration)
    public float GetFadeInCompletionTime()
    {
        float max = 0f;
        for (int i = 0; i < imageActions.Count; i++)
        {
            var a = imageActions[i];
            if (a.fadeDuration > 0f)
            {
                float end = a.startDelay + a.fadeDuration;
                if (end > max) max = end;
            }
        }
        return max;
    }

    public void Play()
    {
        Debug.Log($"OpeningScene.Play: '{sceneName}' with {imageActions.Count} image actions");
        // Kick off configured image actions
        if (runner != null)
        {
            foreach (var action in imageActions)
            {
                runner.StartCoroutine(RunImageAction(action));
            }
            if (!string.IsNullOrEmpty(soundKey))
            {
                runner.StartCoroutine(PlaySoundRoutine());
            }
        }

        if (sceneObject != null)
        {
            sceneObject.SetActive(true);
            Debug.Log($"Playing opening scene: {sceneName}");
        }
        else
        {
            Debug.LogWarning($"OpeningScene '{sceneName}' has no GameObject assigned!");
        }
    }

    // Called by OpeningSceneManager when a specific scene number starts (1-based)
    internal void TriggerScheduledFadeOuts(int sceneNumber)
    {
        if (runner == null) return;
        for (int i = 0; i < imageActions.Count; i++)
        {
            var a = imageActions[i];
            if (a.fadeOutScheduled) continue;
            if (a.fadeOutAtSceneIndex.HasValue && a.fadeOutAtSceneIndex.Value == sceneNumber)
            {
                if (a.target != null)
                {
                    a.fadeOutScheduled = true;
                    runner.StartCoroutine(FadeOutAndCleanupRoutine(a));
                }
            }
        }
    }

    public void Stop()
    {
        if (sceneObject != null)
        {
            sceneObject.SetActive(false);
        }
    }

    public void ClearActivatedObjects()
    {
        for (int i = 0; i < activatedObjects.Count; i++)
        {
            var t = activatedObjects[i];
            if (t.go == null) continue;
            if (t.created)
            {
                Object.Destroy(t.go);
            }
            else
            {
                t.go.SetActive(false);
            }
        }
        activatedObjects.Clear();
    }

    private IEnumerator RunImageAction(OpeningImageAction a)
    {
        Debug.Log($"OpeningScene.RunImageAction: begin key='{a.key}'");
        // Locate or create target GameObject
        if (a.target == null)
        {
            a.target = FindInSceneIncludingInactive(a.key);
            a.createdByUs = false;
        }
        if (a.target != null)
        {
            Debug.Log($"OpeningScene.RunImageAction: found existing GO '{a.key}' in scene");
        }
        if (a.target == null)
        {
            // Try load prefab (direct and under common base folder)
            var prefab = Resources.Load<GameObject>(a.key) ?? Resources.Load<GameObject>($"Opening_Scene/{a.key}");
            if (prefab != null)
            {
                a.target = Object.Instantiate(prefab);
                a.createdByUs = true;
                Debug.Log($"OpeningScene.RunImageAction: instantiated prefab from Resources '{(Resources.Load<GameObject>(a.key)!=null?a.key:$"Opening_Scene/{a.key}")}'");
            }
        }
        if (a.target == null)
        {
            // Try load sprite and create a GO (direct and under common base folder)
            var sprite = Resources.Load<Sprite>(a.key) ?? Resources.Load<Sprite>($"Opening_Scene/{a.key}");
            if (sprite != null)
            {
                a.target = new GameObject(a.key, typeof(SpriteRenderer));
                var sr = a.target.GetComponent<SpriteRenderer>();
                sr.sprite = sprite;
                a.createdByUs = true;
                Debug.Log($"OpeningScene.RunImageAction: created GO with Sprite from Resources '{(Resources.Load<Sprite>(a.key)!=null?a.key:$"Opening_Scene/{a.key}")}'");
            }
        }

        if (a.target == null)
        {
            // Try load texture and create a SpriteRenderer from it
            var tex = Resources.Load<Texture2D>(a.key) ?? Resources.Load<Texture2D>($"Opening_Scene/{a.key}");
            if (tex != null)
            {
                a.target = new GameObject(a.key, typeof(SpriteRenderer));
                var rect = new Rect(0, 0, tex.width, tex.height);
                var pivot = new Vector2(0.5f, 0.5f);
                var spr = Sprite.Create(tex, rect, pivot, 100f);
                var sr2 = a.target.GetComponent<SpriteRenderer>();
                sr2.sprite = spr;
                a.createdByUs = true;
                Debug.Log($"OpeningScene.RunImageAction: created GO from Texture2D in Resources '{(Resources.Load<Texture2D>(a.key)!=null?a.key:$"Opening_Scene/{a.key}")}'");
            }
        }

        if (a.target == null)
        {
            // Final fallback: create a white square so we can see pipeline working
            a.target = new GameObject($"{a.key}_Fallback", typeof(SpriteRenderer));
            var tex = Texture2D.whiteTexture;
            var rect = new Rect(0, 0, tex.width, tex.height);
            var pivot = new Vector2(0.5f, 0.5f);
            var whiteSprite = Sprite.Create(tex, rect, pivot, 100f);
            var sr = a.target.GetComponent<SpriteRenderer>();
            sr.sprite = whiteSprite;
            sr.color = new Color(0.2f, 0.6f, 1f, 1f); // light blue to be visible
            a.createdByUs = true;
            Debug.LogWarning($"OpeningScene: Using fallback white square for '{a.key}' (resource not found)");
        }

        // If fading in, ensure fully transparent and not active until fade starts
        if (a.fadeDuration > 0f)
        {
            EnsureAlpha(a.target, 0f);
            a.target.SetActive(false);
        }
        else
        {
            EnsureAlpha(a.target, 1f);
            a.target.SetActive(true);
        }

        // Position and sorting
        a.target.transform.position = a.startPos;
        if (a.scaleOverride.HasValue)
        {
            a.target.transform.localScale = a.scaleOverride.Value;
        }
        var srComp = a.target.GetComponent<SpriteRenderer>();
        if (srComp != null)
        {
            srComp.sortingOrder = a.sortingOrder;
        }

        // Track for clearing later
        activatedObjects.Add(new TrackedObject { go = a.target, created = a.createdByUs });

        if (a.startDelay > 0f)
            yield return new WaitForSeconds(a.startDelay);

        if (a.fadeDuration > 0f)
        {
            Debug.Log($"OpeningScene.RunImageAction: fade-in {a.fadeDuration}s for '{a.key}'");
            EnsureAlpha(a.target, 0f);
            a.target.SetActive(true);
            runner.StartCoroutine(FadeInRoutine(a.target, a.fadeDuration));
        }
        else
        {
            a.target.SetActive(true);
            EnsureAlpha(a.target, 1f);
        }

        if (a.moveTarget.HasValue && a.moveDuration > 0f)
        {
            Debug.Log($"OpeningScene.RunImageAction: move to {a.moveTarget.Value} over {a.moveDuration}s for '{a.key}'");
            runner.StartCoroutine(MoveRoutine(a.target, a.moveTarget.Value, a.moveDelay, a.moveDuration));
        }

        if (a.destroyAtSeconds.HasValue)
        {
            runner.StartCoroutine(DestroyRoutine(a));
        }
    }

    private IEnumerator FadeInRoutine(GameObject go, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            EnsureAlpha(go, p);
            yield return null;
        }
        if (go != null) EnsureAlpha(go, 1f);
    }

    private IEnumerator MoveRoutine(GameObject go, Vector3 target, float delay, float duration)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (go == null) yield break;
        Vector3 start = go.transform.position;
        float t = 0f;
        while (t < duration)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            go.transform.position = Vector3.Lerp(start, target, p);
            yield return null;
        }
        if (go != null) go.transform.position = target;
    }

    private IEnumerator DestroyRoutine(OpeningImageAction a)
    {
        yield return new WaitForSeconds(Mathf.Max(0, a.destroyAtSeconds.Value));
        if (a.target == null) yield break;
        if (a.createdByUs) Object.Destroy(a.target);
        else a.target.SetActive(false);
        Debug.Log($"OpeningScene.RunImageAction: cleaned '{a.key}'");
    }

    private IEnumerator FadeOutAndCleanupRoutine(OpeningImageAction a)
    {
        if (a.target == null) yield break;
        float duration = Mathf.Max(0f, a.fadeOutDuration);
        if (duration <= 0f)
        {
            // immediate cleanup
            if (a.createdByUs) Object.Destroy(a.target);
            else a.target.SetActive(false);
            yield break;
        }
        float t = 0f;
        while (t < duration)
        {
            if (a.target == null) yield break;
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float alpha = 1f - p;
            EnsureAlpha(a.target, alpha);
            yield return null;
        }
        if (a.target != null)
        {
            if (a.createdByUs) Object.Destroy(a.target);
            else a.target.SetActive(false);
        }
        Debug.Log($"OpeningScene: faded out and cleaned '{a.key}' at scheduled scene");
    }

    private IEnumerator PlaySoundRoutine()
    {
        if (soundDelay > 0f) yield return new WaitForSeconds(soundDelay);
        if (runner == null || string.IsNullOrEmpty(soundKey)) yield break;
        // Load order: direct, Opening_Scene/, sound/ (for Resources/sound/)
        var clip = Resources.Load<AudioClip>(soundKey)
                   ?? Resources.Load<AudioClip>("Opening_Scene/" + soundKey)
                   ?? Resources.Load<AudioClip>("sound/" + soundKey);
        if (clip == null)
        {
            Debug.LogWarning($"OpeningScene: could not load AudioClip '{soundKey}' (tried '', 'Opening_Scene/', 'sound/')");
            yield break;
        }

        // Find or create SFX AudioSource (separate from BGM)
        AudioSource sfxSource = GetOrCreateSFXAudioSource();

        // Stop previous sound effect (but not BGM)
        if (sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }

        sfxSource.volume = soundVolume;
        if (soundLoop)
        {
            sfxSource.loop = true;
            sfxSource.clip = clip;
            sfxSource.Play();
        }
        else
        {
            sfxSource.PlayOneShot(clip, soundVolume);
        }
    }

    // Get or create a dedicated SFX AudioSource (separate from BGM)
    private AudioSource GetOrCreateSFXAudioSource()
    {
        if (runner == null) return null;

        // Look for all AudioSources on the runner
        AudioSource[] sources = runner.gameObject.GetComponents<AudioSource>();

        // The first AudioSource is assumed to be BGM (created by OpeningSceneManager)
        // Find or create the second one for SFX
        if (sources.Length >= 2)
        {
            return sources[1]; // Return the SFX source
        }

        // If only one exists (BGM), create a new one for SFX
        AudioSource sfxSource = runner.gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        return sfxSource;
    }

    private static void EnsureAlpha(GameObject go, float a)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var c = sr.color; c.a = a; sr.color = c; return;
        }

        var img = go.GetComponent<Image>();
        if (img != null)
        {
            var c = img.color; c.a = a; img.color = c; return;
        }

        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = a; return;
        }
    }

    private static GameObject FindInSceneIncludingInactive(string name)
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var found = FindInChildrenByName(roots[i].transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static Transform FindInChildrenByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var res = FindInChildrenByName(child, name);
            if (res != null) return res;
        }
        return null;
    }

    private struct TrackedObject
    {
        public GameObject go;
        public bool created;
    }
}
