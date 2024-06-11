using System;
using UnityEngine;

[Serializable]
public class OlderEngineComponent
{
    public float friction = 0.01f;
    public float angularVelocity = 0.0f;
    public float throttle = 0.0f;
    public float inertia = 0.1f;

    public float idleRPMTorque = 180.0f;
    public float peakRPMTorque = 320.0f;
    public float maxRPMTorque = 80.0f;

    public float idleRPM = 1000.0f;
    public float peakRPM = 5000.0f;
    public float maxRPM = 8000.0f;

    private const float RAD_to_RPM = 9.54929658551f; // TODO RAD to RPM conversion
    private const float RPM_to_RAD = 0.10471975512f; // TODO RPM to RAD conversion

    public float Torque { get; private set; } = 0.0f;

    public void EngineOutput(float deltaTime)
    {
        float effectiveTorque = EvaluateRPM(angularVelocity * RAD_to_RPM) * (throttle < 0 ? 0 : throttle > 1 ? 1 : throttle);
        float frictionTorque = -angularVelocity * friction;
        Torque = effectiveTorque + frictionTorque;
        angularVelocity = angularVelocity + (Torque / inertia * deltaTime);

        // clamp
        angularVelocity = angularVelocity < 0 ? 0 : angularVelocity > (15000 * RPM_to_RAD) ? angularVelocity = 15000 * RPM_to_RAD : angularVelocity;
    }

    public void EngineInput(float deltaTime, float torque)
    {

    }

    private float EvaluateRPM(float rpm)
    {
        float result = 0.0f;

        if (rpm <= idleRPM)
        {
            result = idleRPMTorque;
        }
        else if (rpm <= peakRPM)
        {
            result = idleRPMTorque + (peakRPMTorque - idleRPMTorque) * ((rpm - idleRPM) / (peakRPM - idleRPM));
        }
        else if (rpm <= maxRPM)
        {
            result = peakRPMTorque + (maxRPMTorque - peakRPMTorque) * ((rpm - peakRPM) / (maxRPM - peakRPM));
        }
        else
        {
            result = maxRPMTorque;
        }

        return result;
    }
}