using UnityEngine;

/// <summary>
/// Triggers a fade transition when the player enters the portal collider.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EndPortal : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "NextScene";
    [SerializeField] private bool requirePlayerTag = true;

    private bool triggered;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
        {
            return;
        }

        if (requirePlayerTag && !other.CompareTag("Player"))
        {
            return;
        }

        triggered = true;

        if (ScreenFadeController.Instance != null)
        {
            ScreenFadeController.Instance.FadeToScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("[EndPortal] ScreenFadeController not found. Loading scene without fade.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
        }
    }
}
