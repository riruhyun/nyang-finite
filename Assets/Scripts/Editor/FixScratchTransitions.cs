using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class FixScratchTransitions : EditorWindow
{
    [MenuItem("Tools/Fix Scratch Animation Transitions")]
    public static void FixTransitions()
    {
        // Animator Controller 로드
        string controllerPath = "Assets/Animations/Cat/Player.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        
        if (controller == null)
        {
            Debug.LogError($"Animator Controller를 찾을 수 없습니다: {controllerPath}");
            return;
        }

        Debug.Log($"[FIX] Animator Controller 발견: {controller.name}");

        // 모든 레이어 순회
        foreach (var layer in controller.layers)
        {
            Debug.Log($"[FIX] 레이어 체크: {layer.name}");
            
            // Scratch 상태 찾기
            AnimatorState scratchState = null;
            foreach (var state in layer.stateMachine.states)
            {
                if (state.state.name == "Scratch")
                {
                    scratchState = state.state;
                    Debug.Log($"[FIX] Scratch 상태 발견!");
                    break;
                }
            }

            if (scratchState == null)
            {
                Debug.LogWarning($"[FIX] {layer.name} 레이어에서 Scratch 상태를 찾을 수 없습니다.");
                continue;
            }

            // Scratch 상태의 모든 Transition 제거
            int transitionCount = scratchState.transitions.Length;
            Debug.Log($"[FIX] Scratch 상태에서 {transitionCount}개의 Transition 발견");

            // 역순으로 제거 (인덱스 꼬임 방지)
            for (int i = transitionCount - 1; i >= 0; i--)
            {
                var transition = scratchState.transitions[i];
                Debug.Log($"[FIX] Transition 제거: Scratch -> {transition.destinationState?.name ?? "Any State"}");
                scratchState.RemoveTransition(transition);
            }

            // Scratch 애니메이션 클립 설정 확인
            var motion = scratchState.motion;
            if (motion != null)
            {
                Debug.Log($"[FIX] Scratch Motion: {motion.name}");
                
                // AnimationClip이면 Loop 설정 확인
                if (motion is AnimationClip clip)
                {
                    Debug.Log($"[FIX] Scratch Clip Loop 설정: {clip.isLooping}");
                    if (clip.isLooping)
                    {
                        Debug.LogWarning("[FIX] ⚠️ Scratch 애니메이션이 Loop로 설정되어 있습니다! 이것이 문제의 원인일 수 있습니다.");
                        Debug.LogWarning("[FIX] Scratch.anim 파일을 선택하고 Inspector에서 'Loop Time' 체크박스를 해제하세요!");
                    }
                }
            }

            Debug.Log($"[FIX] ✅ Scratch 상태 수정 완료!");
        }

        // 변경사항 저장
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        
        Debug.Log("[FIX] 🎉 모든 수정 완료! Animator Controller가 저장되었습니다.");
    }
}
