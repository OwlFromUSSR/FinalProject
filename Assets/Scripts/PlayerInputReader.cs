using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour, IPlayerInput
{
    [Header("Ввод")]
    [Tooltip("Мёртвая зона для аналогового стика геймпада.")]
    [SerializeField, Range(0f, 0.9f)] private float stickDeadzone = 0.2f;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private bool inputEnabled = true;

    public float Horizontal
    {
        get
        {
            if (!inputEnabled) return 0f;
            float value = moveAction.ReadValue<float>();
            return Mathf.Abs(value) < stickDeadzone ? 0f : Mathf.Clamp(value, -1f, 1f);
        }
    }

    public bool JumpPressedThisFrame => inputEnabled && jumpAction.WasPressedThisFrame();
    public bool JumpHeld => inputEnabled && jumpAction.IsPressed();
    public bool SprintHeld => inputEnabled && sprintAction.IsPressed();

    private void Awake()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick/x");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Keyboard>/w");
        jumpAction.AddBinding("<Keyboard>/upArrow");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/buttonWest");
    }

    private void OnEnable() => SetActionsActive(inputEnabled);

    private void OnDisable() => SetActionsActive(false);

    private void OnDestroy()
    {
        moveAction?.Dispose();
        jumpAction?.Dispose();
        sprintAction?.Dispose();
    }

    public void SetEnabled(bool enabled)
    {
        inputEnabled = enabled;
        SetActionsActive(enabled && isActiveAndEnabled);
    }

    private void SetActionsActive(bool active)
    {
        if (moveAction == null) return;

        if (active)
        {
            moveAction.Enable();
            jumpAction.Enable();
            sprintAction.Enable();
        }
        else
        {
            moveAction.Disable();
            jumpAction.Disable();
            sprintAction.Disable();
        }
    }
}