using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 전환 시 플레이어 상태(HP, 스태미나, 토스트)를 저장하고 복원하는 매니저.
/// PlayerPrefs + JSON을 사용하여 씬 전환 간 데이터를 유지합니다.
/// </summary>
public class PlayerStateManager : MonoBehaviour
{
  public static PlayerStateManager instance;

  // PlayerPrefs 키 상수
  private const string KEY_PLAYER_DATA = "Player_SaveData";
  private const string KEY_STAMINA = "Player_Stamina";
  private const string KEY_HAS_SAVED_STATE = "Player_HasSavedState";

  private void Awake()
  {
    if (instance == null)
    {
      instance = this;
      DontDestroyOnLoad(gameObject);
      SceneManager.sceneLoaded += OnSceneLoaded;
      Debug.Log("[PlayerStateManager] 인스턴스 생성");
    }
    else
    {
      Destroy(gameObject);
    }
  }

  private void OnDestroy()
  {
    if (instance == this)
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
    }
  }

  private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    if (scene.name.StartsWith("Stage"))
    {
      StartCoroutine(RestoreStateDelayed());
    }
  }

  private System.Collections.IEnumerator RestoreStateDelayed()
  {
    yield return null;
    yield return null;
    RestorePlayerState();
  }

  /// <summary>
  /// 현재 플레이어 상태를 저장합니다.
  /// </summary>
  public void SavePlayerState()
  {
    var player = FindFirstObjectByType<PlayerController>();
    if (player == null)
    {
      Debug.LogWarning("[PlayerStateManager] PlayerController를 찾을 수 없습니다!");
      return;
    }

    // PlayerController의 GetSaveData()로 모든 데이터를 한번에 가져와서 JSON으로 저장
    var saveData = player.GetSaveData();
    PlayerPrefs.SetString(KEY_PLAYER_DATA, JsonUtility.ToJson(saveData));

    // 스태미나 저장
    if (StaminaManager.instance != null)
    {
      PlayerPrefs.SetFloat(KEY_STAMINA, StaminaManager.instance.GetCurrentStamina());
    }

    PlayerPrefs.SetInt(KEY_HAS_SAVED_STATE, 1);
    PlayerPrefs.Save();

    Debug.Log($"[PlayerStateManager] 저장 완료: HP={saveData.health}, Toast={saveData.toastId}");
  }

  /// <summary>
  /// 저장된 플레이어 상태를 복원합니다.
  /// </summary>
  public void RestorePlayerState()
  {
    if (PlayerPrefs.GetInt(KEY_HAS_SAVED_STATE, 0) != 1)
    {
      Debug.Log("[PlayerStateManager] 저장된 상태 없음");
      return;
    }

    var player = FindFirstObjectByType<PlayerController>();
    if (player == null)
    {
      Debug.LogWarning("[PlayerStateManager] PlayerController를 찾을 수 없습니다!");
      return;
    }

    // JSON에서 데이터 복원하여 PlayerController의 LoadSaveData()로 적용
    string json = PlayerPrefs.GetString(KEY_PLAYER_DATA, "");
    if (!string.IsNullOrEmpty(json))
    {
      var saveData = JsonUtility.FromJson<PlayerController.PlayerSaveData>(json);
      player.LoadSaveData(saveData);
    }

    // 스태미나 복원
    if (StaminaManager.instance != null)
    {
      SetStamina(PlayerPrefs.GetFloat(KEY_STAMINA, 0f));
    }

    Debug.Log("[PlayerStateManager] 복원 완료!");
  }

  /// <summary>
  /// 저장된 상태를 클리어합니다.
  /// </summary>
  public void ClearSavedState()
  {
    PlayerPrefs.DeleteKey(KEY_PLAYER_DATA);
    PlayerPrefs.DeleteKey(KEY_STAMINA);
    PlayerPrefs.DeleteKey(KEY_HAS_SAVED_STATE);
    PlayerPrefs.Save();
    Debug.Log("[PlayerStateManager] 저장 데이터 클리어");
  }

  private void SetStamina(float value)
  {
    if (StaminaManager.instance == null) return;

    var field = typeof(StaminaManager).GetField("currentStamina",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    field?.SetValue(StaminaManager.instance, value);

    var method = typeof(StaminaManager).GetMethod("UpdateStaminaUI",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    method?.Invoke(StaminaManager.instance, null);
  }
}
