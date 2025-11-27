using UnityEngine;

/// <summary>
/// Makes an object float up and down using a sine wave animation.
/// Attach this to the PressE object for a subtle floating effect.
/// </summary>
public class FloatingAnimation : MonoBehaviour
{
    [Header("Float Settings")]
    [SerializeField] private float floatAmplitude = 0.15f; // How far it moves up/down
    [SerializeField] private float floatSpeed = 2f; // How fast it oscillates
    
    private Vector3 startPosition;
    private float timeOffset;

    private void Start()
    {
        startPosition = transform.position;
        // Random offset so multiple floating objects don't sync
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float yOffset = Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPosition.x, startPosition.y + yOffset, startPosition.z);
    }
}