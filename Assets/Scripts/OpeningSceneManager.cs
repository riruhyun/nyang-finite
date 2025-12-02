using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.Audio;

[DefaultExecutionOrder(-200)]
public class OpeningSceneManager : MonoBehaviour
{
    public static OpeningSceneManager Instance { get; private set; }
    [Header("Completion Transition")]
    public bool loadOnComplete = true;
    public string nextSceneName = "Lab";
    public float loadDelay = 0f;

    [Header("Interaction")]
    [Tooltip("Allow mouse click or any key to advance opening scenes.")]
    public bool allowClickToAdvance = true;
    [Tooltip("Require a click/any key AFTER each scene completes to advance. Ignores early clicks during motions.")]
    public bool requireClickAfterSceneComplete = false;

    [Header("Background Music")]
    [SerializeField] private bool playBgmOnStart = true;
    [SerializeField] private string bgmKey = "Opening_bgm";
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;
    [SerializeField] private bool bgmLoop = true;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [Header("SFX Mixer")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    public static AudioMixerGroup SfxMixerGroup { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("OpeningSceneManager.Awake: instance set.");
            DontDestroyOnLoad(gameObject);
            SfxMixerGroup = sfxMixerGroup;
        }
        else
        {
            Debug.LogWarning("OpeningSceneManager.Awake: duplicate found, destroying this instance.");
            Destroy(gameObject);
        }
    }

    private List<OpeningScene> scenes = new List<OpeningScene>();
    private int currentSceneIndex = 0;
    private bool isPlaying = false;
    private Coroutine playRoutine;
    private bool requestAdvance = false;
    private bool acceptAdvanceClick = false;
    private AudioSource bgmSource;

    public OpeningScene AddScene(int id)
    {
        OpeningScene scene = new OpeningScene(id);
        scene.SetRunner(this);
        scenes.Add(scene);
        Debug.Log($"OpeningSceneManager.AddScene: added scene id {id}");
        return scene;
    }

    public void StartOpening()
    {
        if (scenes.Count > 0 && !isPlaying)
        {
            isPlaying = true;
            currentSceneIndex = 0;
            if (playBgmOnStart)
            {
                PlayBackgroundMusic();
            }
            Debug.Log($"OpeningSceneManager.StartOpening: starting with {scenes.Count} scenes.");
            playRoutine = StartCoroutine(PlayScenes());
        }
        else if (scenes.Count == 0)
        {
            Debug.LogWarning("OpeningSceneManager.StartOpening: no scenes configured.");
        }
        else if (isPlaying)
        {
            Debug.Log("OpeningSceneManager.StartOpening: already playing.");
        }
    }

    private IEnumerator PlayScenes()
    {
        Debug.Log("OpeningSceneManager.PlayScenes: coroutine started.");
        while (currentSceneIndex < scenes.Count)
        {
            // Before starting this scene, trigger any scheduled fade-outs that should happen when this scene begins
            int sceneNumber = currentSceneIndex + 1; // expose 1-based number to callers
            for (int s = 0; s < currentSceneIndex; s++)
            {
                var prev = scenes[s];
                prev.TriggerScheduledFadeOuts(sceneNumber);
            }

            OpeningScene scene = scenes[currentSceneIndex];
            Debug.Log($"OpeningSceneManager.PlayScenes: playing index {currentSceneIndex}, name {scene.sceneName}, duration {scene.duration}");
            scene.Play();
            bool isLast = currentSceneIndex == scenes.Count - 1;
            // Determine effective interaction policy for this scene
            bool effectiveRequireClick = requireClickAfterSceneComplete;
            if (scene.requireClickAfterCompleteOverride.HasValue)
            {
                effectiveRequireClick = scene.requireClickAfterCompleteOverride.Value;
            }
            if (!isLast)
            {
                // During scene motion: only allow early advance if not requiring post-completion click
                bool allowEarly = allowClickToAdvance && !effectiveRequireClick;
                yield return WaitWithAdvance(scene.duration, allowEarly);
                // After motion completes: optionally wait for user click to advance
                if (effectiveRequireClick)
                {
                    yield return WaitForAdvanceClick();
                }
                else if (scene.postAdvanceDelay > 0f)
                {
                    // Hold a bit after completion before moving on
                    yield return WaitWithAdvance(scene.postAdvanceDelay, false);
                }
                if (scene.clearAllOnNext)
                {
                    scene.ClearActivatedObjects();
                }
            }
            else
            {
                // For the last scene: wait until fade-in completes, then 1s buffer before transition
                float wait = scene.GetFadeInCompletionTime() + 1f;
                Debug.Log($"OpeningSceneManager.PlayScenes: last scene, waiting {wait:F2}s (fade-in complete + 1s)");
                if (wait > 0f)
                {
                    // Do not allow skipping the last scene motion if require-click is enabled
                    bool allowEarly = allowClickToAdvance && !effectiveRequireClick;
                    yield return WaitWithAdvance(wait, allowEarly);
                }
                if (effectiveRequireClick)
                {
                    yield return WaitForAdvanceClick();
                }
                else if (scene.postAdvanceDelay > 0f)
                {
                    yield return WaitWithAdvance(scene.postAdvanceDelay, false);
                }
            }
            currentSceneIndex++;
        }

        isPlaying = false;
        Debug.Log("Opening sequence completed");

        if (loadOnComplete && !string.IsNullOrEmpty(nextSceneName))
        {
            if (loadDelay > 0f) yield return new WaitForSeconds(loadDelay);
            Debug.Log($"OpeningSceneManager: Loading next scene '{nextSceneName}'");
            StopBackgroundMusic();

            // 새 게임 시작 시 저장된 플레이어 상태 클리어
            if (PlayerStateManager.instance != null)
            {
                PlayerStateManager.instance.ClearSavedState();
                Debug.Log("[OpeningSceneManager] 새 게임 시작으로 저장된 상태 클리어");
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void ResetOpening(bool clearObjects = true)
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        StopAllCoroutines();
        if (clearObjects)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i] != null)
                {
                    scenes[i].ClearActivatedObjects();
                }
            }
        }
        scenes.Clear();
        currentSceneIndex = 0;
        isPlaying = false;
        Debug.Log("OpeningSceneManager: ResetOpening completed.");
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
        {
            if (acceptAdvanceClick)
                requestAdvance = true;
        }
    }

    private IEnumerator WaitWithAdvance(float seconds, bool allowEarlyAdvance)
    {
        requestAdvance = false;
        acceptAdvanceClick = allowEarlyAdvance;
        float t = 0f;
        while (t < seconds && !(allowEarlyAdvance && requestAdvance))
        {
            t += Time.deltaTime;
            yield return null;
        }
        requestAdvance = false;
        acceptAdvanceClick = false;
    }

    private IEnumerator WaitForAdvanceClick()
    {
        requestAdvance = false;
        acceptAdvanceClick = true;
        while (!requestAdvance)
        {
            yield return null;
        }
        requestAdvance = false;
        acceptAdvanceClick = false;
    }

    private void PlayBackgroundMusic()
    {
        if (string.IsNullOrEmpty(bgmKey)) return;

        var clip = Resources.Load<AudioClip>(bgmKey)
                   ?? Resources.Load<AudioClip>("Opening_Scene/" + bgmKey)
                   ?? Resources.Load<AudioClip>("sound/" + bgmKey);
        if (clip == null)
        {
            Debug.LogWarning($"OpeningSceneManager: BGM clip '{bgmKey}' not found (tried '', 'Opening_Scene/', 'sound/')");
            return;
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }
        if (bgmMixerGroup != null)
        {
            bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        }
        bgmSource.loop = bgmLoop;
        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
    }

    private void StopBackgroundMusic()
    {
        if (bgmSource == null) return;
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    private void OnDestroy()
    {
        StopBackgroundMusic();
    }
}
