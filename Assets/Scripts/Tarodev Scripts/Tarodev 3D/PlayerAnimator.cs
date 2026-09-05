using UnityEngine;

namespace Tarodev3D
{
    /// <summary>
    /// 3D port of Tarodev's PlayerAnimator. Where the 2D version flipped and tilted a sprite,
    /// this turns the character to face its movement direction. Everything else (particles,
    /// audio, idle speed, jump/land feedback) is a straight port and stays fully optional --
    /// leave any of the fields empty and that piece of feedback is simply skipped, which is
    /// handy on this bare-bones test scene.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _anim;
        [SerializeField] private Transform _visual;

        [Header("Settings")]
        [SerializeField] private float _rotationSpeed = 720f;
        [SerializeField, Range(1f, 3f)] private float _maxIdleSpeed = 2;
        [SerializeField] private float _squashAmount = 0.15f;
        [SerializeField] private float _squashRecoverSpeed = 8f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem _jumpParticles;
        [SerializeField] private ParticleSystem _moveParticles;
        [SerializeField] private ParticleSystem _landParticles;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] _footsteps;

        private AudioSource _source;
        private IPlayerController _player;
        private bool _grounded;
        private Vector3 _visualScale = Vector3.one;
        private float _squash;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _player = GetComponent<IPlayerController>();
            if (_visual != null) _visualScale = _visual.localScale;
        }

        private void OnEnable()
        {
            _player.Jumped += OnJumped;
            _player.GroundedChanged += OnGroundedChanged;

            if (_moveParticles) _moveParticles.Play();
        }

        private void OnDisable()
        {
            _player.Jumped -= OnJumped;
            _player.GroundedChanged -= OnGroundedChanged;

            if (_moveParticles) _moveParticles.Stop();
        }

        private void Update()
        {
            if (_player == null) return;

            HandleFacing();
            HandleIdleSpeed();
            HandleSquash();
        }

        private void HandleFacing()
        {
            if (_player.MoveDirection == Vector3.zero) return;
            var targetRotation = Quaternion.LookRotation(_player.MoveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        private void HandleIdleSpeed()
        {
            var inputStrength = _player.FrameInput.magnitude;
            if (_anim) _anim.SetFloat(IdleSpeedKey, Mathf.Lerp(1, _maxIdleSpeed, inputStrength));
            if (_moveParticles) _moveParticles.transform.localScale = Vector3.MoveTowards(_moveParticles.transform.localScale, Vector3.one * inputStrength, 2 * Time.deltaTime);
        }

        private void HandleSquash()
        {
            if (_visual == null) return;
            _squash = Mathf.MoveTowards(_squash, 0, _squashRecoverSpeed * Time.deltaTime);
            var s = Mathf.Sin(_squash * Mathf.PI) * _squashAmount;
            _visual.localScale = new Vector3(_visualScale.x * (1 + s), _visualScale.y * (1 - s), _visualScale.z * (1 + s));
        }

        private void OnJumped()
        {
            if (_anim)
            {
                _anim.SetTrigger(JumpKey);
                _anim.ResetTrigger(GroundedKey);
            }

            if (_grounded) // Avoid coyote
            {
                _squash = 1f;
                if (_jumpParticles) _jumpParticles.Play();
            }
        }

        private void OnGroundedChanged(bool grounded, float impact)
        {
            _grounded = grounded;

            if (grounded)
            {
                if (_anim) _anim.SetTrigger(GroundedKey);
                if (_footsteps != null && _footsteps.Length > 0 && _source) _source.PlayOneShot(_footsteps[Random.Range(0, _footsteps.Length)]);
                if (_moveParticles) _moveParticles.Play();

                _squash = Mathf.InverseLerp(0, 20, impact);
                if (_landParticles)
                {
                    _landParticles.transform.localScale = Vector3.one * _squash;
                    _landParticles.Play();
                }
            }
            else
            {
                if (_moveParticles) _moveParticles.Stop();
            }
        }

        private static readonly int GroundedKey = Animator.StringToHash("Grounded");
        private static readonly int IdleSpeedKey = Animator.StringToHash("IdleSpeed");
        private static readonly int JumpKey = Animator.StringToHash("Jump");
    }
}
