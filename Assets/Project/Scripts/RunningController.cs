using UnityEngine;
using UnityEngine.InputSystem;

namespace Project
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class RunningController : MonoBehaviour
    {
        // input action string
        private const string ActionStringJump = "Jump";
        private const string ActionStringAttack = "Attack";
        private const string ActionStringMove = "Move";
        private const string ActionStringSprint = "Sprint";
        private const string ActionStringWalk = "Walk";
        [SerializeField] private Animator animator;
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private PlayerInput playerInput;

        public Camera followCamera;
        public bool grounded = true;
        public LayerMask groundLayers;
        private readonly int _animIDAttacking = Animator.StringToHash("Attacking");
        private readonly int _animIDAttackTriggered = Animator.StringToHash("AttackTriggered");
        private readonly int _animIDFreeFall = Animator.StringToHash("FreeFall");
        private readonly int _animIDGrounded = Animator.StringToHash("Grounded");
        private readonly int _animIDJumpTriggered = Animator.StringToHash("JumpTriggered");
        private readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

        // animation Id
        private readonly int _animIDSpeed = Animator.StringToHash("Speed");
        private readonly float _fallTimeout = 0.15f;
        private readonly float _gravity = -15.0f;

        private readonly float _groundedOffset = -0.1f;
        private readonly float _groundedRadius = 0.3f;
        private readonly float _jumpHeight = 1.4f;

        private readonly float _jumpTriggeredTimeout = 0.50f;
        private readonly float _moveSpeed = 24f;
        private readonly float _rotationSmoothTime = 0.12f;
        private readonly float _speedChangeRate = 10.0f;
        private readonly float _sprintSpeed = 100f;
        private readonly float _terminalVelocity = 53.0f;


        private readonly float _walkSpeed = 6.0f;
        private float _animationBlend;
        private float _attackTriggeredTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _forwardAngle;

        // timeout delta
        private float _jumpTriggeredTimeoutDelta;
        private float _rotationVelocity;

        // player
        private float _speed;
        private float _targetRotation;
        private float _verticalVelocity;
        private float attackTriggeredTimeout = 0.50f;

        private void Awake()
        {
        }

        private void Update()
        {
            GroundCheck();
            HandleJumpAndGravity();
            Move();
        }

        private void FixedUpdate()
        {
        }

        private void LateUpdate()
        {
        }

        private void OnValidate()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (rigidbody == null) rigidbody = GetComponent<Rigidbody>();
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            animator.applyRootMotion = false;
        }

        private void GroundCheck()
        {
            var spherePosition = transform.position + Vector3.up * _groundedOffset;
            grounded = Physics.CheckSphere(spherePosition, _groundedRadius, groundLayers,
                QueryTriggerInteraction.Ignore);
            // get the normal of the ground to orient player correctly
            RaycastHit hit;
            Vector3 groundNormal;
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down * 5f,
                    out hit, groundLayers))
                groundNormal = hit.normal;
            else
                groundNormal = Vector3.up;

            // calculate the angle of slope below player, considering direction of player
            var forward = transform.forward;
            var projection = Vector3.ProjectOnPlane(forward, groundNormal).normalized;
            _forwardAngle = Vector3.SignedAngle(forward, projection, transform.right);

            Debug.DrawRay(spherePosition + Vector3.up * _groundedOffset,
                Vector3.down * (_groundedOffset + _groundedRadius), Color.red, 0.0f, false);
            animator.SetBool(_animIDGrounded, grounded);
        }

        private void Move()
        {
            var targetSpeed = playerInput.actions[ActionStringWalk].IsInProgress() ? _walkSpeed : _moveSpeed;
            targetSpeed = playerInput.actions[ActionStringSprint].IsInProgress() ? _sprintSpeed : targetSpeed;
            var moveInput = playerInput.actions[ActionStringMove].ReadValue<Vector2>();

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            if (moveInput == Vector2.zero) targetSpeed = 0.0f;

            var currentHorizontalSpeed = new Vector3(rigidbody.velocity.x, 0.0f, rigidbody.velocity.z).magnitude;
            var speedOffset = 0.1f;
            var inputMagnitude = 1f; // in case that input was analog stick


            // handle speed change
            if (Mathf.Abs(currentHorizontalSpeed - targetSpeed) > speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * _speedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * _speedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            var inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            if (moveInput != Vector2.zero && grounded)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  followCamera.transform.eulerAngles.y;
                var rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            // move player, considering the ground normal
            var targetDirection = Quaternion.Euler(_forwardAngle, _targetRotation, 0.0f) * Vector3.forward;
            var targetDeltaPosition = targetDirection.normalized * (_speed * Time.deltaTime) +
                                      new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;
            // rigidbody.MovePosition(rigidbody.position + targetDeltaPosition);
            rigidbody.velocity = grounded
                ? targetDeltaPosition / Time.fixedDeltaTime
                : new Vector3(rigidbody.velocity.x * 0.95f, rigidbody.velocity.y, rigidbody.velocity.z * 0.95f);

            // update animator
            animator.SetFloat(_animIDSpeed, _animationBlend);
            animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }

        private void HandleJumpAndGravity()
        {
            if (grounded)
            {
                _fallTimeoutDelta = _fallTimeout; // reset
                animator.SetBool(_animIDJumpTriggered, false);
                animator.SetBool(_animIDFreeFall, false);

                if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;

                if (playerInput.actions[ActionStringJump].IsInProgress() && _jumpTriggeredTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                    animator.SetBool(_animIDJumpTriggered, true);
                }

                if (_jumpTriggeredTimeoutDelta >= 0.0f) _jumpTriggeredTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTriggeredTimeoutDelta = _jumpTriggeredTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                    _fallTimeoutDelta -= Time.deltaTime;
                else
                    animator.SetBool(_animIDFreeFall, true);

                // inputActions.Player.Jump.ReadValue<bool>();
            }

            // limit vertical speed
            if (_verticalVelocity < _terminalVelocity) _verticalVelocity += _gravity * Time.deltaTime;
        }
    }
}