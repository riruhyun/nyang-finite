using UnityEngine;

// 게임 오브젝트를 플레이어를 따라 카메라와 함께 움직이는 스크립트
public class ScrollingObject : MonoBehaviour {
    public float followSpeed = 2f; // 따라가는 속도
    
    private Transform player; // 플레이어의 Transform 참조
    private Vector3 offset; // 초기 오프셋

    private void Start() {
        // 플레이어 자동 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            // 초기 오프셋 저장
            offset = transform.position - player.position;
        }
    }

    private void Update() {
        if (!GameManager.instance.isGameover && player != null)
        {
            // 플레이어를 따라 부드럽게 이동
            Vector3 targetPosition = player.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }
}