using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Opening 씬 구성 예제
/// 이 스크립트를 Opening 씬에 추가하고 원하는 대로 수정하세요!
/// </summary>
[DefaultExecutionOrder(-100)]
public class OpeningSceneSetup : MonoBehaviour
{
    private OpeningSceneManager manager;

    private void Awake()
    {
        // Try get manager on same GameObject, else find in scene, else create one
        manager = GetComponent<OpeningSceneManager>();
        if (manager == null)
        {
            manager = FindObjectOfType<OpeningSceneManager>();
            if (manager != null)
            {
                Debug.Log("OpeningSceneSetup.Awake: found OpeningSceneManager in scene.");
            }
        }
        if (manager == null)
        {
            var go = new GameObject("OpeningSceneManager");
            manager = go.AddComponent<OpeningSceneManager>();
            Debug.Log("OpeningSceneSetup.Awake: created new OpeningSceneManager.");
        }
    }

    private void Start()
    {
        if (manager == null)
        {
            Debug.LogError("OpeningSceneSetup: OpeningSceneManager could not be created/found.");
            return;
        }

        var activeScene = SceneManager.GetActiveScene().name;
        if (activeScene != "Opening")
        {
            Debug.Log($"OpeningSceneSetup: Skipping opening in '{activeScene}' scene.");
            return;
        }

        // Ensure manager starts fresh for this scene
        manager.ResetOpening();

        Debug.Log("OpeningSceneSetup: initializing opening sequence...");
        manager.requireClickAfterSceneComplete = true;
        SetupOpening();
        manager.StartOpening();
    }

    private void SetupOpening()
    {
        OpeningScene scene1 = manager.AddScene(1);
        scene1.SetDuration(1.25f);
        scene1.SetSound("pigeon");                                  
        scene1.SetSoundDelay(0f);                                                                              
        scene1.SetSoundVolume(1f);                                                                                          
        scene1.SetSoundLoop(false);
        scene1.AddImage("opening_scene1", new Vector3(-4.5f, 0, 0), 5)
        .Scale(0.75f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(-4f, 0, 0), 1f)
        .FadeOutAtScene(3, 0.25f);

        OpeningScene scene2 = manager.AddScene(2);
        scene2.SetDuration(1.25f);
        scene2.AddImage("opening_scene2", new Vector3(3.5f, 0, 0), 6)
        .Scale(0.73f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(3f, 0, 0), 1f)
        .FadeOutAtScene(3, 0.25f);

        OpeningScene scene3 = manager.AddScene(3);
        scene3.SetDuration(1.25f);
        scene3.AddImage("opening_scene3", new Vector3(-3.25f, -0.5f, 0), 4)
        .Scale(0.73f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(-3.25f, 0, 0), 1f)
        .FadeOutAtScene(5, 0.25f);

        OpeningScene scene4 = manager.AddScene(4);
        scene4.SetDuration(1.25f);
        scene4.AddImage("opening_scene4", new Vector3(3.25f, 0.5f, 0), 3)
        .Scale(0.73f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(3.25f, 0, 0), 1f)
        .FadeOutAtScene(5, 0.25f);

        OpeningScene scene5 = manager.AddScene(5);
        scene5.SetDuration(0);
        scene5.AddImage("opening_scene5", new Vector3(0, 0, 0), 2)
        .Scale(0.85f)
        .ImageFadeIn(0)
        .FadeOutAtScene(6, 0);

        OpeningScene scene6 = manager.AddScene(6);
        scene6.SetDuration(1.25f);
        scene6.RequireClickAfterComplete(false);
        scene6.AutoAdvanceAfterComplete(0.5f);
        scene6.AddImage("opening_scene6", new Vector3(0, 0, 0), 3)
        .Scale(0.73f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(0, 0, 0), 1f)
        .FadeOutAtScene(7, 0.25f);

        OpeningScene scene7 = manager.AddScene(7);
        scene7.SetDuration(1.25f);
        scene7.RequireClickAfterComplete(false);
        scene7.AutoAdvanceAfterComplete(0.5f);
        scene7.AddImage("opening_scene7", new Vector3(-3.75f, 0, 0), 4)
        .Scale(0.73f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(-3.25f, 0, 0), 1f)
        .FadeOutAtScene(9, 0.25f);

        OpeningScene scene8 = manager.AddScene(8);
        scene8.SetDuration(1.25f);
        scene8.RequireClickAfterComplete(false);
        scene8.AutoAdvanceAfterComplete(0.5f);
        scene8.AddImage("opening_scene8", new Vector3(2.75f, 0, 0), 3)
        .Scale(0.73f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(3.25f, 0, 0), 1f)
        .FadeOutAtScene(9, 0.25f);

        OpeningScene scene9 = manager.AddScene(9);
        scene9.SetDuration(1.25f);
        scene9.RequireClickAfterComplete(false);
        scene9.AutoAdvanceAfterComplete(0.5f);
        scene9.AddImage("opening_scene9", new Vector3(0, 0, 0), 3)
        .Scale(0.85f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(0, 0, 0), 1f)
        .FadeOutAtScene(10, 0.25f);

        OpeningScene scene10 = manager.AddScene(10);
        scene10.SetDuration(1.25f);
        scene10.AddImage("opening_scene10", new Vector3(0, 0, 0), 3)
        .Scale(0.85f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(0, 0, 0), 1f)
        .FadeOutAtScene(11, 0.25f);

        OpeningScene scene11 = manager.AddScene(11);
        scene11.SetDuration(1.25f);
        scene11.RequireClickAfterComplete(false);
        scene11.AutoAdvanceAfterComplete(0.5f);
        scene11.AddImage("opening_scene11", new Vector3(-4.25f, 0.5f, 0), 2)
        .Scale(0.85f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(-4.25f, 0, 0), 1f)
        .FadeOutAtScene(13, 0.25f);

        OpeningScene scene12 = manager.AddScene(12);
        scene12.SetDuration(1.25f);
        scene12.RequireClickAfterComplete(false);
        scene12.AutoAdvanceAfterComplete(2.5f);
        scene12.AddImage("opening_scene12", new Vector3(2.75f, -0.5f, 0), 2)  
        .Scale(0.85f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(2.75f, 0, 0), 1f)
        .FadeOutAtScene(13, 0.25f);

        OpeningScene scene13 = manager.AddScene(13);
        scene13.SetDuration(1.25f);
        scene13.AddImage("opening_scene13", new Vector3(0, 0, 0), 2)
        .Scale(0.73f)
        .ImageFadeIn(1.25f)
        .MoveTo(new Vector3(0, 0, 0), 1f)
        .FadeOutAtScene(14, 0.25f);
    }
}
