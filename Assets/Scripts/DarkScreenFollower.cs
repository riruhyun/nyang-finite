using UnityEngine;

/// <summary>
/// Keeps the DarkScreen overlay aligned to the player (with optional offset).
/// </summary>
public class DarkScreenFollower : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = Vector3.zero;
    [SerializeField] private bool freezeRotation = true;

    private void Awake()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target != null && offset == Vector3.zero)
        {
            offset = transform.position - target.position;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;

        if (freezeRotation)
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
