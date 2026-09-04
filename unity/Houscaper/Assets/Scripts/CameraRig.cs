using UnityEngine;
using UnityEngine.EventSystems;

namespace Houscaper
{
    /// <summary>
    /// Townscaper-style orbit camera. Dragging turns the world; a press that barely moves is
    /// forwarded to <see cref="BuildController"/> as a click, so one button both looks and builds.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Vector3 Pivot = new Vector3(0f, 1.2f, 0f);

        public float Distance = 17f;
        public float MinDistance = 5f;
        public float MaxDistance = 42f;

        public float Yaw = 35f;
        public float Pitch = 32f;
        public float MinPitch = 8f;
        public float MaxPitch = 78f;

        /// <summary>Pixels of travel before a press stops counting as a click.</summary>
        public float ClickSlop = 7f;
        public float ClickTime = 0.45f;

        public System.Action<int> Clicked;

        float _targetYaw;
        float _targetPitch;
        float _targetDistance;
        Vector3 _targetPivot;

        Vector2 _pressPosition;
        float _pressTime;
        int _pressButton = -1;
        bool _pressMoved;
        bool _pressOverUi;

        Camera _camera;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _targetYaw = Yaw;
            _targetPitch = Pitch;
            _targetDistance = Distance;
            _targetPivot = Pivot;
        }

        void Update()
        {
            HandleMouse();
            HandleZoom();
            HandleKeys();
            ApplyTransform();
        }

        void HandleMouse()
        {
            for (int button = 0; button <= 2; button++)
            {
                if (!Input.GetMouseButtonDown(button)) continue;
                if (_pressButton != -1) continue;

                _pressButton = button;
                _pressPosition = Input.mousePosition;
                _pressTime = Time.unscaledTime;
                _pressMoved = false;
                _pressOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }

            if (_pressButton == -1) return;

            var current = (Vector2)Input.mousePosition;
            var delta = current - _pressPosition;

            if (!_pressMoved && delta.magnitude > ClickSlop) _pressMoved = true;

            if (Input.GetMouseButton(_pressButton))
            {
                if (_pressMoved && !_pressOverUi)
                {
                    var frameDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

                    if (_pressButton == 2)
                    {
                        Pan(frameDelta);
                    }
                    else
                    {
                        _targetYaw += frameDelta.x * 4.2f;
                        _targetPitch = Mathf.Clamp(_targetPitch - frameDelta.y * 3.2f, MinPitch, MaxPitch);
                    }
                }

                return;
            }

            // Button released.
            bool wasClick = !_pressMoved
                && !_pressOverUi
                && Time.unscaledTime - _pressTime < ClickTime;

            if (wasClick) Clicked?.Invoke(_pressButton);
            _pressButton = -1;
        }

        void HandleZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _targetDistance = Mathf.Clamp(_targetDistance * Mathf.Pow(0.88f, scroll), MinDistance, MaxDistance);
            }
        }

        void HandleKeys()
        {
            float horizontal = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Mathf.Abs(horizontal) > 0f) _targetYaw += horizontal * 70f * Time.deltaTime;

            float vertical = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Mathf.Abs(vertical) > 0f)
            {
                _targetPitch = Mathf.Clamp(_targetPitch + vertical * 45f * Time.deltaTime, MinPitch, MaxPitch);
            }
        }

        void Pan(Vector2 frameDelta)
        {
            var right = Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;
            var forward = Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
            float scale = _targetDistance * 0.02f;

            _targetPivot -= (right * frameDelta.x + forward * frameDelta.y) * scale;
            _targetPivot.x = Mathf.Clamp(_targetPivot.x, -14f, 14f);
            _targetPivot.z = Mathf.Clamp(_targetPivot.z, -14f, 14f);
            _targetPivot.y = Mathf.Clamp(_targetPivot.y, 0f, 8f);
        }

        void ApplyTransform()
        {
            float smoothing = 1f - Mathf.Exp(-14f * Time.deltaTime);

            Yaw = Mathf.LerpAngle(Yaw, _targetYaw, smoothing);
            Pitch = Mathf.Lerp(Pitch, _targetPitch, smoothing);
            Distance = Mathf.Lerp(Distance, _targetDistance, smoothing);
            Pivot = Vector3.Lerp(Pivot, _targetPivot, smoothing);

            var rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            transform.SetPositionAndRotation(Pivot - rotation * Vector3.forward * Distance, rotation);
        }

        public Ray PointerRay()
        {
            return _camera.ScreenPointToRay(Input.mousePosition);
        }

        public void FrameOn(Vector3 point)
        {
            _targetPivot = new Vector3(
                Mathf.Clamp(point.x, -14f, 14f),
                Mathf.Clamp(point.y, 0f, 8f),
                Mathf.Clamp(point.z, -14f, 14f));
        }
    }
}
