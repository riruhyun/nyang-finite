public interface IPlayerInputSource
{
    bool GetMoveLeft();
    bool GetMoveRight();
    bool GetJumpDown();
    bool GetJumpHeld();
    bool GetJumpUp();
    bool GetScratchDown();
    bool GetPunchDown();
    bool GetDashDown();
}
