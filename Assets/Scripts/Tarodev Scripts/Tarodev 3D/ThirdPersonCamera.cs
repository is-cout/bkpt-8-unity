using UnityEngine;

namespace Tarodev3D
{
    /// <summary>
    /// Minimal mouse-orbit third person camera. Not part of the ported Tarodev logic -- just
    /// enough to give PlayerController a camera-relative forward/right and to look reasonable
    /// while testing the movement in the course.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _targetOffset = new(0, 1.5f, 0);

        [Header("Orbit")]
        [SerializeField] private float _distance = 5f;
        [SerializeField] private float _minDistance = 0.5f;
        [SerializeField] private float _mouseSensitivity = 3f;
        [SerializeField] private float _minPitch = -30f;
        [SerializeField] private float _maxPitch = 70f;

        [Header("Collision")]
        [SerializeField] private LayerMask _collisionMask = ~0;
        [SerializeField] private float _collisionRadius = 0.2f;

        [Header("Cursor")]
        [SerializeField] private bool _lockCursor = true;

        private float _yaw;
        private float _pitch = 15f;

        private void Start()
        {
            if (_lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (_target != null) _yaw = _target.eulerAngles.y;
        }

        private void Update()
        {
            // Basic click-to-lock / escape-to-free cursor handling for testing in the editor.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (_lockCursor && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                _yaw += Input.GetAxis("Mouse X") * _mouseSensitivity;
                _pitch -= Input.GetAxis("Mouse Y") * _mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
            }

            var rotation = Quaternion.Euler(_pitch, _yaw, 0);
            var pivot = _target.position + _targetOffset;

            var distance = _distance;
            if (Physics.SphereCast(pivot, _collisionRadius, -(rotation * Vector3.forward), out var hit, _distance, _collisionMask, QueryTriggerInteraction.Ignore))
            {
                distance = Mathf.Max(_minDistance, hit.distance);
            }

            transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * distance, rotation);
        }
    }
}
