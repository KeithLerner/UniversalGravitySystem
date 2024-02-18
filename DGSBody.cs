using SQDUtilities;
using UnityEngine;

namespace DynamicGravitySystem
{
    [RequireComponent(typeof(Rigidbody))]
    public class DGSBody : MonoBehaviour
    {
        #region ENUMS
        /// <summary>
        /// Used to determine the ways in which the DGSBody can be interacted 
        /// with.
        /// </summary>
        public enum DGSBodyMotorMode 
        { 
            /// <summary>
            /// Static is for DGSBodies that don't move.
            /// </summary>
            Static,

            /// <summary>
            /// Proportional, Integral, Derivative is for vehicle like 
            /// behavior.
            /// </summary>
            PID,

            /// <summary>
            /// SimpleCharacter is for charactercontroller type behavior,
            /// </summary>
            SimpleCharacter,

            /// <summary>
            /// AdvancedCharacter is for rigidbody like behavior,
            /// </summary>
            AdvancedCharacter
        }
        
        /// <summary>
        /// Used to determine the way in which acceleration is applied to a 
        /// DGSBody.
        /// </summary>
        public enum DGSAccelerationMode 
        { 
            /// <summary>
            /// Changes the velocity by (MaxVelocityInWishDirection - 
            /// CurrentVelocity) * AccelerationFactor
            /// </summary>
            Basic, 
            
            /// <summary>
            /// Replicates the acceleration found in the Source Engine.
            /// </summary>
            Source, 
            
            /// <summary>
            /// Replicates the acceleration found in Quake 3.
            /// </summary>
            Quake 
        }
        #endregion ENUMS

        // Inputs
        public DGSBodyMotorMode motorMode = DGSBodyMotorMode.Static;
        public DGSMass mass;
        [SerializeField] private DGSMaterial m_Material;
        [SerializeField, Range(0, 1)] private float m_CoyoteTime = .1f;
        [SerializeField, Range(0, 1)] private float m_AutoOrientStrength = .2f;
        [SerializeField] private GroundCheck m_GroundCheck;
        [SerializeField] private RigidbodyConstraints m_InitialConstraints = 
            RigidbodyConstraints.FreezeAll;


        // Trackers
        private Vector3 m_GlobalTargetVelocity;
        private Vector3 m_StoredVelocity;
        private Rigidbody m_Rigidbody;

        // Getters and Setters
        public GroundCheck GroundCheck => m_GroundCheck;
        public float MaterialFriction => m_Material.friction;
        public float CoyoteTime => m_CoyoteTime;
        public Vector3 GlobalTargetVelocity => m_GlobalTargetVelocity;
        public Vector3 DGSLocalUp => 
            DGSManager.instance.GetDGSUpDirection(this);
        public Vector3 TrueLocalUp => transform.up;
        public Vector3 TrueVelocity => m_Rigidbody.velocity;
        public Vector3 Acceleration => 
            (m_Rigidbody.velocity - m_StoredVelocity) * Time.fixedDeltaTime;
        public Vector3 TrueFlatVel =>
            VectorMath.NegateDirection(TrueVelocity, DGSLocalUp);
        public Vector3 FlatVel =>
            VectorMath.NegateDirection(m_GlobalTargetVelocity, DGSLocalUp);
        public Vector3 TrueDownVel =>
            VectorMath.IsolateDirection(TrueVelocity, -DGSLocalUp);
        public Vector3 DownVel =>
            VectorMath.IsolateDirection(m_GlobalTargetVelocity, -DGSLocalUp);
        public float TrueFlatSpeed => TrueFlatVel.magnitude;
        public float FlatSpeed => FlatVel.magnitude;
        public float TrueDownSpeed => TrueDownVel.magnitude;
        public float DownSpeed => DownVel.magnitude;


        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.constraints = m_InitialConstraints;
            m_Rigidbody.useGravity = false;
        }

        private void Update()
        {
            switch (motorMode)
            {
                default: // Static
                    break;

                case DGSBodyMotorMode.PID:

                    break;

                case DGSBodyMotorMode.SimpleCharacter:

                    break;

                case DGSBodyMotorMode.AdvancedCharacter:

                    break;
            }

            // Debug Visual
            if (motorMode != DGSBodyMotorMode.Static) 
                m_GroundCheck.DrawGroundCheck();
        }

        private void LateUpdate()
        {
            // Update Rotation
            switch (motorMode)
            {
                default: // Static
                    {
                        // Calculate orientation rotation
                        Quaternion directTargetRotation = Quaternion.FromToRotation(TrueLocalUp, DGSLocalUp);
                        Quaternion inverseTargetRotation = directTargetRotation * Quaternion.Euler(180f, 0f, 0f);
                        Quaternion targetRotation = Vector3.Angle(TrueLocalUp, DGSLocalUp) <= 90 ? directTargetRotation : inverseTargetRotation;

                        Quaternion newRotation = Quaternion.Slerp(
                            transform.rotation, transform.rotation * targetRotation,
                            30 * m_AutoOrientStrength * Time.deltaTime);


                        // Apply orientation rotation
                        transform.rotation = newRotation;
                        break;
                    }

                case DGSBodyMotorMode.PID:
                    { }
                    break;

                case DGSBodyMotorMode.SimpleCharacter:
                    { }
                    break;

                case DGSBodyMotorMode.AdvancedCharacter:
                    {
                        // Calculate orientation rotation
                        Quaternion directTargetRotation = Quaternion.FromToRotation(TrueLocalUp, DGSLocalUp);
                        Quaternion inverseTargetRotation = directTargetRotation * Quaternion.Euler(180f, 0f, 0f);
                        Quaternion targetRotation = Vector3.Angle(TrueLocalUp, DGSLocalUp) <= 90 ? directTargetRotation : inverseTargetRotation;

                        Quaternion newRotation = Quaternion.Slerp(
                            transform.rotation, transform.rotation * targetRotation,
                            30 * m_AutoOrientStrength * Time.deltaTime);


                        // Apply orientation rotation
                        transform.rotation = newRotation;
                        break;
                    }
            }
        }

        private void FixedUpdate()
        {
            // Do Ground Check
            if (motorMode != DGSBodyMotorMode.Static) 
                m_GroundCheck.UpdateGrounded();

            // Do grounded functions
            if (m_GroundCheck.IsGrounded)
            {
                if (!m_GroundCheck.StoredGroundedValue)
                {
                    m_GroundCheck.DoOnGrounded();

                    // REPLACE WITH SOME FORM OF BOUNCINESS THAT IS AWARE OF
                    // SLOPES but only for advancedcharacter
                    NegateDownVelocity();
                }
                else
                {
                    m_GroundCheck.DoWhenGrounded();

                    // Apply half of friction:
                    // Provide a change in direction opposite the desired move
                    // direction
                    Friction(GetDGSFriction(), Time.fixedDeltaTime);
                }
            }
            else
            {
                // Gravity
                if (m_GroundCheck.TimeSinceLastGrounded > m_CoyoteTime)
                    Gravity(Time.fixedDeltaTime);
            }

            // Update Position
            switch (motorMode)
            {
                default: // Static
                    break;

                case DGSBodyMotorMode.PID:

                    break;

                case DGSBodyMotorMode.SimpleCharacter:
                    
                    break;

                case DGSBodyMotorMode.AdvancedCharacter:
                    // Update velocity
                    m_Rigidbody.velocity = m_GlobalTargetVelocity;
                    // Round velocity
                    if (m_GroundCheck.IsGrounded &&
                        TrueFlatSpeed <= .01f) 
                        // Condition is almost exclusively met while
                        // slowing down due to friction
                        IsolateDownVelocity();

                    break;
            }

            // Update private trackers
            m_StoredVelocity = m_Rigidbody.velocity;
        }

        /// <summary>
        /// Accelerates the global target velocity of the DGSBody, 
        /// queuing the actual acceleration for the next physics frame. 
        /// Ignores mass.
        /// </summary>
        /// <param name="wishVelocity"> The target/max speed and direction the 
        /// acceleration can reach. </param>
        /// <param name="acceleration"> The magnitude of the 
        /// acceleration. </param>
        /// <param name="physicsTimeInterval"> The physics time step interval. 
        /// If called from update use Time.deltaTime, 
        /// if called from fixedUpdate use Time.fixedDeltaTime. </param>
        /// <param name="accelerationMode"> The style of acceleration used. 
        /// Basic by default. </param>
        /// <returns> The new global target velocity. </returns>
        public Vector3 DGSCharacterAccelerate(Vector3 wishVelocity, 
            float physicsTimeInterval, float acceleration,
            DGSAccelerationMode accelerationMode = DGSAccelerationMode.Basic)
        {
            Vector3 deltaVelocity;
            Vector3 initialVelocity = TrueFlatVel;
            Vector3 wishDirection = wishVelocity.normalized;
            float wishSpeed = wishVelocity.magnitude;

            // Apply half of friction:
            // Makes it hard to change directions on surfaces with low friction
            if (m_GroundCheck.IsGrounded) acceleration *= GetDGSFriction();

            switch (accelerationMode)
            {
                default: // Basic
                    {
                        // Accelerated velocity in direction of movment
                        float accelSpeed = acceleration * physicsTimeInterval;

                        // Delta: (b-a) * t part of a standard lerp formula
                        deltaVelocity = (wishVelocity - initialVelocity) * 
                            accelSpeed;
                        break;
                    }

                case DGSAccelerationMode.Source: // Source
                    {
                        // Vector projection of initialVelocity onto
                        // accelerationDirection
                        float projVel = Vector3.Dot(initialVelocity, 
                            wishDirection);
                        // Accelerated speed in accelerationDirection
                        float delta = acceleration * physicsTimeInterval;

                        // If necessary, truncate the accelerated velocity so
                        // the vector projection does not exceed maxSpeed
                        if (projVel + delta > wishSpeed) delta = wishSpeed - 
                                projVel;

                        // Set deltaVelocity
                        deltaVelocity = wishDirection * delta;
                        break;
                    }

                case DGSAccelerationMode.Quake: // Quake
                    {
                        // Vector projection of initialVelocity onto
                        // accelerationDirection
                        float initialSpeed = Vector3.Dot(initialVelocity, 
                            wishDirection);
                        // Simplified accelerated speed in
                        // accelerationDirection
                        float addSpeed = wishSpeed - initialSpeed; // d = b - a

                        // If exceeding max speed, cancel acceleration
                        if (addSpeed <= 0)
                        {
                            deltaVelocity = Vector3.zero;
                            break;
                        }

                        // Accelerated speed in accelerationDirection
                        float accelerationSpeed = acceleration * wishSpeed * 
                            physicsTimeInterval;

                        // If necessary, truncate the change in speed so the
                        // vector projection does not exceed maxSpeed
                        if (accelerationSpeed > addSpeed) accelerationSpeed = 
                                addSpeed;

                        // Set deltaVelocity
                        deltaVelocity = accelerationSpeed * wishDirection;
                        break;
                    }
            }

            // Queue the acceleration
            m_GlobalTargetVelocity += deltaVelocity;

            // Return the new velocity
            return m_GlobalTargetVelocity;
        }

        /// <summary>
        /// Add force to the DGSBody.
        /// </summary>
        /// <param name="force"> The force to be added to the 
        /// DGSBody. </param>
        /// <param name="physicsTimeInterval"> The physics time step interval. 
        /// If called from update use Time.deltaTime, 
        /// if called from fixedUpdate use Time.fixedDeltaTime. </param>
        /// <param name="forceMode"> The way the force is applied to the 
        /// DGSBody. </param>
        public void AddForce(Vector3 force, float physicsTimeInterval, 
            ForceMode forceMode = ForceMode.Force)
        {
            switch(forceMode)
            {
                default:
                    //Interprets the input as force
                    m_GlobalTargetVelocity += physicsTimeInterval *
                        force / mass.Mass;
                    break;

                case ForceMode.Acceleration:
                    //Interprets the parameter as an acceleration
                    m_GlobalTargetVelocity += physicsTimeInterval *
                        force;
                    break;

                case ForceMode.Impulse:
                    //Interprets the parameter as momentum
                    m_GlobalTargetVelocity += force / mass.Mass;
                    break;

                case ForceMode.VelocityChange:
                    //Interprets the parameter as a direct velocity change
                    m_GlobalTargetVelocity += force;
                    break;
            }
            m_GlobalTargetVelocity += physicsTimeInterval * force / mass.Mass;
        }

        /// <summary>
        /// Add torque to the DGSBody.
        /// </summary>
        /// <param name="torque"> The force to be added to the 
        /// DGSBody. </param>
        /// <param name="physicsTimeInterval"> The physics time step interval. 
        /// If called from update use Time.deltaTime, 
        /// if called from fixedUpdate use Time.fixedDeltaTime. </param>
        /// <param name="torqueMode"> The way the force is applied to the 
        /// DGSBody. </param>

        public void AddTorque(Vector3 torque, float physicsTimeInterval, 
            ForceMode torqueMode = ForceMode.Force)
        {
            switch(torqueMode)
            {
                default:
                    //Interprets the input as torque
                    m_Rigidbody.angularVelocity += physicsTimeInterval *
                        torque / mass.Mass;
                    break;

                case ForceMode.Acceleration:
                    //Interprets the parameter as angular acceleration
                    m_Rigidbody.angularVelocity += physicsTimeInterval * 
                        torque;
                    break;

                case ForceMode.Impulse:
                    //Interprets the parameter as an angular momentum
                    m_Rigidbody.angularVelocity += torque / mass.Mass;
                    break;

                case ForceMode.VelocityChange:
                    //Interprets the parameter as a direct angular velocity
                    //change
                    m_Rigidbody.angularVelocity += torque;
                    break;
            }
        }

        /// <summary>
        /// Directly updates the rotation of the DGSBody.
        /// </summary>
        /// <param name="axis"> The axis the DGSBody will rotate around. </param>
        /// <param name="angle"> The number of degrees to rotate. </param>
        /// <param name="space"> The relative space of the axis. </param>
        public void Rotate(Vector3 axis, float angle, Space space = Space.World)
        {
            transform.Rotate(axis, angle, space);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forward"></param>
        public void Orient(Vector3 forward)
        {
            transform.rotation = Quaternion.LookRotation(forward, DGSLocalUp);
        }

        /// <summary>
        /// Applies gravity to the DGSBody using the DGSManager.
        /// </summary>
        /// <param name="physicsTimeInterval"> The physics time step interval. 
        /// If called from update use Time.deltaTime, 
        /// if called from fixedUpdate use Time.fixedDeltaTime. </param>
        private void Gravity(float physicsTimeInterval)
        {
            AddForce(DGSManager.instance.GetDGSGravityMagnitude(this) * 
                physicsTimeInterval * - DGSLocalUp, 0, 
                ForceMode.VelocityChange);
        }

        /// <summary>
        /// Applies friction to the DGSBody using the GroundCheck system. 
        /// </summary>
        /// <param name="friction"> Coeficient of friction to apply. DGS 
        /// friction values all exist inclusively between zero and one</param>
        /// <param name="physicsTimeInterval"> The physics time step interval. 
        /// If called from update use Time.deltaTime, 
        /// if called from fixedUpdate use Time.fixedDeltaTime. </param>
        private void Friction(float friction, float physicsTimeInterval)
        {
            // Change TrueVelocity 
            if (Vector3.Dot(m_GlobalTargetVelocity, Acceleration) < 0)
                m_GlobalTargetVelocity += -FlatVel.normalized * friction * physicsTimeInterval;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private float GetDGSFriction()
        {
            return m_GroundCheck.StoredGroundGameObject.GetComponent<DGSBody>().MaterialFriction;
        }

        /// <summary>
        /// Removes the local Y velocity component from m_GlobalTargetVelocity.
        /// </summary>
        /// <returns> The difference between initial and final down speed</returns>
        public float NegateDownVelocity()
        {
            float downSpeed = TrueDownSpeed;
            if (downSpeed > 0) m_GlobalTargetVelocity = VectorMath.NegateDirection(m_GlobalTargetVelocity, -DGSLocalUp);
            return downSpeed - DownSpeed;
        }

        /// <summary>
        /// Removes the local X and Z velocity components from m_GlobalTargetVelocity.
        /// </summary>
        /// <returns> The magnitude of the new m_GlobalTargetVelocity vector. </returns>
        public float IsolateDownVelocity()
        {
            m_GlobalTargetVelocity = VectorMath.IsolateDirection(m_GlobalTargetVelocity, -DGSLocalUp);
            return m_GlobalTargetVelocity.magnitude;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minSpeed"></param>
        /// <param name="maxSpeed"></param>
        /// <param name="smooth"></param>
        /// <param name="physicsTimeInterval"> The physics time step interval. 
        /// If called from update use Time.deltaTime, 
        /// if called from fixedUpdate use Time.fixedDeltaTime. </param>
        public void ClampFlatVelocity(float minSpeed, float maxSpeed, bool smooth = false, float physicsTimeInterval = .5f)
        {
            if (m_GlobalTargetVelocity.magnitude <= maxSpeed && m_GlobalTargetVelocity.magnitude >= minSpeed)
                return;
            float speed;
            if (smooth)
            {
                speed = Mathf.Lerp(FlatSpeed,
                    Mathf.Abs(minSpeed - FlatSpeed) < Mathf.Abs(maxSpeed - FlatSpeed) ?
                    minSpeed : maxSpeed,
                    physicsTimeInterval);
            }
            else
            {
                speed = Mathf.Clamp(FlatSpeed, minSpeed, maxSpeed);
            }

            m_GlobalTargetVelocity = FlatVel.normalized * speed + DownVel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="t"></param>
        public void LerpFlatVelocity(Vector3 target, float t = .5f)
        {
            Vector3 init = m_GlobalTargetVelocity;
            Vector3 delta = (target - TrueFlatVel) * t;
            m_GlobalTargetVelocity += Vector3.ClampMagnitude(delta, Vector3.Distance(init, target));
        }

    }

}