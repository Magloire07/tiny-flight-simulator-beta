using UnityEngine;

namespace MFlight.Demo
{
    [System.Serializable]
    public class AerodynamicProperties
    {
        [Header("Lift Properties")]
        [Tooltip("Surface area of the wings in square meters")]
        public float wingArea = 8.0f;  // Ajusté pour un petit avion léger
        
        [Tooltip("Maximum lift coefficient")]
        public float maxLiftCoef = 1.2f;
        
        [Tooltip("Angle of attack (in degrees) where maximum lift occurs")]
        public float criticalAngleOfAttack = 12f;
        
        [Tooltip("Angle of attack (in degrees) where stall begins")]
        public float stallAngle = 15f;

        [Header("Drag Properties")]
        [Tooltip("Base drag coefficient (when angle of attack is 0)")]
        public float parasiteDragCoef = 0.03f;
        
        [Tooltip("How much drag increases with angle of attack")]
        public float inducedDragFactor = 0.1f;

        [Header("Air Properties")]
        [Tooltip("Air density in kg/m³ (default is sea level)")]
        public float airDensity = 1.225f;
        
        [Tooltip("How quickly the plane responds to aerodynamic forces")]
        public float responseMultiplier = 1f;

        [Header("Control Surface Effectiveness")]
        [Tooltip("How control effectiveness changes with airspeed")]
        public AnimationCurve controlEffectiveness = new AnimationCurve(
            new Keyframe(0f, 0f),        // No control at zero speed
            new Keyframe(20f, 0.5f),     // Some control at low speed
            new Keyframe(100f, 1f),      // Full control at cruise speed
            new Keyframe(300f, 0.7f)     // Reduced control at very high speed
        );

        [Header("Stall Behavior")]
        [Tooltip("How much control you have during a stall (0-1)")]
        public float stallControlEffectiveness = 0.3f;
        
        [Tooltip("How much random turbulence is added during stall")]
        public float stallTurbulence = 1.0f;

        public float CalculateLiftCoefficient(float angleOfAttack)
        {
            // Convert angle to degrees for easier working
            float angleDeg = angleOfAttack * Mathf.Rad2Deg;
            
            // Pre-stall regime
            if (Mathf.Abs(angleDeg) < criticalAngleOfAttack)
            {
                // Simple linear relationship before stall
                return maxLiftCoef * (angleDeg / criticalAngleOfAttack);
            }
            // Stall regime
            else
            {
                // Reduce lift after stall angle, but maintain some lift
                float stallFactor = Mathf.Lerp(1f, 0.2f, 
                    (Mathf.Abs(angleDeg) - criticalAngleOfAttack) / (stallAngle - criticalAngleOfAttack));
                return maxLiftCoef * stallFactor * Mathf.Sign(angleDeg);
            }
        }

        public float CalculateDragCoefficient(float angleOfAttack, float liftCoef)
        {
            // Basic drag formula: CD = CD0 + (CL²/π*AR*e)
            // We simplify it here with the inducedDragFactor
            return parasiteDragCoef + (liftCoef * liftCoef * inducedDragFactor);
        }

        public float GetControlEffectiveness(float airspeed)
        {
            return controlEffectiveness.Evaluate(airspeed);
        }
    }
}