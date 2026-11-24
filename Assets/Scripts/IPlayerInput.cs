
public interface IPlayerInput
{
    float Horizontal { get; }

    bool JumpPressedThisFrame { get; }

    bool JumpHeld { get; }

    bool SprintHeld { get; }

    void SetEnabled(bool enabled);
}