using UnityEngine;

// 플레이어가 이동할 때 배경을 무한 루프시키는 스크립트
public class BackgroundLoop : MonoBehaviour {
    private float width; // 배경의 가로 길이
    private Transform player; // 플레이어 참조

    private void Awake() {
        BoxCollider2D backgroundCollider = GetComponent<BoxCollider2D>();
        width = backgroundCollider.size.x;
        
        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void Update() {
        if (player != null)
        {
            // 플레이어가 배경의 오른쪽 끝을 넘어가면 왼쪽으로 이동
            if (player.position.x > transform.position.x + width)
            {
                Reposition();
            }
            // 플레이어가 배경의 왼쪽 끝을 넘어가면 오른쪽으로 이동
            else if (player.position.x < transform.position.x - width)
            {
                RepositionLeft();
            }
        }
    }

    // 오른쪽으로 이동하는 메서드
    private void Reposition() {
        Vector2 offset = new Vector2(width * 2f, 0);
        transform.position = (Vector2)transform.position + offset;
    }
    
    // 왼쪽으로 이동하는 메서드
    private void RepositionLeft() {
        Vector2 offset = new Vector2(-width * 2f, 0);
        transform.position = (Vector2)transform.position + offset;
    }
}