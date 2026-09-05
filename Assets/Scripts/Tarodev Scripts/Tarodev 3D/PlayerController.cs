using System;
using UnityEngine;

namespace Tarodev3D
{
    /// <summary>
    /// 3D port of Tarodev's 2D PlayerController. Same loop -- gather input, check collisions,
    /// handle jump/gravity/direction, apply movement -- moved from Rigidbody2D/X-only motion
    /// onto a Rigidbody moving on the XZ plane. Horizontal input is made camera-relative so it
    /// reads naturally in third person.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private PlayerStats _stats;
        [SerializeField] private Transform _cameraTransform;

        private Rigidbody _rb;
        private CapsuleCollider _col;
        private FrameInput _frameInput;
        private Vector3 _frameVelocity;
        private Vector3 _moveDirection;

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public Vector3 MoveDirection => _moveDirection;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion

        private float _time;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();

            // We drive gravity and velocity ourselves, same as the 2D original.
            _rb.useGravity = false;
            _rb.freezeRotation = true;

            if (_cameraTransform == null && Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.Space),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate()
        {
            CheckCollisions();

            HandleJump();
            HandleDirection();
            HandleGravity();

            ApplyMovement();
        }

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private void CheckCollisions()
        {
            GetCapsulePoints(out var top, out var bottom, out var radius);

            // Ground and ceiling. Relies on _stats.PlayerLayer excluding the player's own layer
            // so the cast, which starts inside our own capsule, doesn't hit ourselves.
            bool groundHit = Physics.CapsuleCast(top, bottom, radius, Vector3.down, _stats.GrounderDistance, ~_stats.PlayerLayer, QueryTriggerInteraction.Ignore);
            bool ceilingHit = Physics.CapsuleCast(top, bottom, radius, Vector3.up, _stats.GrounderDistance, ~_stats.PlayerLayer, QueryTriggerInteraction.Ignore);

            // Hit a ceiling
            if (ceilingHit) _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

            // Landed on the ground
            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            // Left the ground
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }
        }

        private void GetCapsulePoints(out Vector3 top, out Vector3 bottom, out float radius)
        {
            var center = _col.bounds.center;
            radius = _col.radius;
            var halfHeight = Mathf.Max(_col.height * 0.5f - radius, 0);
            top = center + Vector3.up * halfHeight;
            bottom = center - Vector3.up * halfHeight;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _frameVelocity.y > 0) _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote) ExecuteJump();

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _stats.JumpPower;
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            var wishDir = CameraRelativeInput();
            var currentHorizontal = new Vector3(_frameVelocity.x, 0, _frameVelocity.z);

            if (wishDir == Vector3.zero)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                currentHorizontal = Vector3.MoveTowards(currentHorizontal, Vector3.zero, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                currentHorizontal = Vector3.MoveTowards(currentHorizontal, wishDir * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }

            _frameVelocity.x = currentHorizontal.x;
            _frameVelocity.z = currentHorizontal.z;
            _moveDirection = wishDir;
        }

        /// <summary>Turns the raw Move input into a flattened, camera-relative world direction.</summary>
        private Vector3 CameraRelativeInput()
        {
            if (_frameInput.Move == Vector2.zero) return Vector3.zero;

            var forward = Vector3.forward;
            var right = Vector3.right;

            if (_cameraTransform != null)
            {
                forward = _cameraTransform.forward;
                forward.y = 0;
                forward.Normalize();

                right = _cameraTransform.right;
                right.y = 0;
                right.Normalize();
            }

            var dir = forward * _frameInput.Move.y + right * _frameInput.Move.x;
            return dir.sqrMagnitude > 1 ? dir.normalized : dir;
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Please assign a PlayerStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public Vector2 FrameInput { get; }

        /// <summary>Flattened, camera-relative, normalized world-space move direction (zero when idle).</summary>
        public Vector3 MoveDirection { get; }
    }
}
