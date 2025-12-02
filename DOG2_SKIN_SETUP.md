# Dog2 Skin System 설정 가이드

## 개요

이 시스템은 Enemy 소환 시 `skinId`를 지정하여 다른 애니메이션을 사용할 수 있게 합니다.

- **skinId = 1**: 기본 애니메이션 사용 (오버라이드 없음)
- **skinId = 2**: dog2 스프라이트를 사용한 애니메이션으로 오버라이드

## 필수 작업: Dog2 애니메이션 클립 생성

### Unity 에디터에서 수동으로 생성하는 방법

1. **Resources 폴더 구조 생성**
   - `Assets/Resources/Animations/Dog2` 폴더를 생성합니다

2. **각 애니메이션 클립 생성** (Attack, Death, Idle, Jump, Walk)

   각 애니메이션에 대해:

   a. Project 창에서 `Assets/Resources/Animations/Dog2` 폴더를 선택

   b. 우클릭 > Create > Animation을 선택하고 이름을 지정합니다:
      - `Attack.anim`
      - `Death.anim`
      - `Idle.anim`
      - `Jump.anim`
      - `Walk.anim`

   c. 생성된 애니메이션 클립을 더블클릭하여 Animation 창을 엽니다

   d. Sprites/Dog 폴더에서 해당하는 dog2 스프라이트를 찾습니다:
      - `dog2_Attack.png` (Attack 애니메이션용)
      - `dog2_Death.png` (Death 애니메이션용)
      - `dog2_Idle.png` (Idle 애니메이션용)
      - `dog2_Jump.png` (Jump 애니메이션용)
      - `dog2_Walk.png` (Walk 애니메이션용)

   e. 각 스프라이트 시트의 화살표를 클릭하여 개별 프레임을 확인합니다

   f. 모든 프레임을 선택하여 Animation 창의 타임라인으로 드래그합니다

   g. 프레임 레이트를 원본 애니메이션과 동일하게 설정합니다 (보통 12fps)

3. **설정 확인**
   - 각 애니메이션이 올바르게 재생되는지 Animation 창에서 미리보기
   - 루프 설정이 필요한 애니메이션(Idle, Walk)은 Loop Time을 체크

### 원본 애니메이션 설정 복사 (추천)

더 정확한 결과를 위해 원본 애니메이션의 설정을 복사하는 것이 좋습니다:

1. `Assets/Animations/Dog/` 폴더의 원본 애니메이션을 선택
2. Inspector에서 다음을 확인:
   - Frame Rate
   - Loop Time (Animation Clip Settings)
   - Animation Events (있다면)
3. 새로 만든 dog2 애니메이션에 동일한 설정을 적용

## 사용 방법

### EnemySpawner에서 사용

`EnemySpawner` 컴포넌트의 `Spawn Definitions` 배열에서:

```
Spawn Definition 0:
  - Enemy Kind: Dog
  - Skin Id: 1  (기본 스킨)
  - Move Speed: 2
  - Max Health: 100
  ...

Spawn Definition 1:
  - Enemy Kind: Dog
  - Skin Id: 2  (dog2 스킨 - 오버라이드 적용)
  - Move Speed: 2
  - Max Health: 100
  ...
```

### 코드에서 직접 사용

```csharp
// Skin ID를 지정하여 적 소환
EnemySpawnHelper.SpawnDefinition definition = new EnemySpawnHelper.SpawnDefinition
{
    enemyKind = EnemySpawnHelper.EnemyKind.Dog,
    skinId = 2,  // dog2 스킨 사용
    moveSpeed = 2f,
    maxHealth = 100f,
    attackDamage = 1f,
    attackSpeed = 1f
};
```

## 시스템 작동 원리

1. **EnemySpawnHelper**: `SpawnDefinition`의 `skinId` 값을 확인
2. **EnemySkinManager**: `skinId > 1`이면 `ApplySkin()` 호출
3. **AnimatorOverrideController**: 런타임에 생성되어 원본 애니메이션을 dog2 애니메이션으로 교체
4. **Resources.Load**: `Resources/Animations/Dog2/` 폴더에서 애니메이션 클립 로드

## 트러블슈팅

### "No dog2 animation clips found" 경고가 나타나는 경우

- `Assets/Resources/Animations/Dog2/` 경로가 정확한지 확인
- 애니메이션 파일 이름이 정확한지 확인 (Attack, Death, Idle, Jump, Walk)
- Resources 폴더 이름의 대소문자 확인 (반드시 "Resources"여야 함)

### 애니메이션이 재생되지 않는 경우

- Unity 콘솔에서 에러 메시지 확인
- dog2 스프라이트 시트가 Multiple 모드로 설정되었는지 확인
- 애니메이션 클립에 프레임이 제대로 추가되었는지 확인

### 스프라이트가 표시되지 않는 경우

- SpriteRenderer 컴포넌트가 Enemy 프리팹에 있는지 확인
- dog2 스프라이트의 Import Settings가 올바른지 확인

## 향후 확장

더 많은 스킨을 추가하려면:

1. `EnemySkinManager.cs`에 새로운 스킨 처리 로직 추가
2. `Resources/Animations/Dog3/` 등의 폴더 생성
3. `skinId = 3` 등으로 사용

Cat이나 Pigeon 등 다른 적 타입에도 동일한 패턴으로 적용 가능합니다.
