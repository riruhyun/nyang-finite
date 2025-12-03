using UnityEngine;

/// <summary>
/// PlayerController 입력을 외부 스크립트가 흉내낼 수 있게 해주는 Shim.
/// DummyPlayerDriver 등에서 이 컴포넌트에 값을 세팅하면 PlayerController가
/// 동일한 입력으로 인식합니다.
/// </summary>
[DefaultExecutionOrder(-200)]
public class PlayerInputShim : MonoBehaviour, IPlayerInputSource
{
    [Range(-1f, 1f)]
    [SerializeField] private float horizontalAxis = 0f;
    private bool jumpHeld;
    private bool jumpDownFlag;
    private bool jumpUpFlag;
    private bool scratchFlag;
    private bool punchFlag;
    private bool dashFlag;

    public void SetHorizontal(float value)
    {
        horizontalAxis = Mathf.Clamp(value, -1f, 1f);
    }

    public void StopMovement()
    {
        horizontalAxis = 0f;
    }

    public void PressJump()
    {
        if (!jumpHeld)
        {
            jumpHeld = true;
            jumpDownFlag = true;
        }
    }

    public void ReleaseJump()
    {
        if (jumpHeld)
        {
            jumpHeld = false;
            jumpUpFlag = true;
        }
    }

    public void TriggerScratch()
    {
        scratchFlag = true;
    }

    public void TriggerPunch()
    {
        punchFlag = true;
    }

    public void TriggerDash()
    {
        dashFlag = true;
    }

    public bool GetMoveLeft()
    {
        return horizontalAxis < -0.01f;
    }

    public bool GetMoveRight()
    {
        return horizontalAxis > 0.01f;
    }

    public bool GetJumpDown()
    {
        bool val = jumpDownFlag;
        jumpDownFlag = false;
        return val;
    }

    public bool GetJumpHeld()
    {
        return jumpHeld;
    }

    public bool GetJumpUp()
    {
        bool val = jumpUpFlag;
        jumpUpFlag = false;
        return val;
    }

    public bool GetScratchDown()
    {
        bool val = scratchFlag;
        scratchFlag = false;
        return val;
    }

    public bool GetPunchDown()
    {
        bool val = punchFlag;
        punchFlag = false;
        return val;
    }

    public bool GetDashDown()
    {
        bool val = dashFlag;
        dashFlag = false;
        return val;
    }
}
