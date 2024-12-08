using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class PhysicsExtensions {
    public static void ToWorldSpaceCapsule(this CapsuleCollider capsule, out Vector3 point0, out Vector3 point1, out float radius) {
        Vector3 center = capsule.transform.TransformPoint(capsule.center);
        radius = 0f;
        float height = 0f;
        Vector3 lossyScale = AbsVec3(capsule.transform.lossyScale);
        Vector3 dir = Vector3.zero;

        switch (capsule.direction) {
        case 0: // x
            radius = Mathf.Max(lossyScale.y, lossyScale.z) * capsule.radius;
            height = lossyScale.x * capsule.height;
            dir = capsule.transform.TransformDirection(Vector3.right);
            break;
        case 1: // y
            radius = Mathf.Max(lossyScale.x, lossyScale.z) * capsule.radius;
            height = lossyScale.y * capsule.height;
            dir = capsule.transform.TransformDirection(Vector3.up);
            break;
        case 2: // z
            radius = Mathf.Max(lossyScale.x, lossyScale.y) * capsule.radius;
            height = lossyScale.z * capsule.height;
            dir = capsule.transform.TransformDirection(Vector3.forward);
            break;
        }

        if (height < radius * 2f) {
            dir = Vector3.zero;
        }

        point1 = center + dir * (height * 0.5f - radius);
        point0 = center - dir * (height * 0.5f - radius);
    }

    public static void ToWorldSpaceCapsule(this CapsuleCollider capsule, Vector3 center, out Vector3 point0, out Vector3 point1, out float radius) {

        radius = 0f;
        float height = 0f;
        Vector3 lossyScale = AbsVec3(capsule.transform.lossyScale);
        Vector3 dir = Vector3.zero;

        switch (capsule.direction) {
        case 0: // x
            radius = Mathf.Max(lossyScale.y, lossyScale.z) * capsule.radius;
            height = lossyScale.x * capsule.height;
            dir = capsule.transform.TransformDirection(Vector3.right);
            break;
        case 1: // y
            radius = Mathf.Max(lossyScale.x, lossyScale.z) * capsule.radius;
            height = lossyScale.y * capsule.height;
            dir = capsule.transform.TransformDirection(Vector3.up);
            break;
        case 2: // z
            radius = Mathf.Max(lossyScale.x, lossyScale.y) * capsule.radius;
            height = lossyScale.z * capsule.height;
            dir = capsule.transform.TransformDirection(Vector3.forward);
            break;
        }

        if (height < radius*2f) {
            dir = Vector3.zero;
        }

        point1 = center + dir * (height * 0.5f - radius);
        point0 = center - dir * (height * 0.5f - radius);
    }

    private static Vector3 AbsVec3(Vector3 v) {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}