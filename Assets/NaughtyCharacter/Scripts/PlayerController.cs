using UnityEngine;

namespace NaughtyCharacter
{
    [System.Serializable]
    public class MovementSettings
    {
        public float Acceleration = 25.0f;
        public float Decceleration = 25.0f;
        public float MaxHorizontalSpeed = 8.0f;
        public float JumpSpeed = 10.0f;
        public float JumpAbortSpeed = 10.0f;
    }

    [System.Serializable]
    public class GravitySettings
    {
        public float Gravity = 20f;
        public float GroundedGravity = 7f; // A constant grabity that is applied when the player is grounded
        public float MaxFallSpeed = 40f;
    }

    [System.Serializable]
    public class RotationSettings
    {
        [Header("Character Rotation")]
        public float RotationSpeed = 15.0f;
        public bool OrientRotationToMovement = true;

        [Header("Control Rotation")]
        public float ControlRotationSensitivity = 3.0f;
        public float MinPitchAngle = -75.0f;
        public float MaxPitchAngle = 5.0f;
    }

    public enum ControllerState
    {
        Idle,
        Running,
        Airborne
    }

    public class PlayerController : MonoBehaviour
    {
        public PlayerCamera PlayerCamera;
        public MovementSettings MovementSettings;
        public GravitySettings GravitySettings;
        public RotationSettings RotationSettings;

        private CharacterController _characterController;

        private float _targetHorizontalSpeed; // In meters/second
        private float _horizontalSpeed; // In meters/second
        private float _verticalSpeed; // In meters/second
        private bool _isGrounded;

        public ControllerState State { get; private set; }
        public ControllerState PrevState { get; private set; }
        public Vector3 Velocity => _characterController.velocity;
        public Vector3 HorizontalVelocity => _characterController.velocity.SetY(0.0f);
        public Vector3 VerticalVelocity => _characterController.velocity.Multiply(0.0f, 1.0f, 0.0f);
        public Vector2 ControlRotation { get; private set; } // X (Pitch), Y (Yaw)

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            UpdateControlRotation();
            PlayerCamera.SetControlRotation(ControlRotation);
        }

        private void FixedUpdate()
        {
            UpdateState();
            UpdateHorizontalSpeed();
            UpdateVerticalSpeed();

            Vector3 movement = _horizontalSpeed * GetMovementDirection() + _verticalSpeed * Vector3.up;
            _characterController.Move(movement * Time.deltaTime);

            OrientRotationToMovement(movement.SetY(0.0f));
            PlayerCamera.SetPosition(transform.position);

            _isGrounded = _characterController.isGrounded;
        }

        private void UpdateState()
        {
            PrevState = State;

            if (_isGrounded)
            {
                if (Velocity.sqrMagnitude > 0.0f)
                {
                    State = ControllerState.Running;
                }
                else
                {
                    State = ControllerState.Idle;
                }
            }
            else
            {
                State = ControllerState.Airborne;
            }
        }

        private void UpdateHorizontalSpeed()
        {
            Vector2 moveInput = PlayerInput.MoveInput;
            if (moveInput.sqrMagnitude > 1.0f)
            {
                moveInput.Normalize();
            }

            _targetHorizontalSpeed = moveInput.magnitude * MovementSettings.MaxHorizontalSpeed;
            float acceleration = PlayerInput.HasMoveInput ? MovementSettings.Acceleration : MovementSettings.Decceleration;

            _horizontalSpeed = Mathf.MoveTowards(_horizontalSpeed, _targetHorizontalSpeed, acceleration * Time.deltaTime);
        }

        private void UpdateVerticalSpeed()
        {
            if (_isGrounded)
            {
                _verticalSpeed = -GravitySettings.GroundedGravity;

                if (PlayerInput.JumpInput)
                {
                    _verticalSpeed = MovementSettings.JumpSpeed;
                    _isGrounded = false;
                }
            }
            else
            {
                if (!PlayerInput.JumpInput && _verticalSpeed > 0.0f)
                {
                    // This is what causes holding jump to jump higher that tapping jump.
                    _verticalSpeed = Mathf.MoveTowards(_verticalSpeed, -GravitySettings.MaxFallSpeed, MovementSettings.JumpAbortSpeed * Time.deltaTime);
                }

                _verticalSpeed = Mathf.MoveTowards(_verticalSpeed, -GravitySettings.MaxFallSpeed, GravitySettings.Gravity * Time.deltaTime);
            }
        }

        private bool OrientRotationToMovement(Vector3 movement)
        {
            if (RotationSettings.OrientRotationToMovement && movement.sqrMagnitude > 0.0f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
                if (RotationSettings.RotationSpeed > 0.0f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSettings.RotationSpeed * Time.deltaTime);
                }
                else
                {
                    transform.rotation = targetRotation;
                }

                return true;
            }

            return false;
        }

        private void UpdateControlRotation()
        {
            Vector2 camInput = PlayerInput.CameraInput;

            // Adjust the yaw angle (Y Rotation)
            float yawAngle = ControlRotation.y;
            yawAngle += camInput.x * RotationSettings.ControlRotationSensitivity;
            yawAngle %= 360.0f;

            // Adjust the pitch angle (X Rotation)
            float pitchAngle = ControlRotation.x;
            pitchAngle -= camInput.y * RotationSettings.ControlRotationSensitivity;
            pitchAngle %= 360.0f;
            pitchAngle = Util.ClampAngle(pitchAngle, RotationSettings.MinPitchAngle, RotationSettings.MaxPitchAngle);

            ControlRotation = new Vector2(pitchAngle, yawAngle);
        }

        private Vector3 GetMovementDirection()
        {
            if (!PlayerInput.HasMoveInput)
            {
                if (HorizontalVelocity.sqrMagnitude > 0.0f)
                {
                    return HorizontalVelocity.normalized;
                }
                else
                {
                    return Vector3.zero;
                }
            }

            // Calculate the move direction relative to camera yaw rotation
            Vector3 cameraForward = PlayerCamera.Camera.transform.forward.SetY(0.0f).normalized;
            Vector3 cameraRight = PlayerCamera.Camera.transform.right.SetY(0.0f).normalized;

            Vector3 moveDir = (cameraForward * PlayerInput.MoveInput.y + cameraRight * PlayerInput.MoveInput.x);

            if (moveDir.sqrMagnitude > 1f)
            {
                moveDir.Normalize();
            }

            return moveDir;
        }
    }
}