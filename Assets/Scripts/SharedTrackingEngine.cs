using UnityEngine;

/// <summary>
/// A lightweight shared tracking engine that can be attached once to an owner GameObject
/// and referenced by many enemies (clones). It centralizes target lookup and provides
/// a place to switch tracking algorithms without duplicating work per clone.
/// </summary>
public class SharedTrackingEngine : MonoBehaviour
{
  public enum TrackingAlgorithm
  {
    DirectChase,
    AStar,
    Flocking
  }

  public enum ViewMode
  {
    SideView2D,
    TopDown2D
  }

  [Header("Shared Target")]
  [Tooltip("Target transform to track (auto-fills Player tag if null)")]
  public Transform target;

  [Header("Defaults")]
  public TrackingAlgorithm defaultAlgorithm = TrackingAlgorithm.DirectChase;
  public ViewMode defaultView = ViewMode.SideView2D;

  void Awake()
  {
    if (target == null)
    {
      var player = GameObject.FindGameObjectWithTag("Player");
      if (player != null) target = player.transform;
    }
  }
}

