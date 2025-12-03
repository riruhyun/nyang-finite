using UnityEngine;

public class ButtonClickHandler : MonoBehaviour
{
    private LobbyButtonController controller;

    private void Start()
    {
        controller = FindObjectOfType<LobbyButtonController>();
        if (controller == null)
        {
            Debug.LogError("[ButtonClickHandler] LobbyButtonController not found!");
        }
    }

    private void OnMouseDown()
    {
        if (controller != null)
        {
            controller.OnButtonClicked(gameObject.name);
            Debug.Log($"[ButtonClickHandler] {gameObject.name} clicked!");
        }
    }
}
