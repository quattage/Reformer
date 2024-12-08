
using System;
using System.Collections.Generic;
using Animancer;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterManager : MonoBehaviour {

    [SerializeField] public WrappedKCC controller;

    [SerializeField] Camera cam;

    [SerializeField] private Animator puppet;
    [SerializeField] Transform skeletalRoot;

    [SerializeField] public float orthoCast;
    [SerializeField] public float perspCast;

    bool isFacingPositive = true;
    public bool canControl = true;

    Vector3 wishdir;

    Vector2 localVelocity;
    Vector3 velocity;
    float lerpYFacing = 0;


    //////////// ANIMATOR CALLBACKS ////////////////
    public void setPuppetPhaseLeft() {
        puppet.SetFloat("PhaseBlend", 0f);
    }

    public void setPuppetPhaseNeutral() {
        puppet.SetFloat("PhaseBlend", 0.5f);
    }

    public void setPuppetPhaseRight() {
        puppet.SetFloat("PhaseBlend", 1f);
    }

    public void enableMovement() {
        canControl = true;
    }

    public void disableMovement() {
        canControl = false;
    }
    //////////////////////////////////////////////// are these even used
    
    public void Awake() {
        lerpYFacing = skeletalRoot.rotation.eulerAngles.y;
    }

    public void updateWishdir(bool locked) {
    
        Vector3 output = Vector2.zero;

        if(!canControl) {
            wishdir = output;
            return;
        }

        if(locked) {
            if(Input.GetKey(KeyCode.A))
                output.x = -1;
            else if(Input.GetKey(KeyCode.D))
                output.x = 1;
            wishdir = output.normalized;
            localVelocity = controller.GetMoveDelta();
            return;
        }

        // ~~ fuck inputsystem lol ~~ //
        if(Input.GetKey(KeyCode.W)) {
            output.z = 1;
            if(Input.GetKey(KeyCode.A))
                output.x = -1;
            else if(Input.GetKey(KeyCode.D))
                output.x = 1;
        } else if(Input.GetKey(KeyCode.S)) {
            output.z = -1;
            if(Input.GetKey(KeyCode.A))
                output.x = -1;
            else if(Input.GetKey(KeyCode.D))
                output.x = 1;
        } else if(Input.GetKey(KeyCode.A))
            output.x = -1;
        else if(Input.GetKey(KeyCode.D))
            output.x = 1;

        wishdir = output.normalized;
        localVelocity = controller.GetMoveDelta();
    }

    void InterpolateClampedRotation(float targY) {
        float mul = GameDirector.INSTANCE.isTransitioning ? 50f : 10f;
        if(!controller.IsGrounded) mul *= 0.2f;
        skeletalRoot.transform.localRotation = Quaternion.Lerp(skeletalRoot.transform.localRotation, Quaternion.Euler(-90, targY, 0), Time.deltaTime * mul);
    }

    void Update() {
        updateWishdir(GameDirector.INSTANCE.isOrtho);
        if(canControl) {
            puppet.SetBool("IsAirborne", !controller.IsGrounded);
            if(controller.LandedThisFrame)
                puppet.SetTrigger("Land");
            if(Input.GetKeyDown(KeyCode.R))
                GameDirector.INSTANCE.TogglePerspective();
            if(canSprint() && Input.GetKey(KeyCode.LeftShift))
                controller.IsSprinting = true;
            else controller.IsSprinting = false;
        } else {
            puppet.SetBool("IsAirborne", false);
            puppet.SetFloat("XSpeed", 0);
            puppet.SetFloat("YSpeed", 0);
            puppet.SetFloat("ZSpeed", 0);
            controller.IsSprinting = false;
        }
    }

    public void OnPerspectiveChanged() {
        if(GameDirector.INSTANCE.isOrtho)
            controller.SetOrthoCollisions();
        else controller.SetPerspCollisions();
    }

    private bool canSprint() {
        if(!canControl) return false;
        if(!controller.IsGrounded) return false;
        if(GameDirector.INSTANCE.isOrtho)
            return isFacingPositive ? wishdir.x > 0 : wishdir.x < 0;

        float diff = Vector3.Dot(wishdir, velocity.normalized);
        // TODO diff
        return true;
    }

    void FixedUpdate() {
        if(!canControl) {
            controller.Move(Vector3.zero, false);
            return;
        }

        if(GameDirector.INSTANCE.isOrtho) moveOrtho();
        else movePersp();
    }

    public void moveOrtho() {
        velocity = controller.Move(wishdir, Input.GetKey(KeyCode.Space));
        if(controller.JumpedThisFrame) puppet.SetTrigger("Jump");
        puppet.SetFloat("ZSpeed", localVelocity.x * 1.2f);
        puppet.SetFloat("XSpeed", 0);
        puppet.SetFloat("YSpeed", Mathf.Min(-velocity.y, 0.3f));
        isFacingPositive = GameDirector.INSTANCE.mouseDisplace.x > 0;
        InterpolateClampedRotation(isFacingPositive ? 35 : 145);
    }

    public void movePersp() {
        Vector2 dir = new Vector2(wishdir.x, wishdir.z);
        velocity = controller.Move(dir, Input.GetKey(KeyCode.Space));

        // mouse-based facing direction deprecated (felt like shit)
        //     Vector3 diff;
        //     if(GameDirector.INSTANCE.isMouseValid)
        //         diff = GameDirector.INSTANCE.mousePositionWS - transform.position;
        //     else diff = velocity;
        //     float rY = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
        //     InterpolateClampedRotation(-rY);

        if(velocity.sqrMagnitude > 0.001f)
            lerpYFacing = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;

        InterpolateClampedRotation(lerpYFacing - 90);

        puppet.SetFloat("ZSpeed", localVelocity.x);
        puppet.SetFloat("XSpeed", localVelocity.y);
        puppet.SetFloat("YSpeed", Mathf.Min(-velocity.y, 0.3f));
    }

    private string getDebugString() {
        return $"FPS: {(1/Time.deltaTime).ToString("F0")}\n" +
                    $"deltaTime: {Time.deltaTime}\n" +
                    $"Timescale: {Time.timeScale}\n\n" +

                    $"Gravity: {controller.Gravity}\n" +
                    $"Speed: {velocity.magnitude.ToString("f2")}\n" +
                    $"Velocity: {velocity.ToString("F6")}\n" +
                    $"Position: {transform.position.ToString("F4")}\n" +
                    $"wishdir: {wishdir}\n" +
                    $"LookDir: {transform.forward.ToString("f2")}\n\n" +
                        
                    $"Grounded: {controller.IsGrounded}\n" +
                    $"On Slope: {controller.IsOnSlope}\n" +
                    $"Slope Angle: {controller.SlopeAngle}\n" +
                    $"Sliding: {controller.IsSliding}\n\n" +
                        
                    $"Crouching: N/A\n" +
                    $"Coyote: {controller.Coyote}\n"
        ;
    }


    void OnGUI() {
        if(!GameDirector.INSTANCE.debugging) return;
        GUIStyle style = GUIStyle.none;
        style.fontSize = 17;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(10, 10, 500, 500), getDebugString(), style);
    }


    
}
