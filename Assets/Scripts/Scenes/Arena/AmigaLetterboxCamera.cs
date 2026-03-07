using UnityEngine;

namespace Scenes.Arena
{
    /// <summary>
    /// Keeps the camera viewport at the design aspect ratio (e.g. 640:512), adding
    /// letterboxing so the game view matches the Amiga look on 16:9 and other ratios.
    /// Attach to the Main Camera in the Game scene.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class AmigaLetterboxCamera : MonoBehaviour
    {
        [Tooltip("Design aspect width (default from DesignResolution).")]
        [SerializeField]
        private int designWidth = DesignResolution.Width;

        [Tooltip("Design aspect height (default from DesignResolution).")]
        [SerializeField]
        private int designHeight = DesignResolution.Height;

        private Camera _camera;
        private float _designAspect;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _designAspect = designWidth / (float)designHeight;
        }

        private void Start()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            ApplyLetterbox();
        }

        private void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                ApplyLetterbox();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_camera != null && designWidth > 0 && designHeight > 0)
            {
                _designAspect = designWidth / (float)designHeight;
                ApplyLetterbox();
            }
        }
#endif

        /// <summary>
        /// Sets the camera's viewport rect so the rendered area has the design aspect ratio,
        /// centered with bars (letterbox or pillarbox) as needed.
        /// </summary>
        public void ApplyLetterbox()
        {
            if (_camera == null)
                return;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            float screenAspect = screenWidth / screenHeight;

            float w, h, x, y;
            if (screenAspect > _designAspect)
            {
                // Screen is wider: pillarbox (horizontal bars on sides)
                w = _designAspect / screenAspect;
                h = 1f;
                x = (1f - w) * 0.5f;
                y = 0f;
            }
            else
            {
                // Screen is taller: letterbox (vertical bars top/bottom)
                w = 1f;
                h = screenAspect / _designAspect;
                x = 0f;
                y = (1f - h) * 0.5f;
            }

            _camera.rect = new Rect(x, y, w, h);
        }
    }
}
