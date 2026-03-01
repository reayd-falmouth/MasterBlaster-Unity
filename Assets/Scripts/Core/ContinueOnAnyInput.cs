#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    public class ContinueOnAnyInput : MonoBehaviour
    {
        [SerializeField]
        float armDelay = 0.25f; // small grace period after load
        float armAt;
        bool fired;

        void OnEnable()
        {
            fired = false;
            armAt = Time.unscaledTime + armDelay;
        }

        void Update()
        {
            if (fired || Time.unscaledTime < armAt)
                return;

            if (PressedThisFrame())
            {
                if (SceneFlowManager.I == null || !SceneFlowManager.ShouldAdvanceOnAnyInput(SceneFlowManager.I.CurrentState))
                    return;
                fired = true;
                SceneFlowManager.I.SignalScreenDone();
            }
        }

        static bool PressedThisFrame()
        {
            var k = Keyboard.current;
            if (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame))
                return true;

            var m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame)
                return true;

            // any connected gamepad
            foreach (var g in Gamepad.all)
                if (g.buttonSouth.wasPressedThisFrame) // A / Cross
                    return true;

            return false;
        }
    }
}
#else
// Legacy Input fallback
using Core;
using UnityEngine;

public class ContinueOnAnyInput : MonoBehaviour
{
    [SerializeField]
    float armDelay = 0.25f;
    float armAt;
    bool fired;

    void OnEnable()
    {
        fired = false;
        armAt = Time.unscaledTime + armDelay;
    }

    void Update()
    {
        if (fired || Time.unscaledTime < armAt)
            return;

        if (
            Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetMouseButtonDown(0)
            || Input.GetButtonDown("Submit")
            || Input.GetButtonDown("Fire1")
        )
        {
            if (SceneFlowManager.I == null || !SceneFlowManager.ShouldAdvanceOnAnyInput(SceneFlowManager.I.CurrentState))
                return;
            fired = true;
            SceneFlowManager.I.SignalScreenDone();
        }
    }
}
#endif
