using SunSwitch.Core;
using SunSwitch.LookAndFeel;
using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SunSwitch.Player
{
    /// <summary>
    /// Project copy of Starter Assets' ThirdPersonController, so package updates can't overwrite it.
    /// Changes: no sprint, coyote time, jump buffer, heavier fall, InputLock support,
    /// Teleport / ResetVelocity for respawn, AddExternalVelocity for jump pads.
    /// Needs Starter Assets (StarterAssetsInputs) imported.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class LightkeeperController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Run speed in m/s (single fixed speed, no sprint)")]
        public float MoveSpeed = 5.0f;
        [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        public float JumpHeight = 1.2f;
        [Tooltip("Negative. Engine default is -9.81")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Minimum seconds between two jumps")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Seconds of falling before the free-fall animation starts")]
        public float FallTimeout = 0.15f;

        [Header("Sun Switch feel")]
        [Tooltip("Seconds you can still jump after walking off a ledge")]
        public float CoyoteTime = 0.1f;
        [Tooltip("Seconds a jump press is remembered before landing")]
        public float JumpBufferTime = 0.12f;
        [Tooltip("Gravity multiplier while falling")]
        public float FallGravityMultiplier = 1.6f;
        [Tooltip("How fast sideways push from jump pads fades (m/s per second)")]
        public float ExternalDrag = 6f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        // camera
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private readonly float _terminalVelocity = 53.0f;
        private Vector3 _externalVelocity;
        private Vector3 _ownVelocity;   // from this script's own Move only (not PlatformRider's carry)

        // timers
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _coyoteTimer;
        private float _jumpBufferTimer;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;
        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _hasAnimator = TryGetComponent(out _animator);
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
            AssignAnimationIDs();
            _jumpTimeoutDelta = 0f;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            // Only clear jump; move/look are masked on read so a held key still works after unlock.
            if (InputLock.IsLocked) _input.jump = false;

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        // ---------- Public API ----------

        /// <summary>Moves the player instantly (respawn). CharacterController must be off for the move to stick.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _controller.enabled = true;
            _targetRotation = rotation.eulerAngles.y;
            ResetVelocity();
        }

        public void ResetVelocity()
        {
            _verticalVelocity = 0f;
            _speed = 0f;
            _externalVelocity = Vector3.zero;
            _ownVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
        }

        /// <summary>Jump pads: vertical part replaces current vertical speed, sideways part fades out over time.</summary>
        public void AddExternalVelocity(Vector3 velocity)
        {
            _verticalVelocity = velocity.y;
            _externalVelocity += new Vector3(velocity.x, 0f, velocity.z);
            _coyoteTimer = 0f;       // no extra jump on top of a launch
            _jumpBufferTimer = 0f;
            _jumpTimeoutDelta = JumpTimeout;   // still grounded for a frame or two; block a jump overriding the launch
            if (_hasAnimator) _animator.SetBool(_animIDJump, true);
        }

        // ---------- Internals ----------

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void CameraRotation()
        {
            Vector2 look = InputLock.IsLocked ? Vector2.zero : _input.look;
            if (look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                // Mouse sensitivity from the Settings screen.
                float deltaTimeMultiplier = (IsCurrentDeviceMouse ? 1.0f : Time.deltaTime) * GameSettings.MouseSensitivity;
                _cinemachineTargetYaw += look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += look.y * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            Vector2 move = InputLock.IsLocked ? Vector2.zero : _input.move;
            float targetSpeed = move == Vector2.zero ? 0.0f : MoveSpeed;

            // Leave out jump-pad push so it isn't turned into extra run speed. Uses the velocity of
            // this script's own last Move: CharacterController.velocity only reports the most recent
            // Move call, which on a moving platform is PlatformRider's carry, not the player's run.
            float currentHorizontalSpeed = new Vector3(_ownVelocity.x - _externalVelocity.x, 0.0f,
                _ownVelocity.z - _externalVelocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(move.x, 0.0f, move.y).normalized;
            if (move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _externalVelocity = Vector3.MoveTowards(_externalVelocity, Vector3.zero, ExternalDrag * Time.deltaTime);

            Vector3 before = transform.position;
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime +
                             _externalVelocity * Time.deltaTime);
            if (Time.deltaTime > 0f) _ownVelocity = (transform.position - before) / Time.deltaTime;

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            // Coyote time: stays topped up while grounded, counts down in the air
            if (Grounded && _verticalVelocity <= 0f) _coyoteTimer = CoyoteTime;   // not while rising: the ground check still touches for a frame after a jump
            else _coyoteTimer -= Time.deltaTime;

            // Jump buffer: remember a press for a short time
            if (_input.jump)
            {
                _jumpBufferTimer = JumpBufferTime;
                _input.jump = false;
            }
            else
            {
                _jumpBufferTimer -= Time.deltaTime;
            }

            if (_jumpTimeoutDelta > 0f) _jumpTimeoutDelta -= Time.deltaTime;

            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;   // stick to the ground
            }
            else
            {
                if (_fallTimeoutDelta >= 0f) _fallTimeoutDelta -= Time.deltaTime;
                else if (_hasAnimator) _animator.SetBool(_animIDFreeFall, true);
            }

            // Jump: buffered press while still inside coyote time
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f && _jumpTimeoutDelta <= 0f)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                _jumpTimeoutDelta = JumpTimeout;
                if (_hasAnimator) _animator.SetBool(_animIDJump, true);
                if (AudioManager.Instance != null) AudioManager.Instance.Play(SfxId.Jump);
            }

            // Gravity, heavier on the way down, capped at terminal velocity
            float g = Gravity * (_verticalVelocity < 0f ? FallGravityMultiplier : 1f);
            _verticalVelocity = Mathf.Max(_verticalVelocity + g * Time.deltaTime, -_terminalVelocity);
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
            Gizmos.color = Grounded ? transparentGreen : transparentRed;
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        // Animation events from the Starter Assets clips. Keep these or Unity logs "has no receiver" errors.
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips != null && FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center),
                    FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center),
                    FootstepAudioVolume);
            }
        }
    }
}
