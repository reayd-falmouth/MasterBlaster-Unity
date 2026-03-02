using UnityEngine;

namespace Scenes.Arena.Player
{
    /// <summary>
    /// Human input: keyboard (device 0) or gamepad (device 1+). Uses legacy Input.
    /// </summary>
    public class HumanPlayerInput : MonoBehaviour, IPlayerInput
    {
        [Header("Device")]
        [Tooltip("0 = keyboard (use KeyCodes below), 1+ = joystick index")]
        public int deviceIndex;

        [Header("Keyboard (device 0 only)")]
        public KeyCode keyUp = KeyCode.W;
        public KeyCode keyDown = KeyCode.S;
        public KeyCode keyLeft = KeyCode.A;
        public KeyCode keyRight = KeyCode.D;
        public KeyCode keyBomb = KeyCode.LeftShift;
        public KeyCode keyDetonate = KeyCode.LeftShift;

        private bool _bombHeldLastFrame;

        public void Init(int deviceIndex, KeyCode up, KeyCode down, KeyCode left, KeyCode right, KeyCode bomb, KeyCode detonate)
        {
            this.deviceIndex = deviceIndex;
            keyUp = up;
            keyDown = down;
            keyLeft = left;
            keyRight = right;
            keyBomb = bomb;
            keyDetonate = detonate;
        }

        private void LateUpdate()
        {
            _bombHeldLastFrame = GetBombHeldInternal();
        }

        public Vector2 GetMoveDirection()
        {
            if (deviceIndex == 0)
                return GetKeyboardMove();
            return GetJoystickMove();
        }

        public bool GetBombDown()
        {
            bool held = GetBombHeldInternal();
            return held && !_bombHeldLastFrame;
        }

        private bool GetBombHeldInternal()
        {
            if (deviceIndex == 0)
                return Input.GetKey(keyBomb);
            return Input.GetKey("Joystick" + deviceIndex + "Button0");
        }

        public bool GetDetonateHeld()
        {
            if (deviceIndex == 0)
                return Input.GetKey(keyDetonate);
            return Input.GetKey("Joystick" + deviceIndex + "Button1");
        }

        private Vector2 GetKeyboardMove()
        {
            float x = 0f, y = 0f;
            if (Input.GetKey(keyUp)) y += 1f;
            if (Input.GetKey(keyDown)) y -= 1f;
            if (Input.GetKey(keyRight)) x += 1f;
            if (Input.GetKey(keyLeft)) x -= 1f;
            return new Vector2(x, y).normalized;
        }

        private Vector2 GetJoystickMove()
        {
            string prefix = "Joystick" + deviceIndex + "Axis";
            float x = Input.GetAxisRaw(prefix + "1"); // left stick X
            float y = Input.GetAxisRaw(prefix + "2"); // left stick Y
            if (Mathf.Abs(x) < 0.2f) x = 0f;
            if (Mathf.Abs(y) < 0.2f) y = 0f;
            return new Vector2(x, y).normalized;
        }
    }
}
