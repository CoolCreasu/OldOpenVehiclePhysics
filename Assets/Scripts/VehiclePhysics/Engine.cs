using System;

[Serializable]
public class Engine
{
    public float friction = 0.35f;
    public float inertia = 0.2f;

    private float idleRPM = 1000.0f;
    private float peakRPM = 5500.0f;
    private float maxRPM = 8000.0f;

    private float idleRPMTorque = 180.0f;
    private float peakRPMTorque = 360.0f;
    private float maxRPMTorque = 80.0f;

    private const float RAD_to_RPM = 9.54929658551f; // TODO RAD to RPM conversion
    private const float RPM_to_RAD = 0.10471975512f; // TODO RPM to RAD conversion

    private float throttle = 0.0f;
    private float angularVelocity = 0.0f;
    private float torque = 0.0f;

    public float Throttle
    {
        get { return throttle; }
        set { throttle = value < 0 ? 0 : value > 1 ? 1 : value; }
    }

    public float AngularVelocity
    {
        get { return angularVelocity; }
    }

    public float Torque
    {
        get { return torque; }
    }

    public float RPM
    {
        get { return angularVelocity * RAD_to_RPM; }
    }

    public void EngineOutput(float deltaTime)
    {
        // torque from engine
        torque = (EvaluateRPM(angularVelocity * RAD_to_RPM) * throttle) - (angularVelocity - (idleRPM * RPM_to_RAD)) * friction;

        angularVelocity = angularVelocity + (torque / inertia * deltaTime);
    }

    public void EngineInput(float deltaTime, float reactionTorque)
    {
        // reaction torque from wheels
        angularVelocity = angularVelocity + (reactionTorque / inertia * deltaTime);
    }

    private float EvaluateRPM(float rpm)
    {
        return rpm < idleRPM ? idleRPMTorque :
            rpm < peakRPM ? idleRPMTorque + ((peakRPMTorque - idleRPMTorque) * ((rpm - idleRPM) / (peakRPM - idleRPM))) :
            rpm < maxRPM ? peakRPMTorque + ((maxRPMTorque - peakRPMTorque) * ((rpm - peakRPM) / (maxRPM - peakRPM))) :
            maxRPMTorque;
    }
}
