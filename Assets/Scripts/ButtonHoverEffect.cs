using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 버튼에 마우스 호버 시 회색 필터를 제거하고 왼쪽으로 이동하는 효과를 추가합니다.
/// 사용법:
/// 1. 버튼에 이 스크립트를 추가
/// 2. 버튼 자식으로 "GrayFilter" 이름의 Image 오브젝트 생성 (또는 Inspector에서 수동 할당)
/// 3. GrayFilter는 버튼 전체를 덮도록 설정하고, 반투명 회색으로 설정
/// </summary>
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Gray Filter Settings")]
    [SerializeField] private Image grayFilterImage; // 회색 필터 Image (자동으로 찾거나 수동 할당)
    [SerializeField] private Color grayColor = new Color(0.3f, 0.3f, 0.3f, 0.6f); // 회색 필터 색상 (어두운 회색, 60% 불투명)
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.25f; // 0.25초 애니메이션
    [SerializeField] private float moveDistance = -2f; // 왼쪽으로 2만큼 이동
    
    private RectTransform rectTransform;
    private Vector2 originalPosition; // 원래 위치
    private Coroutine currentAnimation;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // 원래 위치 저장
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }
        
        // 회색 필터가 설정되지 않았다면 자동으로 찾기
        if (grayFilterImage == null)
        {
            // 자식 오브젝트 중에서 "GrayFilter" 이름을 가진 Image 찾기
            Transform filterTransform = transform.Find("GrayFilter");
            if (filterTransform != null)
            {
                grayFilterImage = filterTransform.GetComponent<Image>();
            }
            else
            {
                Debug.LogWarning($"[ButtonHoverEffect] '{gameObject.name}' 버튼에 'GrayFilter' 자식 오브젝트가 없습니다. " +
                                "Inspector에서 수동으로 할당하거나 'Create Gray Filter' 컨텍스트 메뉴를 사용하세요.");
            }
        }
        
        // 초기 상태: 회색 필터 활성화
        if (grayFilterImage != null)
        {
            grayFilterImage.color = grayColor;
            grayFilterImage.raycastTarget = false; // 클릭 이벤트 방해 방지
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 기존 애니메이션이 실행 중이면 중지
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        // Hover 애니메이션 시작 (필터 제거 + 왼쪽 이동)
        currentAnimation = StartCoroutine(AnimateHover(true));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // 기존 애니메이션이 실행 중이면 중지
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        // 원래 상태로 돌아가는 애니메이션 시작 (필터 복원 + 제자리)
        currentAnimation = StartCoroutine(AnimateHover(false));
    }
    
    private IEnumerator AnimateHover(bool isHovering)
    {
        float elapsed = 0f;
        
        // 시작 값
        Vector2 startPos = rectTransform.anchoredPosition;
        Color startColor = grayFilterImage != null ? grayFilterImage.color : Color.clear;
        
        // 목표 값
        Vector2 targetPos = isHovering 
            ? originalPosition + new Vector2(moveDistance, 0) // 호버: 왼쪽으로 이동
            : originalPosition; // 해제: 제자리
        
        Color targetColor = isHovering 
            ? new Color(grayColor.r, grayColor.g, grayColor.b, 0f) // 호버: 필터 투명하게
            : grayColor; // 해제: 필터 다시 보이게
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // Ease-In-Out 함수 적용 (부드러운 가속/감속)
            float easedT = EaseInOutCubic(t);
            
            // 위치 보간
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, easedT);
            }
            
            // 색상 보간 (회색 필터의 알파값 조정)
            if (grayFilterImage != null)
            {
                grayFilterImage.color = Color.Lerp(startColor, targetColor, easedT);
            }
            
            yield return null;
        }
        
        // 최종 값 정확히 설정
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = targetPos;
        }
        
        if (grayFilterImage != null)
        {
            grayFilterImage.color = targetColor;
        }
        
        currentAnimation = null;
    }
    
    /// <summary>
    /// Ease-In-Out Cubic 함수 (부드러운 가속/감속)
    /// </summary>
    private float EaseInOutCubic(float t)
    {
        return t < 0.5f 
            ? 4f * t * t * t 
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
    
    /// <summary>
    /// Inspector의 컨텍스트 메뉴에서 회색 필터를 자동으로 생성합니다.
    /// 우클릭 > Create Gray Filter 선택
    /// </summary>
    [ContextMenu("Create Gray Filter")]
    private void CreateGrayFilter()
    {
        // 이미 존재하는지 확인
        Transform existingFilter = transform.Find("GrayFilter");
        if (existingFilter != null)
        {
            Debug.LogWarning($"[ButtonHoverEffect] '{gameObject.name}' 버튼에 이미 'GrayFilter'가 존재합니다!");
            return;
        }
        
        // 새 GameObject 생성
        GameObject filterObj = new GameObject("GrayFilter");
        filterObj.transform.SetParent(transform, false);
        
        // Image 컴포넌트 추가
        Image filterImage = filterObj.AddComponent<Image>();
        filterImage.color = grayColor;
        filterImage.raycastTarget = false; // 클릭 이벤트 방해 방지
        
        // RectTransform 설정 (버튼 전체 영역 덮기)
        RectTransform filterRect = filterObj.GetComponent<RectTransform>();
        filterRect.anchorMin = Vector2.zero;
        filterRect.anchorMax = Vector2.one;
        filterRect.sizeDelta = Vector2.zero;
        filterRect.anchoredPosition = Vector2.zero;
        
        // 참조 설정
        grayFilterImage = filterImage;
        
        Debug.Log($"[ButtonHoverEffect] '{gameObject.name}' 버튼에 'GrayFilter'가 생성되었습니다!");
    }
}
