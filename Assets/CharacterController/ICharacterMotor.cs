using UnityEngine;

public interface ICharacterMotor {
    void onTick(Vector3 wishDir, Vector3 currentVel, WrappedKCC character);
    Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel, WrappedKCC character);
    float getSpeedScalar(WrappedKCC character);
}
