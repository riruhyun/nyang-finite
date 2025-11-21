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
    
    private GameObject startButton;
    private GameObject settingButton;
    private GameObject continueButton;
    private SpriteRenderer fadePanel;
    private bool isAnimating = false;
    
    private void Start()
    {
        if (autoFindButtons)
        {
            startButton = GameObject.Find("start_button");
            settingButton = GameObject.Find("setting_button");
            continueButton = GameObject.Find("continue_button");
            
            GameObject fadePanelObj = GameObject.Find("FadePanel");
            if (fadePanelObj != null)
            {
                fadePanel = fadePanelObj.GetComponent<SpriteRenderer>();
            }
            
            if (fadePanel != null)
            {
                Color c = Color.black;
                c.a = 0f;
                fadePanel.color = c;
                fadePanel.sortingOrder = 100;
            }
            
            Debug.Log($"[LobbyButtonController] Auto-Setup: Start={startButton!=null}, Setting={settingButton!=null}, Continue={continueButton!=null}, Fade={fadePanel!=null}");
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
            otherButtons = new GameObject[] { settingButton, continueButton };
            targetScene = openingSceneName;
        }
        else if (buttonName == "setting_button")
        {
            clickedButton = settingButton;
            otherButtons = new GameObject[] { startButton, continueButton };
            targetScene = settingsSceneName;
        }
        else if (buttonName == "continue_button")
        {
            clickedButton = continueButton;
            otherButtons = new GameObject[] { startButton, settingButton };
            targetScene = null;
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
