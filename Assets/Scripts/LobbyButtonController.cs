using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LobbyButtonController : MonoBehaviour
{
    [Header("Auto-Setup")]
    [SerializeField] private bool autoFindButtons = true;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private float buttonMoveDistance = 15f;

    [Header("Scene Names")]
    [SerializeField] private string openingSceneName = "Opening";
    [SerializeField] private string settingsSceneName = "Settings";
    [SerializeField] private string tutorialSceneName = "Tutorial";

    private GameObject startButton;
    private GameObject settingButton;
    private GameObject continueButton;
    private GameObject tutorialButton;
    private SpriteRenderer fadePanel;
    private bool isAnimating = false;

    private void Start()
    {
        if (autoFindButtons)
        {
            startButton = GameObject.Find("start_button");
            settingButton = GameObject.Find("setting_button");
            continueButton = GameObject.Find("continue_button");
            tutorialButton = GameObject.Find("tutorial_button");

            GameObject fadePanelObj = GameObject.Find("FadePanel");
            if (fadePanelObj != null)
            {
                fadePanel = fadePanelObj.GetComponent<SpriteRenderer>();
            }

            if (fadePanel != null)
            {
                fadePanel.sortingOrder = 100;
                var autoFade = fadePanel.GetComponent<FadePanelAutoFade>();
                if (autoFade == null)
                {
                    autoFade = fadePanel.gameObject.AddComponent<FadePanelAutoFade>();
                }
                autoFade.StartFadeOut();
            }

            Debug.Log($"[LobbyButtonController] Auto-Setup: Start={startButton != null}, Setting={settingButton != null}, Continue={continueButton != null}, Tutorial={tutorialButton != null}, Fade={fadePanel != null}");
        }
    }

    public void OnButtonClicked(string buttonName)
    {
        if (isAnimating) return;

        GameObject clickedButton = null;
        GameObject[] otherButtons = null;
        string targetScene = null;

        if (buttonName == "start_button")
        {
            clickedButton = startButton;
            otherButtons = new GameObject[] { settingButton, continueButton, tutorialButton };
            targetScene = openingSceneName;
        }
        else if (buttonName == "setting_button")
        {
            clickedButton = settingButton;
            otherButtons = new GameObject[] { startButton, continueButton, tutorialButton };
            targetScene = settingsSceneName;
        }
        else if (buttonName == "continue_button")
        {
            clickedButton = continueButton;
            otherButtons = new GameObject[] { startButton, settingButton, tutorialButton };
            targetScene = null;
        }
        else if (buttonName == "tutorial_button")
        {
            clickedButton = tutorialButton;
            otherButtons = new GameObject[] { startButton, settingButton, continueButton };
            targetScene = tutorialSceneName;
        }

        if (clickedButton != null && otherButtons != null)
        {
            StartCoroutine(AnimateButtonClick(clickedButton, otherButtons, targetScene));
        }
    }

    private IEnumerator AnimateButtonClick(GameObject clickedButton, GameObject[] otherButtons, string targetScene)
    {
        isAnimating = true;
        Debug.Log($"[LobbyButtonController] {clickedButton.name} clicked! Target scene: {targetScene}");

        foreach (GameObject btn in otherButtons)
        {
            if (btn != null)
            {
                SpriteButtonHoverEffect hover = btn.GetComponent<SpriteButtonHoverEffect>();
                if (hover != null) hover.enabled = false;
            }
        }

        Vector3[] startPositions = new Vector3[otherButtons.Length];
        for (int i = 0; i < otherButtons.Length; i++)
        {
            if (otherButtons[i] != null)
            {
                startPositions[i] = otherButtons[i].transform.position;
            }
        }

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float easedT = EaseInOutCubic(t);

            for (int i = 0; i < otherButtons.Length; i++)
            {
                if (otherButtons[i] != null)
                {
                    Vector3 targetPos = startPositions[i] + new Vector3(buttonMoveDistance, 0, 0);
                    otherButtons[i].transform.position = Vector3.Lerp(startPositions[i], targetPos, easedT);
                }
            }

            if (fadePanel != null)
            {
                Color c = Color.black;
                c.a = easedT;
                fadePanel.color = c;
            }

            yield return null;
        }

        for (int i = 0; i < otherButtons.Length; i++)
        {
            if (otherButtons[i] != null)
            {
                Vector3 finalPos = startPositions[i] + new Vector3(buttonMoveDistance, 0, 0);
                otherButtons[i].transform.position = finalPos;
            }
        }

        if (fadePanel != null)
        {
            Color c = Color.black;
            c.a = 1f;
            fadePanel.color = c;
        }

        Debug.Log($"[LobbyButtonController] Animation complete for {clickedButton.name}!");

        if (!string.IsNullOrEmpty(targetScene))
        {
            Debug.Log($"[LobbyButtonController] Loading scene: {targetScene}");
            SceneManager.LoadScene(targetScene);
        }
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
