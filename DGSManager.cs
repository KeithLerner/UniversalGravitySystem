using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicGravitySystem
{
    public class DGSManager : MonoBehaviour
    {
        public static DGSManager instance;

        [Tooltip("Default of 6.67e-11")]
        public float universalGravitationalConstant = 6.67e-11f;
        [Tooltip("Does not include DGSBodies with gravity types of None")]
        public DGSBody[] sceneDGSBodies;
        [Tooltip("Calculate gravitational force using a simplified variation of Newton's Law of Universal Gravitation. " +
            "\nThis variation ignores mass.")]
        public bool simplifiedUniversalForceCalculation = false;

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(this.gameObject);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            sceneDGSBodies = GetSceneBodies();
        }

        public DGSBody[] GetSceneBodies()
        {
            sceneDGSBodies = FindObjectsOfType(typeof(DGSBody)) as DGSBody[];
            List<DGSBody> dgsList = new List<DGSBody>();
            foreach (var body in sceneDGSBodies)
            {
                if (body.mass.GravType == DGSMass.GravityType.None) continue;
                else dgsList.Add(body);
            }
            sceneDGSBodies = dgsList.ToArray();
            return sceneDGSBodies;
        }

        /// <summary>
        /// Checks if DGSBody b1 is in the Range of Influence of DGSBody b2. 
        /// Used when checking what gravity magnitude should be applied.
        /// </summary>
        /// <param name="b1"> The DGSBody being checked </param>
        /// <param name="b2"> The DGSBody being checked against </param>
        /// <returns> True when DGSBody b1 is in the Range of Influence of DGSBody b2, false otherwise. </returns>


        public bool IsInRangeOfInfluence(DGSBody b1, DGSBody b2)
        {
            if (b1.mass.GravType == DGSMass.GravityType.TransformCenterOfMass)
            {
                return Vector3.Distance(b1.mass.CenterOfMass.transform.position,
                    b2.mass.CenterOfMass.transform.position) < b2.mass.RangeOfInfluence + b2.transform.lossyScale.magnitude;
            }
            else
            {
                // Determine distance in direciton parrallel to gravity
                return Vector3.Dot(b1.mass.CenterOfMass.position, GetDGSUpDirection(b1))
                    < b2.mass.RangeOfInfluence;
            }
        }

        public DGSBody GetClosestDGSBody(DGSBody b1)
        {
            DGSBody b = b1;
            float d = Mathf.Infinity;
            foreach (DGSBody body in sceneDGSBodies)
            {
                if (body == b1) continue;
                float _d = Vector3.Distance(body.transform.position, b1.transform.position);
                if (_d < d)
                {
                    d = _d;
                    b = body;
                }
            }
            return b;
        }

        public float GetDistanceToClosestDGSBody(DGSBody b1)
        {
            float d = Mathf.Infinity;
            foreach (DGSBody b in sceneDGSBodies)
            {
                float _d = Vector3.Distance(b.transform.position, b1.transform.position);
                if (_d < d)
                {
                    d = _d;
                }
            }
            return d;
        }

        public Vector3 GetDGSUpDirection(DGSBody b1)
        {
            DGSBody b2 = GetClosestDGSBody(b1);
            if (b2.mass.GravType == DGSMass.GravityType.VectorDirection)
            {
                return -b2.mass.GravityDirection.normalized;
            }
            else
            {
                return (b1.transform.position - b2.transform.position).normalized;
            }
        }

        public float GetDGSGravityMagnitude(DGSBody b1)
        {
            DGSBody b2 = GetClosestDGSBody(b1);
            if (b2.mass.GravType == DGSMass.GravityType.TransformCenterOfMass &&
                simplifiedUniversalForceCalculation)
                return universalGravitationalConstant /
                    Vector3.Distance(b1.transform.position, b2.transform.position);
            else
                return b2.mass.GravityMagnitude;
        }
    }

}