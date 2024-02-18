using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicGravitySystem
{
    [System.Serializable]
    public class DGSMass
    {
        public enum GravityType { VectorDirection, TransformCenterOfMass, None }

        [SerializeField] private GravityType gravityType;
        [SerializeField] private float mass;
        [SerializeField] private float gravityMagnitude;
        [SerializeField] private float rangeOfInfluence;
        [SerializeField] private Vector3 gravityDirection;
        [SerializeField] private Transform centerOfMass;

        public GravityType GravType { get { return gravityType; } set { gravityType = value; } }
        public float Mass { get { return mass; } set { mass = value; } }
        public float GravityMagnitude { get { return gravityMagnitude; } set { gravityMagnitude = value; } }
        public float RangeOfInfluence { get { return rangeOfInfluence; } set { rangeOfInfluence = value; } }
        public Vector3 GravityDirection { get { return gravityDirection; } }
        public Transform CenterOfMass { get { return centerOfMass; } }
    }
}