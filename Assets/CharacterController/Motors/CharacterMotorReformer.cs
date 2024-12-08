using System;
using UnityEngine;

[RequireComponent(typeof(WrappedKCC))]
public class CharacterMotorReformer : MonoBehaviour, ICharacterMotor {
    
    [SerializeField] private float _terminal = 15;

    [SerializeField] private float _groundAccelerate = 5;
    [SerializeField] private float _airAccelerate = 5;

    [SerializeField] private float _groundFriction = 0.1f;
    [SerializeField] private float _airFriction = 0.1f;

    [SerializeField] private float _expectedRunSpeed = 30f;

    public Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel, WrappedKCC character) {

        

        // friction
        float speed = currentVel.magnitude;
        if(!Mathf.Approximately(speed, 0)) {
            float sub = speed * (character.IsGrounded ? _groundFriction : _airFriction) * Time.fixedDeltaTime;
            if(character.IsSprinting) sub *= 0.82f;
            currentVel *= Mathf.Max(speed - sub, 0) / speed;
        }

        // projection inertia approximation
        float proj = Vector3.Dot(currentVel, wishDir);
        
        float accelFac = character.IsGrounded ? _groundAccelerate : _airAccelerate;
        if(proj < 0) accelFac *= 6.0f;
        
        float accel = accelFac * Time.fixedDeltaTime;
        if(proj + accel > _terminal)
            accel = _terminal - proj;

        return currentVel + wishDir * (character.IsSprinting ? accel : accel * 0.48f);
    }

    public void onTick(Vector3 wishDir, Vector3 currentVel, WrappedKCC character) {
        // unused for now
    }

    public float getSpeedScalar(WrappedKCC character) {
        return _expectedRunSpeed;
    }
}
