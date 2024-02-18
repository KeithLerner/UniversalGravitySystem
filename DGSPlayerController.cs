using UnityEngine;
using System;
using SQDUtilities;

namespace DynamicGravitySystem
{
    [RequireComponent(typeof(DGSBody))]
    public class DGSPlayerController : MonoBehaviour
    {
        [Serializable] public class MovementSettings
        {
            public float maxSpeed;
            public float acceleration;
            public float deceleration;

            public MovementSettings(float maxSpeed, float acceleration, 
                float deceleration)
            {
                this.maxSpeed = maxSpeed;
                this.acceleration = acceleration;
                this.deceleration = deceleration;
            }
        }

        [Header("Character Model")]
        [SerializeField] private Transform m_TopOfHead;
        [SerializeField] private Transform m_BottomOfFeet;

        [Header("Aiming")]
        [SerializeField] private MouseLook m_MouseLook;

        [Header("Base Movement")]
        [Tooltip("How precise air control is. 0 is off or none.")]
        [SerializeField, Range(0, 1)] private float m_AirControl = 0.3f;
        [SerializeField] private DGSBody.DGSAccelerationMode m_GroundAccelerationMode
            = DGSBody.DGSAccelerationMode.Basic;
        [SerializeField] private DGSBody.DGSAccelerationMode m_AirAccelerationMode
            = DGSBody.DGSAccelerationMode.Source;
        

        [Header("Base Movement Modifiers")]
        // Speeds
        [SerializeField] private MovementSettings m_GroundSettings = 
            new MovementSettings(7, 14, 10);
        [SerializeField] private MovementSettings m_AirSettings = 
            new MovementSettings(7, 2, 2);
        [SerializeField] private MovementSettings m_StrafeSettings = 
            new MovementSettings(1, 50, 50);
        [SerializeField] private Crouches m_Crouches;
        // Jumping
        private JumpAbility m_Jumps;
        [Tooltip("Queue jumps while in mid air, assuring the jump will activate " +
            "as soon as possible.")]
        [SerializeField] private bool m_QueueJumpsInAir;
        [Tooltip("How high the player can jump. Measured in player height.")]
        [SerializeField] private float m_JumpStrength;
        [Tooltip("Number of jumps available to the player before needing to " +
            "land again. Zero to prevent the player from jumping.")]
        [SerializeField] private int m_MultiJump;

        // Tracking
        private Camera m_Camera;
        private DGSBody m_PhysicsBody;
        private Vector2 m_MovementInputs;

        // Getters and Setters
        private float AirStrafeAngle => 5 * (1 - m_AirControl);
        
        private void Awake()
        {
            // Camera set up
            m_Camera = GetComponentInChildren<Camera>();
            m_Camera.transform.localEulerAngles = Vector3.zero;
            m_MouseLook.Init(m_Camera.transform.localRotation.x);

            // DGSBody set up
            m_PhysicsBody = GetComponent<DGSBody>();
            m_PhysicsBody.GroundCheck.onGrounded += 
                () => m_Jumps.Deactivate();

            // Basic Movement Modifiers set up
            m_Jumps = new JumpAbility(.1f, m_MultiJump, m_JumpStrength, 
                m_QueueJumpsInAir, m_TopOfHead, m_BottomOfFeet);
            m_Crouches.Init(GetComponent<Animation>(), m_TopOfHead, 
                m_BottomOfFeet);

        }

        private void Update()
        {
            // Gather inputs
            #region Inputs

            // Movement Inputs
            m_MovementInputs = new Vector2(InputManager.instance.horizontalMoveAxis,
                InputManager.instance.verticalMoveAxis);

            // Clamp Movement Inputs
            if (m_MovementInputs.magnitude > 1) m_MovementInputs.Normalize();

            // Aiming Inputs
            m_MouseLook.UpdateEnabledState();

            // Movement Modifying Inputs
            GetJumpInput();
            QueueCrouch();

            #endregion Inputs

            // Rotate the character and camera.
            #region Look 
            // Get rotations
            m_MouseLook.LookRotation(Time.deltaTime,
                out float gdr, out float cldr);
            // global delta rotation
            // camera local delta rotation
            // Do rotations
            m_Camera.transform.Rotate(Vector3.right,
                Mathf.Lerp(m_Camera.transform.localEulerAngles.x, cldr,
                m_MouseLook.Smoothed), Space.Self);
            m_PhysicsBody.Rotate(Vector3.up, gdr, Space.Self);
            // Orient the character
            Vector3 forward = Vector3.Cross(m_PhysicsBody.DGSLocalUp, -m_Camera.transform.right);
            if (forward == Vector3.zero) forward = transform.forward;
            m_PhysicsBody.Orient(forward);

            #endregion Look

            // Set movement state and call for movement accordingly
            #region Basic Movement
            if (m_PhysicsBody.GroundCheck.IsGrounded)
            {
                // Ground Movement
                GroundMove(Time.deltaTime);
            }
            else
            {
                // Air Movement
                if (!CanStrafe())
                    AirMove(Time.deltaTime);
                else
                    AirControl(Time.deltaTime);
            }
            #endregion Basic Movement

            // Modify basic movement
            #region Movement Abilities
            // Jumping
            if (GetJumpInput() && m_Jumps.IsReady) DoJump();

            // Crouching
            if (m_Crouches.CrouchQueued)
            {
                m_Crouches.Crouch();
            }
            else
            {
                m_Crouches.Uncrouch(m_PhysicsBody.GroundCheck.IsGrounded);
            }
            #endregion Movement Abilities
        }

        // Handle ground movement.
        private void GroundMove(float physicsTimeInterval)
        {
            float wishSpeed = WishDirection().magnitude * 
                m_GroundSettings.maxSpeed;
            float accel = (Vector3.Dot(m_PhysicsBody.GlobalTargetVelocity, 
                WishDirection()) < 0) ? m_GroundSettings.deceleration : 
                m_GroundSettings.acceleration;

            // DGSCharacterAccelerate
            m_PhysicsBody.DGSCharacterAccelerate(WishDirection() * wishSpeed, 
                physicsTimeInterval,
                accel, m_GroundAccelerationMode);
        }

        // Handle air movement
        private void AirMove(float physicsTimeInterval)
        {
            // Cached values
            Vector3 flatVel = m_PhysicsBody.TrueFlatVel;

            // Congruent : Will always exist inclusively between -1 and 1
            float congruent = Vector3.Dot(flatVel.normalized, 
                WishDirection().normalized);

            // Wish Speed
            float wishSpeed = WishDirection().magnitude * m_AirSettings.maxSpeed;

            // acceleration value
            float accel = (congruent < 0) ? m_AirSettings.deceleration : 
                m_AirSettings.acceleration;

            // Strafing
            if (CanStrafe())
            {
                if (wishSpeed > m_StrafeSettings.maxSpeed)
                    wishSpeed = m_StrafeSettings.maxSpeed;
                accel = m_StrafeSettings.acceleration;
            }

            // DGSCharacterAccelerate
            m_PhysicsBody.DGSCharacterAccelerate(WishDirection() * wishSpeed, 
                physicsTimeInterval,
                    accel, m_AirAccelerationMode);
        }

        // Handle air control allowing the player to move side 
        // to side much faster rather than being 'sluggish' when it comes to
        // cornering.
        private void AirControl(float physicsTimeInterval)
        {
            // Cached values
            float speed = m_PhysicsBody.FlatSpeed;
            Vector3 flatVel = m_PhysicsBody.TrueFlatVel;

            // Congruent : Will always exist inclusively between -1 and 1
            float congruent = Vector3.Dot(flatVel.normalized, 
                WishDirection().normalized);

            // Wish Speed
            float wishSpeed = WishDirection().magnitude * Mathf.Min(speed, 
                m_AirSettings.maxSpeed);

            // acceleration value
            float accel = (congruent < 0) ? m_AirSettings.deceleration : 
                m_AirSettings.acceleration;

            // Modified Q3 movement value, similar to a secondary
            // acceleration value
            float k = 64 * m_AirControl * congruent * congruent * 
                physicsTimeInterval + 1;

            // Change directions
            Vector3 delta = Vector3.zero;
            if (m_AirControl == 1)
            {
                delta = (WishDirection() * wishSpeed - flatVel) * accel;
            }
            else if (congruent < -.975f)
            {
                delta = accel * k * (WishDirection() * wishSpeed - flatVel);
            }
            else if (congruent < 0)
            {
                // Angle : Will always exist inclusively between 0 and 180
                float angle = Vector3.Angle(flatVel.normalized, 
                    WishDirection().normalized);
                // Error : Will always exist inclusively between 1 and 5
                float error = 1 + (angle / 45f);

                delta = WishDirection().normalized;
                delta.x *= wishSpeed + WishDirection().x * k;
                delta.y *= wishSpeed + WishDirection().y * k;
                delta.z *= wishSpeed + WishDirection().z * k;
                delta.Normalize();
                delta *= error * WishDirection().magnitude * wishSpeed * k;
            }
            m_PhysicsBody.AddForce(delta * m_AirControl * physicsTimeInterval, 
                physicsTimeInterval, ForceMode.VelocityChange);
        }

        // Queues the next jump.
        private bool GetJumpInput()
        {
            return m_Jumps.AutoBunnyHop ? 
                InputManager.instance.jumpHeld || 
                InputManager.instance.jumpPressed : 
                InputManager.instance.jumpPressed;
        }

        // 
        private Vector3 WishDirection()
        {
            return m_MovementInputs.x * transform.right + m_MovementInputs.y *
            transform.forward;
        }

        // Performs Jump
        private void DoJump()
        {
            // Negate any down velocity
            m_PhysicsBody.NegateDownVelocity();

            // Add up velocity
            m_PhysicsBody.AddForce(m_Jumps.GetJumpVelocity(
                DGSManager.instance.GetDGSGravityMagnitude(m_PhysicsBody)) *
                DGSManager.instance.GetDGSUpDirection(m_PhysicsBody), 0, 
                ForceMode.VelocityChange);
            
            // Update jump ability
            m_Jumps.Use();
        }

        // Crouches
        private void QueueCrouch()
        {
            m_Crouches.CrouchQueued = InputManager.instance.movementModifierBetaHeld || 
                InputManager.instance.movementModifierBetaPressed;
        }

        /// <summary>
        /// Determines if the character is capable of an air strafe based on the following criteria:
        ///  - There is only one movement input
        ///  - There is mouse input
        ///  - The angle between the current velocity and the movement input is 90 degrees
        ///  +/- air strafe angle
        /// </summary>
        /// <returns>True only when inputs are perpendicular to the wish direction and 
        /// there is horizontal mouse input</returns>
        private bool CanStrafe()
        {
            if (WishDirection().magnitude == 0 || 
                (m_MovementInputs.x != 0 && m_MovementInputs.y != 0))
                return false;
            float angle = Vector3.Angle(m_PhysicsBody.FlatVel.normalized, WishDirection().normalized);
            float min = 90 - AirStrafeAngle * .5f;
            float max = 90 + AirStrafeAngle * .5f;
            return angle < max && angle > min;
        }
    }
}