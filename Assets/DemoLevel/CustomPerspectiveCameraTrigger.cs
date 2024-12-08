using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomPerspectiveCameraTrigger : MonoBehaviour {
    [SerializeField] private Collider trigger;
    [SerializeField] private Transform camTransform;

    public Vector3 getCameraPosition() {
        return camTransform.position;
    }

    public Quaternion getCameraRotation() {
        return camTransform.rotation;
    }

    public void OnTriggerEnter(UnityEngine.Collider other) {
        Debug.Log("ENTER COLLISION");
        GameDirector.INSTANCE.overlappingDirectorOverride = this;
    }

    public void OnTriggerExit(UnityEngine.Collider other) {
        Debug.Log("EXIT COLLISION");
        GameDirector.INSTANCE.overlappingDirectorOverride = null;
    }


    public override string ToString() {
        return "CAMERATRIGGER-" + gameObject.name;
    }
}
