using UnityEngine;

/// <summary>
/// 카메라 위치를 따라가는 배경
/// Z 위치는 유지
/// </summary>
public class BackgroundFollower : MonoBehaviour
{
    private Camera mainCamera;
    private float originalZ;
    
    private void Start()
    {
        mainCamera = Camera.main;
        originalZ = transform.position.z;
        
        if (mainCamera == null)
        {
            Debug.LogError("BackgroundFollower: Main Camera를 찾을 수 없습니다!");
            enabled = false;
        }
    }
    
    private void LateUpdate()
    {
        if (mainCamera == null) return;
        
        // 카메라 XY 위치를 따라가되, Z는 유지
        Vector3 newPos = mainCamera.transform.position;
        newPos.z = originalZ;
        transform.position = newPos;
    }
}
