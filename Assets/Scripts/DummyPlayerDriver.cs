using System.Collections;
using UnityEngine;

/// <summary>
/// 스크립트에 정의된 경로 혹은 액션 시퀀스를 따라 Player 프리팹을 움직이는 더미 컨트롤러.
/// PlayerController 입력 대신 Rigidbody2D/Animator를 직접 구동하며, 충돌/중력 등 물리는 그대로 유지합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DummyPlayerDriver : MonoBehaviour
{
    public enum ActionType
    {
        MoveTo,
        WalkDirection,
        Wait,
        Jump,
        Dash,
        Punch,
        Scratch,
        CustomAnimation
    }

    [System.Serializable]
    public class ActionStep
    {
        public ActionType actionType = ActionType.MoveTo;
        [Tooltip("MoveTo 시 목표 좌표, Dash/Jump 시 방향 벡터. 기본값은 Vector2.right")]
        public Vector2 targetOrDirection = Vector2.right;
        [Tooltip("Move/Walk 속도 또는 Dash 방향 정규화 전에 곱할 값")]
        public float speedOrPower = 3f;
        [Tooltip("Walk/Wait/Punch/Scratch/Dash/CustomAnimation 지속 시간")]
        public float duration = 0.5f;
        [Tooltip("액션 종료 후 다음 단계로 넘어가기 전 대기 시간")]
        public float waitAfter = 0.25f;
        [Tooltip("CustomAnimation 또는 Punch/Scratch에서 사용할 애니메이션 이름 Override")]
        public string animationNameOverride;
    }

    [System.Serializable]
    public class PathNode
    {
        [Tooltip("이동 목표 월드 좌표")]
        public Vector2 position;
        [Tooltip("해당 구간 이동 속도")]
        public float speed = 3f;
        [Tooltip("도달 후 다음 구간으로 넘어가기 전 대기 시간")]
        public float waitTime = 0.5f;
    }

    [Header("Path Settings")]
    [Tooltip("순서대로 이동할 경로 노드 목록")]
    public PathNode[] path = new PathNode[0];
    [Tooltip("마지막 노드 이후 다시 처음부터 반복할지 여부")]
    public bool loop = false;
    [Tooltip("노드 도달 판정 거리")]
    public float arrivalThreshold = 0.05f;

    [Header("Action Sequence (optional)")]
    [Tooltip("액션 기반 제어를 사용하고 싶다면 이 배열을 설정합니다. 비워두면 PathNode를 사용합니다.")]
    public ActionStep[] actionSequence;
    public bool loopActionSequence = false;
    [Tooltip("점프 시 기본 임펄스 세기")]
    public float defaultJumpImpulse = 6f;
    [Tooltip("대시 시 기본 임펄스 세기")]
    public float defaultDashImpulse = 6f;

    [Header("Player Components")]
    [Tooltip("시작 시 PlayerController를 비활성화하여 입력을 차단합니다.")]
    public bool disablePlayerController = false;
    [Tooltip("이동 방향에 따라 SpriteRenderer flipX를 자동 적용합니다.")]
    public bool faceMovementDirection = true;
    [SerializeField] private string jumpTriggerName = "Jump";
    [SerializeField] private string dashTriggerName = "Dash";
    [SerializeField] private string punchAnimationName = "Punch";
    [SerializeField] private string scratchAnimationName = "Scratch";

    [Tooltip("PlayerInputShim이 있을 경우 입력 흉내 모드로 실행합니다.")]
    [SerializeField] private bool useInputShimForMovement = true;
    [SerializeField] private PlayerInputShim inputShim;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private int currentIndex = 0;
    private bool waiting = false;
    private bool pathEnded = false;
    private static readonly int WalkingHash = Animator.StringToHash("IsWalking");
    private int currentActionIndex = 0;
    private bool actionStarted = false;
    private bool actionWaiting = false;
    private float actionTimer = 0f;
    private Coroutine waitRoutine;
    private bool UsingShim => useInputShimForMovement && inputShim != null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (inputShim == null)
        {
            inputShim = GetComponent<PlayerInputShim>();
        }

        if (disablePlayerController)
        {
            var controller = GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (actionSequence != null && actionSequence.Length > 0)
        {
            RunActionSequence();
            return;
        }

        bool shimActive = UsingShim;

        if (pathEnded || waiting || path == null || path.Length == 0)
        {
            StopMovementInstant();
            return;
        }

        PathNode node = path[currentIndex];
        Vector2 target = node.position;
        Vector2 delta = target - rb.position;

        if (delta.magnitude <= arrivalThreshold)
        {
            StartCoroutine(WaitAndAdvance(node.waitTime));
            StopMovementInstant();
            return;
        }

        Vector2 desiredVelocity = delta.normalized * Mathf.Max(0f, node.speed);
        ApplyDesiredVelocity(desiredVelocity);
    }

    private void RunActionSequence()
    {
        bool shimActive = UsingShim;

        if (actionSequence == null || actionSequence.Length == 0)
        {
            StopMovementInstant();
            return;
        }

        if (actionWaiting)
        {
            StopMovementInstant();
            return;
        }

        if (currentActionIndex >= actionSequence.Length)
        {
            if (loopActionSequence && actionSequence.Length > 0)
            {
                currentActionIndex = 0;
            }
            else
            {
                StopMovementInstant();
                return;
            }
        }

        var step = actionSequence[currentActionIndex];

        if (!actionStarted)
        {
            actionStarted = true;
            actionTimer = 0f;
            InitializeAction(step);
        }

        actionTimer += Time.fixedDeltaTime;
        bool completed = UpdateAction(step);
        if (completed)
        {
            FinishAction(step);
        }
    }

    private void InitializeAction(ActionStep step)
    {
        switch (step.actionType)
        {
            case ActionType.MoveTo:
            case ActionType.WalkDirection:
            case ActionType.Wait:
                break;
            case ActionType.Jump:
                ApplyJump(step);
                break;
            case ActionType.Dash:
                ApplyDash(step);
                break;
            case ActionType.Punch:
                if (inputShim != null)
                {
                    inputShim.TriggerPunch();
                }
                else
                {
                    PlayAnimationClip(step.animationNameOverride, punchAnimationName);
                }
                break;
            case ActionType.Scratch:
                if (inputShim != null)
                {
                    inputShim.TriggerScratch();
                }
                else
                {
                    PlayAnimationClip(step.animationNameOverride, scratchAnimationName);
                }
                break;
            case ActionType.CustomAnimation:
                PlayAnimationClip(step.animationNameOverride, null);
                break;
        }
    }

    private bool UpdateAction(ActionStep step)
    {
        switch (step.actionType)
        {
            case ActionType.MoveTo:
                return UpdateMoveTo(step);
            case ActionType.WalkDirection:
                return UpdateWalk(step);
            case ActionType.Wait:
                StopMovementInstant();
                return actionTimer >= Mathf.Max(0f, step.duration);
            case ActionType.Jump:
            case ActionType.Dash:
            case ActionType.Punch:
            case ActionType.Scratch:
            case ActionType.CustomAnimation:
                return actionTimer >= Mathf.Max(0.01f, step.duration);
            default:
                return true;
        }
    }

    private void FinishAction(ActionStep step)
    {
        actionStarted = false;
        if (inputShim != null)
        {
            if (step.actionType == ActionType.Jump)
            {
                inputShim.ReleaseJump();
            }
            if (step.actionType == ActionType.MoveTo || step.actionType == ActionType.WalkDirection)
            {
                inputShim.StopMovement();
            }
        }
        currentActionIndex++;
        if (step.waitAfter > 0f)
        {
            if (waitRoutine != null) StopCoroutine(waitRoutine);
            waitRoutine = StartCoroutine(ActionWait(step.waitAfter));
        }
    }

    private IEnumerator ActionWait(float seconds)
    {
        actionWaiting = true;
        yield return new WaitForSeconds(seconds);
        actionWaiting = false;
    }

    private bool UpdateMoveTo(ActionStep step)
    {
        Vector2 target = step.targetOrDirection;
        Vector2 delta = target - rb.position;
        if (delta.magnitude <= arrivalThreshold)
        {
            StopMovementInstant();
            return true;
        }

        Vector2 desiredVelocity = delta.normalized * Mathf.Max(0.1f, step.speedOrPower);
        ApplyDesiredVelocity(desiredVelocity);
        return false;
    }

    private bool UpdateWalk(ActionStep step)
    {
        Vector2 dir = step.targetOrDirection.normalized;
        if (dir == Vector2.zero) dir = Vector2.right;
        Vector2 vel = dir * step.speedOrPower;
        if (UsingShim)
        {
            inputShim.SetHorizontal(Mathf.Clamp(vel.x, -1f, 1f));
        }
        else
        {
            rb.linearVelocity = new Vector2(vel.x, rb.linearVelocity.y);
            ApplyAnimation(new Vector2(vel.x, 0f));
        }
        return actionTimer >= Mathf.Max(0f, step.duration);
    }

    private void ApplyJump(ActionStep step)
    {
        if (inputShim != null)
        {
            inputShim.PressJump();
        }
        else
        {
            Vector2 dir = step.targetOrDirection == Vector2.zero ? Vector2.up : step.targetOrDirection.normalized;
            float impulse = step.speedOrPower > 0f ? step.speedOrPower : defaultJumpImpulse;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(dir * impulse, ForceMode2D.Impulse);
            TriggerAnimator(jumpTriggerName);
        }
    }

    private void ApplyDash(ActionStep step)
    {
        if (inputShim != null)
        {
            inputShim.TriggerDash();
        }
        else
        {
            Vector2 dir = step.targetOrDirection == Vector2.zero ? Vector2.right : step.targetOrDirection.normalized;
            float impulse = step.speedOrPower > 0f ? step.speedOrPower : defaultDashImpulse;
            rb.AddForce(dir * impulse, ForceMode2D.Impulse);
            TriggerAnimator(dashTriggerName);
        }
    }

    private void TriggerAnimator(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;
        animator.SetTrigger(triggerName);
    }

    private IEnumerator WaitAndAdvance(float waitTime)
    {
        waiting = true;
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        currentIndex++;
        if (currentIndex >= path.Length)
        {
            if (loop && path.Length > 0)
            {
                currentIndex = 0;
            }
            else
            {
                pathEnded = true;
                rb.linearVelocity = Vector2.zero;
            }
        }
        waiting = false;
    }

    private void ApplyAnimation(Vector2 velocity)
    {
        if (faceMovementDirection && spriteRenderer != null)
        {
            if (velocity.x > 0.01f) spriteRenderer.flipX = false;
            else if (velocity.x < -0.01f) spriteRenderer.flipX = true;
        }

        if (animator != null && animator.HasParameterOfType(WalkingHash, AnimatorControllerParameterType.Bool))
        {
            bool walking = velocity.magnitude > 0.05f;
            animator.SetBool(WalkingHash, walking);
        }
    }

    private void ApplyDesiredVelocity(Vector2 desired)
    {
        if (UsingShim)
        {
            float horizontal = Mathf.Clamp(desired.x, -1f, 1f);
            inputShim.SetHorizontal(horizontal);
        }
        else
        {
            rb.linearVelocity = desired;
            ApplyAnimation(desired);
        }
    }

    private void StopMovementInstant()
    {
        if (UsingShim)
        {
            inputShim.StopMovement();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            ApplyAnimation(Vector2.zero);
        }
    }

    private void PlayAnimationClip(string overrideName, string fallbackName)
    {
        if (animator == null) return;
        string clipName = !string.IsNullOrEmpty(overrideName) ? overrideName : fallbackName;
        if (!string.IsNullOrEmpty(clipName))
        {
            animator.Play(clipName, 0, 0f);
        }
    }
}

internal static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator animator, int hash, AnimatorControllerParameterType type)
    {
        foreach (var param in animator.parameters)
        {
            if (param.nameHash == hash && param.type == type)
                return true;
        }
        return false;
    }
}
