using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// ButtonHoverEffect를 선택한 버튼에 쉽게 적용하기 위한 에디터 유틸리티
/// </summary>
public class ButtonHoverEffectSetup : Editor
{
    [MenuItem("GameObject/UI/Setup Button Hover Effect", false, 10)]
    static void SetupButtonHoverEffect()
    {
        GameObject selectedObj = Selection.activeGameObject;
        
        if (selectedObj == null)
        {
            Debug.LogError("버튼을 선택해주세요!");
            EditorUtility.DisplayDialog("오류", "버튼을 먼저 선택해주세요!", "확인");
            return;
        }

        // ButtonHoverEffect 컴포넌트 추가 (없으면)
        ButtonHoverEffect hoverEffect = selectedObj.GetComponent<ButtonHoverEffect>();
        if (hoverEffect == null)
        {
            hoverEffect = selectedObj.AddComponent<ButtonHoverEffect>();
            Debug.Log($"'{selectedObj.name}'에 ButtonHoverEffect 컴포넌트를 추가했습니다.");
        }

        // GrayFilter 생성
        Transform existingFilter = selectedObj.transform.Find("GrayFilter");
        if (existingFilter != null)
        {
            Debug.LogWarning($"'{selectedObj.name}'에 이미 GrayFilter가 존재합니다!");
            return;
        }

        // 새 GrayFilter GameObject 생성
        GameObject filterObj = new GameObject("GrayFilter");
        filterObj.transform.SetParent(selectedObj.transform, false);

        // Image 컴포넌트 추가
        Image filterImage = filterObj.AddComponent<Image>();
        filterImage.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        filterImage.raycastTarget = false;

        // RectTransform 설정 (버튼 전체 영역 덮기)
        RectTransform filterRect = filterObj.GetComponent<RectTransform>();
        filterRect.anchorMin = Vector2.zero;
        filterRect.anchorMax = Vector2.one;
        filterRect.sizeDelta = Vector2.zero;
        filterRect.anchoredPosition = Vector2.zero;

        Debug.Log($"'{selectedObj.name}'에 GrayFilter를 생성했습니다!");
        EditorUtility.DisplayDialog("완료", $"'{selectedObj.name}' 버튼에 Hover Effect가 설정되었습니다!", "확인");
    }

    [MenuItem("GameObject/UI/Setup All Canvas Buttons Hover Effect", false, 11)]
    static void SetupAllCanvasButtonsHoverEffect()
    {
        // 현재 씬의 모든 Canvas 찾기
        Canvas[] allCanvas = GameObject.FindObjectsOfType<Canvas>();
        
        if (allCanvas.Length == 0)
        {
            Debug.LogError("씬에 Canvas가 없습니다!");
            EditorUtility.DisplayDialog("오류", "씬에 Canvas가 없습니다!", "확인");
            return;
        }

        int buttonCount = 0;
        
        foreach (Canvas canvas in allCanvas)
        {
            // Canvas 아래의 모든 Button 찾기
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            
            foreach (Button button in buttons)
            {
                GameObject buttonObj = button.gameObject;
                
                // ButtonHoverEffect 컴포넌트 추가 (없으면)
                ButtonHoverEffect hoverEffect = buttonObj.GetComponent<ButtonHoverEffect>();
                if (hoverEffect == null)
                {
                    hoverEffect = buttonObj.AddComponent<ButtonHoverEffect>();
                }

                // GrayFilter가 이미 있는지 확인
                Transform existingFilter = buttonObj.transform.Find("GrayFilter");
                if (existingFilter != null)
                {
                    Debug.Log($"'{buttonObj.name}'에 이미 GrayFilter가 존재합니다. 스킵.");
                    continue;
                }

                // 새 GrayFilter GameObject 생성
                GameObject filterObj = new GameObject("GrayFilter");
                filterObj.transform.SetParent(buttonObj.transform, false);

                // Image 컴포넌트 추가
                Image filterImage = filterObj.AddComponent<Image>();
                filterImage.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
                filterImage.raycastTarget = false;

                // RectTransform 설정
                RectTransform filterRect = filterObj.GetComponent<RectTransform>();
                filterRect.anchorMin = Vector2.zero;
                filterRect.anchorMax = Vector2.one;
                filterRect.sizeDelta = Vector2.zero;
                filterRect.anchoredPosition = Vector2.zero;

                buttonCount++;
                Debug.Log($"'{buttonObj.name}'에 Hover Effect 설정 완료!");
            }
        }

        if (buttonCount > 0)
        {
            EditorUtility.DisplayDialog("완료", $"총 {buttonCount}개의 버튼에 Hover Effect가 설정되었습니다!", "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("알림", "Hover Effect를 추가할 버튼이 없습니다.", "확인");
        }
    }

    [MenuItem("GameObject/UI/Setup Button Hover Effect", true)]
    [MenuItem("GameObject/UI/Setup All Canvas Buttons Hover Effect", true)]
    static bool ValidateSetupMenu()
    {
        return true;
    }
}
#endif
