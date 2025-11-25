using UnityEngine;
using Pathfinding;

/// <summary>
/// Forces A* Path Log to be disabled at runtime to silence "Path Completed" logs.
/// Attach to any always-present GameObject (e.g., GameManager).
/// </summary>
public class PathLogDisabler : MonoBehaviour
{
    private void Awake()
    {
        if (AstarPath.active != null)
        {
            AstarPath.active.logPathResults = PathLog.None;
        }
    }
}
