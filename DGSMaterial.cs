using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicGravitySystem
{
    [System.Serializable]
    public struct DGSMaterial
    {
        [Tooltip("Friction ranges between 0 and 1, where 0 is no friction " +
            "and 1 is such high friction that movement is not possible")]
        [Range(0, 1)] public float friction;
        public float bounciness;

        public DGSMaterial(float frict, float bounce)
        {
            friction = Mathf.Clamp01(frict);
            bounciness = bounce;
        }
    }

}