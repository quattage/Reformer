using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.AI;



/// <summary>
/// A Kinematically based character controller
/// Most of this code was yoinked from https://github.com/pokeblokdude/character-controller/blob/main/Assets/CharacterController/KinematicCharacterController.cs
/// this one's collisions are more fully realized than my own KCC and I didn't want to have to debug physics casts for a week and a half to finish this project in time.
/// The motor system has been modified to support swapping and state management between 2D and 3D
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class WrappedKCC : MonoBehaviour {
    
    [Header("Movement")]

    [Tooltip("Whether or not to apply gravity to the controller.")]
    [SerializeField] private bool m_useGravity = true;

    [Tooltip("The max speed above which falling speed will be capped.")]
    [SerializeField] private float m_maxFallSpeed = 20;

    [Tooltip("The default maximum movement speed (can be overridden by character motors).")]
    [field:SerializeField] public float MaxSpeed { get; private set; } = 5;

    
    [Header("Collision")]
    
    [Tooltip("Which layers the controller should take into account when checking for collisions.")]
    [SerializeField] private LayerMask m_collisionMask;

    [Tooltip(
        "Buffer distance inside the collider from which to start collision checks. Should be very small (but not too small)."
    )]
    [SerializeField] private float m_skinWidth = 0.015f;
    
    [Tooltip("The maximum angle at which the controller will treat the surface like a slope.")]
    [SerializeField][Range(1, 89)] private float m_maxSlopeAngle = 55;
    
    [Tooltip("The minimum angle at which the controller will treat a surface like a flat ceiling, stopping vertical movement.")]
    [SerializeField] private float m_minCeilingAngle = 165;

    [Tooltip("The maximum height for a wall to be considered a step that the controller will snap up onto.")]
    [SerializeField] private float m_maxStepHeight = 0.2f;

    [Tooltip("The minimum depth for steps that the controller can climb.")]
    [SerializeField] private float m_minStepDepth = 0.1f;

    
    [Header("Jump")]

    [Tooltip("The height the controller can jump. Determines gravity along with jumpDistance.")]
    [SerializeField] private float m_jumpHeight = 2;

    [Tooltip("The distance the controller can jump when moving at max speed. Determines gravity along with jumpHeight.")]
    [SerializeField] private float m_jumpDistance = 4;

    [Tooltip("How long (in seconds) after you leave the ground can you still jump.")]
    [SerializeField] private float m_coyoteTime = 0.2f;

    [Tooltip(
        "A transform which holds the facing direction of this KCC"
    )]
    [SerializeField] private Transform rotationGuide;

    private bool hasOrthoCollisions = false;


    public ICharacterMotor Motor { get; private set; }
    private Vector3 _moveAmount;
    private Vector3 _velocity;
    private Vector3 _groundSpeed;

    public bool IsGrounded { get; private set; }
    private bool _wasGrounded;
    public bool LandedThisFrame { get; private set; }
    public bool JumpedThisFrame { get; private set; }
    public bool IsBumpingHead { get; private set; }

    public bool IsSliding { get; private set; }
    public bool IsOnSlope { get; private set; }
    public float SlopeAngle { get; private set; }
    private Vector3 _slopeNormal;

    public bool isClimbingStep { get; private set; }

    private List<RaycastHit> _hitPoints;
    private Vector3 _groundPoint;

    public bool IsSprinting { get; set; }

    public float Gravity { get; private set; }
    private Vector3 _gravityVector;
    public bool Coyote { get; private set; }
    private bool _isJumpOnCooldown;
    private bool _jumping;
    private float _jumpForce;

    private Rigidbody _rb;
    private CapsuleCollider _col;

    private Color[] _colors = { Color.red, new Color(1, 0.5f, 0), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };

    void Awake() {
        SetPerspCollisions();
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        _col = GetComponent<CapsuleCollider>();

        Motor = GetComponent<ICharacterMotor>();

        float halfDist = m_jumpDistance/2;
        Gravity = (-2 * m_jumpHeight * MaxSpeed * MaxSpeed) / (halfDist * halfDist);
        _jumpForce = (2 * m_jumpHeight * MaxSpeed) / halfDist;

        _hitPoints = new List<RaycastHit>();
    }

    void Update() {
#if UNITY_EDITOR
        float halfDist = m_jumpDistance/2;
        Gravity = (-2 * m_jumpHeight * MaxSpeed * MaxSpeed) / (halfDist * halfDist);
        _jumpForce = (2 * m_jumpHeight * MaxSpeed) / halfDist;
#endif
    }

    void OnDrawGizmos() {

        if(_col != null) {
            _col.ToWorldSpaceCapsule(out Vector3 capsuleOB, out Vector3 capsuleOT, out float radius);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(capsuleOT, radius);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(capsuleOB, radius);
        }

        if(!GameDirector.INSTANCE.debugging) return;
        Debug.DrawRay(transform.position, _velocity, Color.green, Time.deltaTime);

        if(IsGrounded || IsSliding) {
            Gizmos.DrawWireSphere(_groundPoint, 0.05f);
        }

        if(_hitPoints == null) { return; }

        int i = 0;
        foreach(RaycastHit hit in _hitPoints) {
            Color color = _colors[i % (_colors.Length-1)];
            Gizmos.DrawWireSphere(hit.point, 0.1f);
            Debug.DrawRay(hit.point, hit.normal, color, Time.deltaTime);
            i++;
        }
    }

    /// <summary>
    ///  Moves the attached rigidbody in the desired direction, taking into account gravity, collisions, and slopes, using
    ///  the "collide and slide" algorithm. Returns the current velocity. (Pick either this or Move())
    /// </summary>
    public Vector3 Move(Vector2 moveDir, bool shouldJump) {

        _groundSpeed = Motor.Accelerate(new Vector3(moveDir.x, 0, moveDir.y), _groundSpeed, this);

        _moveAmount = _groundSpeed * Time.deltaTime;

        IsBumpingHead = CeilingCheck(transform.position);
        IsGrounded = GroundCheck(transform.position);
        LandedThisFrame = IsGrounded && !_wasGrounded;
        if(LandedThisFrame) StartCoroutine(JumpCooldown());

        // coyote time
        if(_wasGrounded && !IsGrounded) {
            StartCoroutine(CoyoteTime());
        }

        // scale movement to slope angle
        if(IsGrounded && IsOnSlope && !IsBumpingHead) {
            _moveAmount = ProjectAndScale(_moveAmount, _slopeNormal);
        }
        
        _hitPoints.Clear();

        // --- collision   
        _moveAmount = CollideAndSlide(_moveAmount, transform.position);

        // --- gravity
        if(m_useGravity) {
            _jumping = false;
            if(shouldJump && (IsGrounded || Coyote) && !_isJumpOnCooldown) {
                _gravityVector.y = _jumpForce * Time.deltaTime;
                _jumping = true;
                Coyote = false;
            } 

            if(_jumping && !JumpedThisFrame) JumpedThisFrame = true;
            else JumpedThisFrame = false;

            if((IsGrounded || _wasGrounded) && !_jumping) 
                _moveAmount += SnapToGround(transform.position + _moveAmount);

            if((IsGrounded && !_jumping) || (!IsGrounded && IsBumpingHead)) 
                _gravityVector = new Vector3(0, Gravity, 0) * Time.deltaTime * Time.deltaTime;
            else if(_gravityVector.y > -m_maxFallSpeed)
                _gravityVector.y += Gravity * Time.deltaTime * Time.deltaTime;
            
            _moveAmount += CollideAndSlide(_gravityVector, transform.position + _moveAmount, true);
        }

        // ACTUALLY MOVE THE RIGIDBODY
        _rb.MovePosition(transform.position + _moveAmount);
        
        _wasGrounded = IsGrounded;
        _velocity = _moveAmount / Time.deltaTime;
        return _velocity;
    }

    IEnumerator JumpCooldown() {
        _isJumpOnCooldown = true;
        yield return new WaitForSeconds(m_coyoteTime);
        _isJumpOnCooldown = false;
    }

    public void SetOrthoCollisions() {
        if(hasOrthoCollisions) return;
        hasOrthoCollisions = true;
        transform.position += new Vector3(0f, 1f, 0f);
        m_collisionMask = LayerMask.GetMask("CollidableOrthographic", "CollidableCommon", "CollidableBoundary");
    }

    public void SetPerspCollisions() {
        if(!hasOrthoCollisions) return;
        hasOrthoCollisions = false;
        transform.position += new Vector3(0f, 1f, 0f);
        m_collisionMask = LayerMask.GetMask("CollidablePerspective", "CollidableCommon", "CollidableBoundary");
    }

    IEnumerator CoyoteTime() {
        Coyote = true;
        yield return new WaitForSeconds(m_coyoteTime);
        Coyote = false;
    }

    private Vector3 CollideAndSlide(Vector3 velo, Vector3 pos, bool gravityPass = false) {

        Vector3 accuVelo = Vector3.zero;
        Vector3 prevIterNorm = new Vector3();
        bool isOnStep = false;

        for(int bounce = 0; bounce < 3; bounce++) {
            if(Mathf.Approximately(velo.magnitude, 0)) break;

            float castDist = velo.magnitude;
            Vector3 castDir = velo.normalized;

            _col.ToWorldSpaceCapsule(pos + _col.center, out Vector3 capsuleOB, out Vector3 capsuleOT, out _);
            if(Physics.CapsuleCast(
                capsuleOB, capsuleOT,
                radius(),
                castDir,
                out RaycastHit hit,
                castDist,
                m_collisionMask
            )) {

                _hitPoints.Add(hit);
                HandlePotentialIllusionCollider(hit.collider);

                Vector3 snapToSurface = (castDir.normalized * hit.distance) + (hit.normal * m_skinWidth);
                float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);

                if(snapToSurface.magnitude <= m_skinWidth) { snapToSurface = Vector3.zero; }
                if(gravityPass && surfaceAngle <= m_maxSlopeAngle) {
                    accuVelo += snapToSurface;
                    break;
                }

                Vector3 leftover = velo - snapToSurface;
                
                if(bounce == 0) {
                    prevIterNorm = hit.normal;
                    if(surfaceAngle > m_maxSlopeAngle && IsGrounded && !gravityPass) {
                        float stepOffset = hit.point.y - _groundPoint.y;
                        Vector3 stepDirection = hit.point - pos;
                        stepDirection = new Vector3(stepDirection.x, 0, stepDirection.z).normalized;

                        if(stepOffset < m_maxStepHeight && stepOffset > m_skinWidth) {
                            float stepDist = _col.radius - stepOffset - m_skinWidth;

                            _col.ToWorldSpaceCapsule(pos + _col.center, out capsuleOB, out capsuleOT, out _);
                            if(Physics.CapsuleCast(
                                capsuleOB + snapToSurface + new Vector3(0, stepDist, 0),
                                capsuleOT + snapToSurface + new Vector3(0, stepDist, 0),
                                radius(),
                                stepDirection,
                                out RaycastHit stepCheck,
                                m_minStepDepth + 2 * m_skinWidth,
                                m_collisionMask
                            )) {
                                print(stepCheck.distance);
                                float stepWallAngle = Vector3.Angle(stepCheck.normal, Vector3.up);
                                if((stepCheck.distance - m_skinWidth) > m_minStepDepth || stepWallAngle <= m_maxSlopeAngle) {
                                    isOnStep = true;
                                }
                            }
                            else {
                                isOnStep = true;
                            }

                            if(isOnStep) {
                                snapToSurface.y += stepDist;
                                snapToSurface += stepDirection * 2 * m_skinWidth;
                                snapToSurface += SnapToGround(pos + snapToSurface + leftover);
                            }
                        }

                        prevIterNorm = new Vector3(prevIterNorm.x, 0, prevIterNorm.z).normalized;
                        leftover = new Vector3(leftover.x, 0, leftover.z);
                    }
                    leftover = Vector3.ProjectOnPlane(leftover, prevIterNorm);
                    velo = leftover;
                }
                else if(bounce == 1) {
                    Vector3 crease = Vector3.Cross(prevIterNorm, hit.normal).normalized;
                    if(GameDirector.INSTANCE.debugging) Debug.DrawRay(hit.point, crease, Color.cyan, Time.deltaTime);
                    float dis = Vector3.Dot(leftover, crease);
                    velo = crease * dis;
                }

                if(bounce < 2) {
                    accuVelo += snapToSurface;
                    pos += snapToSurface;
                }
            }
            else {  // no collision
                accuVelo += velo;
                break;
            }
        }
        return accuVelo;
    }

    private Vector3 ProjectAndScale(Vector3 vector, Vector3 planeNormal) {
        float mag = vector.magnitude;
        vector = Vector3.ProjectOnPlane(vector, planeNormal).normalized;
        vector *= mag;
        return vector;
    }

    private Vector3 SnapToGround(Vector3 pos) {
        
        float dist = m_maxStepHeight + m_skinWidth;
        _col.ToWorldSpaceCapsule(pos + _col.center, out Vector3 capsuleOB, out Vector3 capsuleOT, out _);
        if(Physics.CapsuleCast(
            capsuleOB,
            capsuleOT,
            radius(),
            Vector3.down,
            out RaycastHit hit,
            dist,
            m_collisionMask
        )) {
            float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up);
            if(hit.distance - m_skinWidth < m_maxStepHeight && surfaceAngle <= m_maxSlopeAngle) {
                IsGrounded = true;
                return new Vector3(0, -(hit.distance - m_skinWidth), 0);
            }
        }
        return Vector3.zero;
    }

    private bool GroundCheck(Vector3 pos) {

        IsSliding = false;
        bool grounded = false;
        float dist = 2 * m_skinWidth;

        _col.ToWorldSpaceCapsule(pos + _col.center, out Vector3 capsuleOB, out _, out _);
        RaycastHit[] hits = Physics.SphereCastAll(capsuleOB, radius(), Vector3.down, dist, m_collisionMask);
        if(hits.Length > 0) {
            foreach(RaycastHit hit in hits) {
                if(Mathf.Approximately(hit.distance, 0)) { continue; }

                float angle = Vector3.Angle(Vector3.up, hit.normal);
                SlopeAngle = angle;
                _slopeNormal = hit.normal;
                _groundPoint = hit.point;
                if(angle <= m_maxSlopeAngle) {
                    IsSliding = false;
                    IsOnSlope = angle > 0.1f;
                    grounded = true;
                    break;
                }
                else { IsSliding = true; }
            }
        }
        return grounded;
    }

    private float radius() {
        Bounds b = _col.bounds;
        b.Expand(-2 * m_skinWidth);
        return b.extents.x;
    }

    private bool CeilingCheck(Vector3 pos) {
        float dist = 2 * m_skinWidth;

        _col.ToWorldSpaceCapsule(pos + _col.center, out _, out Vector3 capsuleOT, out _);

        RaycastHit hit;
        if(Physics.SphereCast(capsuleOT, _col.bounds.extents.x + m_skinWidth, Vector3.up, out hit, dist, m_collisionMask)) {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            float hitAngle = Vector3.Angle(_moveAmount.normalized, hit.normal);
            if(angle >= m_minCeilingAngle || hitAngle >= m_minCeilingAngle) {
                return true;
            }
        }
        return false;
    }

    // public List<SplitPerspectiveLevelCollider> GetNearbyIllusionColliders() {

    //     // float grow = 1.5f;
    //     // _col.ToWorldSpaceCapsule(out Vector3 capsuleOB, out Vector3 capsuleOT, out float radius);
    //     // Collider[] contacts = Physics.OverlapCapsule(
    //     //     transform.position + (_sphereOffsetBottom * grow), 
    //     //     transform.position + (_sphereOffsetTop * grow),
    //     //     _bounds.extents.x * grow,
    //     //     GameDirector.INSTANCE.character2D
    //     // );

    //     // List<SplitPerspectiveLevelCollider> accepted = new();
    //     // if(contacts.Length == 0) return accepted;

    //     // foreach(Collider coll in contacts) {
    //     //     SplitPerspectiveLevelCollider splc = coll.GetComponent<SplitPerspectiveLevelCollider>();
    //     //     if(splc != null) accepted.Add(splc);
    //     // }

    //     // return accepted;
    // }

    public void HandlePotentialIllusionCollider(Collider c) {
        if(c == null) return;
        if(!GameDirector.INSTANCE.isOrtho) return;

        SplitPerspectiveLevelCollider lco = c.transform.GetComponent<SplitPerspectiveLevelCollider>();
        if(lco != null) {
            if(lco.NeedsTransformation(transform))
                lco.TransformWorldDepth(transform);
        }

        Transform parent = c.transform.parent;
        if(parent == null) return;

        SplitPerspectiveLevelCollider lcp = parent.GetComponent<SplitPerspectiveLevelCollider>();
        if(lcp == null) return;

        if(lcp.NeedsTransformation(transform))
            lcp.TransformWorldDepth(transform);
    }


    // public void CheckCameraTriggers() {
    //     _col.ToWorldSpaceCapsule(out Vector3 capsuleOB, out Vector3 capsuleOT, out float radius);
    //     Collider[] overlaps = Physics.OverlapCapsule(capsuleOB, capsuleOT, radius);
    //     foreach(Collider c in overlaps) {
    //         if(!c.isTrigger) return;
    //     }
    // }

    public Vector3 GetVelocity() {
        return _rb.velocity;
    }

    public Vector2 GetMoveDelta() {
        Vector3 rotated = rotationGuide.InverseTransformDirection(_rb.velocity);
        return new Vector2(rotated.x / Motor.getSpeedScalar(this), rotated.y / Motor.getSpeedScalar(this));
    }


    // private bool UpdateCrouchState(bool shouldCrouch) {
    //     if(shouldCrouch && !IsCrouching) {
    //         _col.height = m_crouchHeight;
    //         _col.center = new Vector3(0, _col.height/2, 0);
    //         return true;
    //     }
    //     else if(IsCrouching && !shouldCrouch) {
    //         if(CanUncrouch()) {
    //             _col.height = _height;
    //             _col.center = new Vector3(0, _col.height/2, 0);
    //             return false;
    //         }
    //     }
    //     return IsCrouching;
    // }
    //
    // private bool CanUncrouch() {
    //     float dist = _height - m_crouchHeight + m_skinWidth;
    //     Vector3 origin = _col.bounds.center + new Vector3(0, _col.height/2 - _col.radius, 0);
    //     return !Physics.SphereCast(origin, radius(), Vector3.up, out _, dist, m_collisionMask);
    // }
}