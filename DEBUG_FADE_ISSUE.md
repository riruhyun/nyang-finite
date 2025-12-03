# Fade-In/Out 문제 디버깅 가이드

## 현재 상황

플레이어가 죽고 씬이 재시작될 때:
- ✅ Fade-Out 작동 (화면이 검어짐)
- ❌ Fade-In 작동 안 함 (검은 화면 유지)

## 디버그 로그 추가 완료

ScreenFadeController.cs에 상세한 로그를 추가했습니다.

## 테스트 방법

1. Unity 에디터 열기
2. **Console 창 열기** (Window > General > Console)
3. Console 우측 상단의 **Clear** 버튼 클릭 (기존 로그 제거)
4. **Play** 버튼 클릭
5. 플레이어가 죽을 때까지 대기
6. **Console 로그 확인**

## 체크 포인트

### 1. 첫 씬 시작 시 (정상)
```
[ScreenFadeController] Awake: Instance 설정...
[ScreenFadeController] Start 호출, fadeOnSceneStart=True
[ScreenFadeController] FadeToAlpha 시작: 1 → 0  ← 밝아짐
[ScreenFadeController] FadeToAlpha 완료: alpha=0
```

### 2. 플레이어 사망 시 (정상)
```
[ScreenFadeController] RespawnWithFade 호출
[ScreenFadeController] RespawnSequence 시작: 3초 대기
[ScreenFadeController] Fade-Out 시작 (화면이 검어짐)
[ScreenFadeController] FadeToAlpha 시작: 0 → 1  ← 검어짐
[ScreenFadeController] Fade-Out 완료, 씬 로드: Stage1
```

### 3. 씬 재시작 후 (문제 발생 지점!)
다음 중 하나가 나타나야 합니다:

**정상:**
```
[ScreenFadeController] HandleSceneLoaded 호출: Stage1, pendingSceneFadeOut=True
[ScreenFadeController] 씬 재시작 후 Fade-In 시작
[ScreenFadeController] FadeToAlpha 시작: 1 → 0  ← 밝아짐
```

**문제 1: HandleSceneLoaded가 호출되지 않음**
```
(아무 로그도 없음)
```

**문제 2: pendingSceneFadeOut이 False**
```
[ScreenFadeController] HandleSceneLoaded 호출: Stage1, pendingSceneFadeOut=False
[ScreenFadeController] pendingSceneFadeOut=false, Fade-In 스킵
```

**문제 3: fadeCanvasGroup이 null**
```
[ScreenFadeController] HandleSceneLoaded 호출: Stage1, pendingSceneFadeOut=True
[ScreenFadeController] fadeCanvasGroup이 null입니다! Fade 불가능
```

## 로그 확인 후 조치

Console의 모든 로그를 복사해서 알려주시면:
1. 정확한 문제 원인 파악
2. 맞춤 해결책 제공

## 임시 해결책

만약 HandleSceneLoaded가 호출되지 않는다면:
- OnEnable/OnDisable에서 이벤트 등록이 실패했을 가능성
- SceneManager.sceneLoaded 이벤트가 제대로 연결되지 않았을 가능성

## 참고

- Console에서 `[ScreenFadeController]`로 필터링하면 관련 로그만 볼 수 있습니다
- 로그 색상:
  - 흰색: 일반 정보
  - 노란색: 경고 (Warning)
  - 빨간색: 에러 (Error)
