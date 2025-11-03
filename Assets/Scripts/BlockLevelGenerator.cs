using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 블록 기반 레벨 생성 시스템
/// 포맷: "타입,X,Y,충돌여부|타입,X,Y,충돌여부|..."
/// 예시: "1,0,0,1|2,1,0,1|5,2,0,0"
/// </summary>
public class BlockLevelGenerator : MonoBehaviour
{
    [Header("Block Sprites")]
    [Tooltip("Tile_01부터 Tile_81까지의 스프라이트 배열")]
    public Sprite[] blockSprites;
    
    [Header("Level Data")]
    [Tooltip("레벨 데이터 문자열: 타입,X,Y,충돌|타입,X,Y,충돌|...")]
    [TextArea(5, 20)]
    public string levelData = "";
    
    [Header("Block Settings")]
    [Tooltip("블록 하나의 크기 (Unity 유닛)")]
    public float blockSize = 1f;
    
    [Tooltip("블록들의 Sorting Layer")]
    public string sortingLayerName = "Floor";
    
    [Tooltip("블록들의 Sorting Order")]
    public int sortingOrder = 0;
    
    private List<GameObject> spawnedBlocks = new List<GameObject>();
    
    private void Start()
    {
        LoadBlockSprites();
        GenerateLevel();
    }
    
    /// <summary>
    /// Assets/Blocks 폴더에서 모든 Tile 스프라이트 로드
    /// </summary>
    private void LoadBlockSprites()
    {
        if (blockSprites != null && blockSprites.Length > 0)
        {
            Debug.Log($"BlockLevelGenerator: {blockSprites.Length}개의 블록 스프라이트가 수동으로 할당되었습니다.");
            return;
        }
        
        // Tile_01부터 Tile_81까지 자동 로드
        List<Sprite> loadedSprites = new List<Sprite>();
        for (int i = 1; i <= 81; i++)
        {
            string spriteName = $"Tile_{i:D2}";
            Sprite sprite = Resources.Load<Sprite>($"Blocks/{spriteName}");
            
            if (sprite != null)
            {
                loadedSprites.Add(sprite);
            }
            else
            {
                // Resources 폴더가 아니면 직접 경로로 시도
                // AssetDatabasetDatabase.LoadAssetAtPath<Sprite>($"Assets/Blocks/{spriteName}.png");
                if (sprite != null)
                {
                    loadedSprites.Add(sprite);
                }
            }
        }
        
        blockSprites = loadedSprites.ToArray();
        Debug.Log($"BlockLevelGenerator: {blockSprites.Length}개의 블록 스프라이트를 로드했습니다.");
    }
    
    /// <summary>
    /// 레벨 데이터 문자열을 파싱하여 블록 생성
    /// </summary>
    public void GenerateLevel()
    {
        // 기존 블록 제거
        ClearLevel();
        
        if (string.IsNullOrWhiteSpace(levelData))
        {
            Debug.LogWarning("BlockLevelGenerator: levelData가 비어있습니다!");
            return;
        }
        
        if (blockSprites == null || blockSprites.Length == 0)
        {
            Debug.LogError("BlockLevelGenerator: 블록 스프라이트가 로드되지 않았습니다!");
            return;
        }
        
        // 데이터 파싱
        string[] blocks = levelData.Split('|');
        
        foreach (string blockData in blocks)
        {
            if (string.IsNullOrWhiteSpace(blockData)) continue;
            
            string[] parts = blockData.Split(',');
            
            if (parts.Length < 4)
            {
                Debug.LogWarning($"BlockLevelGenerator: 잘못된 블록 데이터 - {blockData}");
                continue;
            }
            
            // 데이터 파싱
            if (!int.TryParse(parts[0].Trim(), out int type))
            {
                Debug.LogWarning($"BlockLevelGenerator: 타입 파싱 실패 - {parts[0]}");
                continue;
            }
            
            if (!float.TryParse(parts[1].Trim(), out float x))
            {
                Debug.LogWarning($"BlockLevelGenerator: X 좌표 파싱 실패 - {parts[1]}");
                continue;
            }
            
            if (!float.TryParse(parts[2].Trim(), out float y))
            {
                Debug.LogWarning($"BlockLevelGenerator: Y 좌표 파싱 실패 - {parts[2]}");
                continue;
            }
            
            if (!int.TryParse(parts[3].Trim(), out int hasCollision))
            {
                Debug.LogWarning($"BlockLevelGenerator: 충돌 플래그 파싱 실패 - {parts[3]}");
                continue;
            }
            
            // 블록 생성
            SpawnBlock(type, x, y, hasCollision == 1);
        }
        
        Debug.Log($"BlockLevelGenerator: {spawnedBlocks.Count}개의 블록을 생성했습니다.");
    }
    
    /// <summary>
    /// 개별 블록 생성
    /// </summary>
    private void SpawnBlock(int type, float x, float y, bool hasCollision)
    {
        // 타입 인덱스 검증 (1-based를 0-based로 변환)
        int spriteIndex = type - 1;
        
        if (spriteIndex < 0 || spriteIndex >= blockSprites.Length)
        {
            Debug.LogWarning($"BlockLevelGenerator: 타입 {type}에 해당하는 스프라이트가 없습니다!");
            return;
        }
        
        Sprite sprite = blockSprites[spriteIndex];
        
        if (sprite == null)
        {
            Debug.LogWarning($"BlockLevelGenerator: 타입 {type}의 스프라이트가 null입니다!");
            return;
        }
        
        // GameObject 생성
        GameObject block = new GameObject($"Block_{type}_{spawnedBlocks.Count}");
        block.transform.SetParent(transform);
        block.transform.localPosition = new Vector3(x * blockSize, y * blockSize, 0);
        block.transform.localScale = Vector3.one;
        
        // SpriteRenderer 추가
        SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;
        
        // 충돌체 추가 (필요한 경우)
        if (hasCollision)
        {
            PolygonCollider2D collider = block.AddComponent<PolygonCollider2D>();
            // PolygonCollider2D는 자동으로 스프라이트 형태를 따라감
        }
        
        spawnedBlocks.Add(block);
    }
    
    /// <summary>
    /// 모든 생성된 블록 제거
    /// </summary>
    public void ClearLevel()
    {
        foreach (GameObject block in spawnedBlocks)
        {
            if (block != null)
            {
                DestroyImmediate(block);
            }
        }
        
        spawnedBlocks.Clear();
    }
    
    // 에디터에서 레벨 재생성 버튼
    [ContextMenu("Generate Level")]
    private void RegenerateLevel()
    {
        GenerateLevel();
    }
    
    [ContextMenu("Clear Level")]
    private void ClearLevelContext()
    {
        ClearLevel();
    }
}
