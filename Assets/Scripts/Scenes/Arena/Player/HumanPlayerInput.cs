using UnityEngine;
using UnityEngine.InputSystem;

namespace Scenes.Arena.Player
{
    /// <summary>
    /// Human input via the new Input System. This component is added at runtime by GameManager
    /// when a player has a controller assigned (see GameManager.AttachInputProvider).
    /// The Input Action Asset is set from GameManager's "Player Input Actions" field (assign
    /// PlayerControls in the Game scene). Move and PlaceBomb are required; Detonate is optional.
    /// </summary>
    public class HumanPlayerInput : MonoBehaviour, IPlayerInput
    {
        [Header("Input System")]
        [Tooltip("Assign PlayerControls or UIMenus Input Action Asset in Inspector (or set from GameManager).")]
        public InputActionAsset inputActions
        {
            get => _inputActions;
            set { _inputActions = value; BindActions(); }
        }

        [SerializeField]
        private InputActionAsset _inputActions;

        [Header("Device")]
        [Tooltip("Kept for compatibility with GameManager; input comes from the action asset.")]
        public int deviceIndex;

        private InputAction _moveAction;
        private InputAction _bombAction;
        private InputAction _detonateAction;
        private bool _bombHeldLastFrame;

        public void Init(int deviceIndex, KeyCode up, KeyCode down, KeyCode left, KeyCode right, KeyCode bomb, KeyCode detonate)
        {
            this.deviceIndex = deviceIndex;
        }

        private void Awake()
        {
            BindActions();
        }

        private void BindActions()
        {
            if (_inputActions == null)
                return;
            var map = _inputActions.FindActionMap("Player");
            if (map == null)
                return;
            _moveAction = map.FindAction("Move");
            _bombAction = map.FindAction("PlaceBomb");
            _detonateAction = map.FindAction("Detonate");
            if (isActiveAndEnabled)
            {
                _moveAction?.Enable();
                _bombAction?.Enable();
                _detonateAction?.Enable();
            }
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _bombAction?.Enable();
            _detonateAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _bombAction?.Disable();
            _detonateAction?.Disable();
        }

        private void LateUpdate()
        {
            _bombHeldLastFrame = GetBombHeldInternal();
        }

        public Vector2 GetMoveDirection()
        {
            if (_moveAction == null)
                return Vector2.zero;
            return _moveAction.ReadValue<Vector2>();
        }

        public bool GetBombDown()
        {
            bool held = GetBombHeldInternal();
            return held && !_bombHeldLastFrame;
        }

        private bool GetBombHeldInternal()
        {
            if (_bombAction == null)
                return false;
            return _bombAction.IsPressed();
        }

        /// <summary>True while the detonate/bomb button is held (do not detonate). When false (button released), time/remote bombs may detonate.</summary>
        public bool GetDetonateHeld()
        {
            if (_detonateAction != null)
                return _detonateAction.IsPressed();
            return _bombAction != null && _bombAction.IsPressed();
        }
    }
}
