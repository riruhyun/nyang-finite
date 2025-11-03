using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Follow Settings")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;
    [SerializeField] private Vector3 offset = Vector3.zero;
    
    private Vector3 initialPosition;
    
    private void Start()
    {
        if (cameraTransform == null)
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObj != null)
            {
                cameraTransform = cameraObj.transform;
            }
            else
            {
                Debug.LogError("FollowCamera: Main Camera를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }
        
        initialPosition = transform.position;
    }
    
private void LateUpdate()
    {
        if (cameraTransform == null) return;
        
        Vector3 newPosition = transform.position;
        
        if (followX)
        {
            // X축만 카메라 따라가기 (배경/바닥 이동)
            newPosition.x = cameraTransform.position.x + offset.x;
        }
        
        if (followY)
        {
            // Y축도 따라가기 (옵션)
            newPosition.y = cameraTransform.position.y + offset.y;
        }
        
        transform.position = newPosition;
    }
}
