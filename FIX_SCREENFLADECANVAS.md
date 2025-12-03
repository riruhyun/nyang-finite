# ScreenFadeCanvas 수정 가이드

## 현재 상태
ScreenFadeCanvas가 이미 씬에 존재하고 거의 완성되어 있습니다!

## 필요한 수정 (1개만!)

**ScreenFadeController의 fadeCanvasGroup 필드가 비어있습니다.**

### Unity 에디터에서 수정 (권장)

1. Unity에서 **Stage1.unity** 씬 열기

2. **Hierarchy**에서 `ScreenFadeCanvas` 클릭

3. **Inspector**에서 `Screen Fade Controller` 컴포넌트 찾기

4. **Fade Canvas Group** 필드 확인 (현재 None으로 비어있음)

5. **연결하기:**
   - Hierarchy에서 `ScreenFadeCanvas` 확장
   - 자식 오브젝트인 `FadeImage` 선택
   - Inspector에서 `Canvas Group` 컴포넌트를 찾음
   - Canvas Group 컴포넌트 이름을 클릭한 상태로 드래그
   - ScreenFadeCanvas의 Inspector로 돌아가서
   - Screen Fade Controller의 `Fade Canvas Group` 필드에 드롭

6. **Ctrl+S**로 저장

### 다른 스테이지에도 적용하려면

같은 ScreenFadeCanvas가 DontDestroyOnLoad로 설정되어 있어서
**한 번만 설정하면 모든 스테이지에서 작동합니다!**

Stage1에서만 설정하면 됩니다.

## 테스트

1. Play 버튼 클릭
2. 플레이어가 적에게 죽을 때까지 대기
3. 예상 동작:
   - 3초 대기
   - 화면이 검게 페이드 아웃 (1초)
   - 씬 재시작
   - 화면이 밝아지는 페이드 인 (1초)

## 문제 해결

만약 작동하지 않으면:
- Console 창 확인 (에러 메시지)
- ScreenFadeCanvas가 Active 상태인지 확인
- FadeImage의 CanvasGroup Alpha가 0~1 사이인지 확인
