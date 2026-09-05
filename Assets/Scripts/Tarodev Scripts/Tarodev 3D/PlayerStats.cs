using UnityEngine;

namespace Tarodev3D
{
    /// <summary>
    /// 3D port of Tarodev's ScriptableStats. Same tunables as the 2D controller -- the only
    /// conceptual change is that "horizontal" now means the whole XZ plane instead of just X.
    /// </summary>
    [CreateAssetMenu(menuName = "Tarodev 3D/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("LAYERS")] [Tooltip("Set this to the layer your player is on")]
        public LayerMask PlayerLayer;

        [Header("INPUT")] [Tooltip("Makes all input snap to an integer. Prevents gamepads from walking slowly. Recommended value is true to ensure gamepad/keyboard parity.")]
        public bool SnapInput = true;

        [Tooltip("Minimum forward/back input required before it's recognized. Avoids drifting with sticky controllers"), Range(0.01f, 0.99f)]
        public float VerticalDeadZoneThreshold = 0.3f;

        [Tooltip("Minimum left/right input required before it's recognized. Avoids drifting with sticky controllers"), Range(0.01f, 0.99f)]
        public float HorizontalDeadZoneThreshold = 0.1f;

        [Header("MOVEMENT")] [Tooltip("The top horizontal movement speed, in meters/second")]
        public float MaxSpeed = 6;

        [Tooltip("The player's capacity to gain horizontal speed")]
        public float Acceleration = 50;

        [Tooltip("The pace at which the player comes to a stop")]
        public float GroundDeceleration = 30;

        [Tooltip("Deceleration in air only after stopping input mid-air")]
        public float AirDeceleration = 15;

        [Tooltip("A constant downward force applied while grounded. Helps on slopes"), Range(0f, -10f)]
        public float GroundingForce = -2f;

        [Tooltip("The detection distance for grounding and roof detection"), Range(0f, 0.5f)]
        public float GrounderDistance = 0.05f;

        [Header("JUMP")] [Tooltip("The immediate velocity applied when jumping")]
        public float JumpPower = 9;

        [Tooltip("The maximum vertical movement speed")]
        public float MaxFallSpeed = 25;

        [Tooltip("The player's capacity to gain fall speed. a.k.a. In Air Gravity")]
        public float FallAcceleration = 45;

        [Tooltip("The gravity multiplier added when jump is released early")]
        public float JumpEndEarlyGravityModifier = 3;

        [Tooltip("The time before coyote jump becomes unusable. Coyote jump allows jump to execute even after leaving a ledge")]
        public float CoyoteTime = .15f;

        [Tooltip("The amount of time we buffer a jump. This allows jump input before actually hitting the ground")]
        public float JumpBuffer = .2f;
    }
}
