using UnityEngine;

/// <summary>
/// Attach this to enemies to bind them to a SharedTrackingEngine
/// with per-enemy overrides for algorithm and view mode.
/// </summary>
public class SharedTrackingClient : MonoBehaviour
{
  [Header("Shared Engine Reference")]
  public SharedTrackingEngine engine;

  [Header("Overrides (optional)")]
  public SharedTrackingEngine.TrackingAlgorithm algorithmOverride;
  public bool useAlgorithmOverride = false;
  public SharedTrackingEngine.ViewMode viewModeOverride;
  public bool useViewModeOverride = false;

  [Header("Abilities")]
  [Tooltip("Enable jump ability for this clone")] public bool canJump = true;

  public SharedTrackingEngine.TrackingAlgorithm ActiveAlgorithm
    => useAlgorithmOverride ? algorithmOverride : (engine != null ? engine.defaultAlgorithm : SharedTrackingEngine.TrackingAlgorithm.DirectChase);

  public SharedTrackingEngine.ViewMode ActiveView
    => useViewModeOverride ? viewModeOverride : (engine != null ? engine.defaultView : SharedTrackingEngine.ViewMode.SideView2D);
}
