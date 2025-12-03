using UnityEngine;

/// <summary>
/// Attach to spawned enemies to explicitly tag their kind (Dog/Cat/Rat/Pigeon),
/// so EnemySkinManager can override animations even when the base prefab uses another sprite.
/// </summary>
public class EnemyKindOverride : MonoBehaviour
{
  public EnemySpawnHelper.EnemyKind kind = EnemySpawnHelper.EnemyKind.Dog;
}
