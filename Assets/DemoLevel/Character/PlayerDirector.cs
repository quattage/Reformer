using System;
using System.Collections;
using OccaSoftware.Altos.Runtime;
using OccaSoftware.Buto.Runtime;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements.Experimental;

public class GameDirector : MonoBehaviour {

    [SerializeField] public bool debugging = true;

    [SerializeField] private Camera _cam;
    [SerializeField] private float focusDistance = 100;
    [SerializeField] private float perspectiveShiftSpeed = 5f;

    [SerializeField] private VolumeProfile post;
    [SerializeField] private Light flasher;

    [SerializeField] private float fogMin = 7;
    [SerializeField] private float fogMax = -8.62f;
    [SerializeField] private float fogDensity = 9.61f;
    [SerializeField] private float orthoFogMin;
    [SerializeField] private float orthoFogMax;
    [SerializeField] private float orthoFogDensity;

    [SerializeField] private Transform fakeCharacter;
    [SerializeField] private Transform characterFocusPoint;
    [SerializeField] private Transform rotator;
    [SerializeField] private Transform panner;

    
    [SerializeField] private RenderTexture blitTarget;
    [SerializeField] CharacterManager manager;

    private Quaternion initialRotation;

    
    public LayerMask mouseTargetables;
    public LayerMask character2D;
    public LayerMask character3D;

    [DoNotSerialize] public bool isTransitioning { get; private set; }
    [DoNotSerialize] public bool isOrtho { get; private set; }
    private bool canToggle = true;
    private float orthoFactor = 0;

    private float fov = 90f;
    private float lerpFOVTarget = -1f;

    private Vector3 position;
    private Vector3 startRotation;
    private float size;


    [SerializeField] private float maxZDepth = 3f;
    [SerializeField] private float minZDepth = -10f;

    private ButoVolumetricFog fog;
    private LensDistortion lens;
    private ChromaticAberration aber;
    private float aberStrength;

    private float updatect = 0;

    [DoNotSerialize] public bool isMouseValid;
    [DoNotSerialize] public Vector3 mousePositionWS;
    [DoNotSerialize] public Vector3 cameraPositionTarget;

    [SerializeField] private GameObject cursor;

    [DoNotSerialize] public Vector2 mouseDisplace = Vector2.zero;
    [DoNotSerialize] public Vector3 mouseDirectionRelative = Vector3.zero;

    public CustomPerspectiveCameraTrigger overlappingDirectorOverride;

    public static GameDirector instance;
    public static GameDirector INSTANCE {
        get {
            if(instance == null)
                instance = FindObjectOfType<GameDirector>();

            return instance;
        }
    }

    public void Awake() {

        initialRotation = panner.transform.rotation;
        cursor = Instantiate(cursor);

        mouseTargetables = LayerMask.GetMask("CollidablePerspective", "CollidableCommon", "CollidableOrthographic");
        character3D = LayerMask.GetMask("Default", "Water", "Character", "CollidablePerspective", "CollidableCommon", "CollidableBoundary");
        character2D = LayerMask.GetMask("Default", "Water", "2DCharacter", "CollidableOrthographic", "CollidableCommon", "CollidableBoundary");

        fakeCharacter.gameObject.SetActive(true);
        _cam.orthographic = false;
        isTransitioning = false;

        flasher.enabled = true;
        flasher.GetComponent<ButoLight>().enabled = true;
        
        startRotation = rotator.rotation.eulerAngles;
        post.TryGet<ButoVolumetricFog>(out fog);
        post.TryGet<LensDistortion>(out lens);
        post.TryGet<ChromaticAberration>(out aber);

        fog.fogDensity.value = fogDensity;
        fog.baseHeight.value = fogMin;
        fog.attenuationBoundarySize.value = fogMax;

        aberStrength = aber.intensity.value;
    }




    public void Update() {
        TrackMouseAndMoveCamera();
        if(updatect < 1) {
            updatect += Time.deltaTime;
            AltosSkyDirector.Instance.skyDefinition.SetSystemTime(9);
        } else if(updatect < 2) {
            updatect += Time.deltaTime;
            AltosSkyDirector.Instance.skyDefinition.SetSystemTime(-5);
        } else if(updatect < 3) {
            updatect += Time.deltaTime;
            AltosSkyDirector.Instance.skyDefinition.SetSystemTime(5);
        }
    }

    public void TrackMouseAndMoveCamera() {

        float scaleX = (float)blitTarget.width / (float)Screen.width;
        float scaleY = (float)blitTarget.height / (float)Screen.height;

        float rtMPX = Input.mousePosition.x * scaleX;
        float rtMPY = Input.mousePosition.y * scaleY;

        Vector2 viewMousePos = new Vector2(rtMPX, rtMPY);

        Ray mouseRay = _cam.ScreenPointToRay(viewMousePos);
        isMouseValid = Physics.Raycast(mouseRay, out RaycastHit hitInfo, isOrtho ? 5000 : 1000, mouseTargetables);

        mouseDisplace = new Vector2(Input.mousePosition.x - (Screen.width / 2f), Input.mousePosition.y - (Screen.height / 2f));
        mouseDirectionRelative = mousePositionWS - characterFocusPoint.position;
        mousePositionWS = hitInfo.point;

        if(GameDirector.INSTANCE.debugging) {
            Debug.DrawRay(characterFocusPoint.position, mouseDirectionRelative, Color.cyan);
            Debug.DrawLine(_cam.transform.position, hitInfo.point, Color.yellow, Time.deltaTime);
            if(isMouseValid) {
                cursor.SetActive(true);
                cursor.transform.position = mousePositionWS;
                cursor.transform.rotation = Quaternion.LookRotation(hitInfo.normal, Vector3.up);
                Debug.DrawRay(hitInfo.point, hitInfo.normal * 50f);
            } else {
                cursor.SetActive(false);
                cursor.transform.position = Vector3.zero;
            }
        } else {
            cursor.SetActive(false);
            cursor.transform.position = Vector3.zero;
        }

        if(!isOrtho && overlappingDirectorOverride != null) {
            panner.transform.position = Vector3.Lerp(panner.transform.position, overlappingDirectorOverride.getCameraPosition(), Time.deltaTime * 5f);
            panner.transform.rotation = Quaternion.Slerp(panner.transform.rotation, overlappingDirectorOverride.getCameraRotation(), Time.deltaTime * 5f);
            return;
        } else {
            panner.transform.rotation = Quaternion.Slerp(panner.transform.rotation, initialRotation, Time.deltaTime * 5f);
        }

        Vector2 playerPos = new Vector2(characterFocusPoint.position.x, characterFocusPoint.position.y);
        Vector2 mDS = new Vector2(mouseDisplace.x / 40, mouseDisplace.y / 60);
        Vector2 cameraTarget = playerPos + mDS + getSituationalAdjustment();

        float distanceFactor = Vector2.Distance(playerPos, mDS);
        Vector3 orthoTarget = Vector3.Lerp(panner.transform.position, new Vector3(cameraTarget.x, cameraTarget.y, panner.transform.position.z), Time.deltaTime * 3);

        float zDepth = characterFocusPoint.position.z;
        zDepth = Mathf.Clamp(zDepth, minZDepth, maxZDepth);
        Vector3 cfP = new Vector3(characterFocusPoint.position.x, characterFocusPoint.position.y, zDepth);
        Vector3 perspTarget = Vector3.Lerp(panner.transform.position, cfP + new Vector3(0, -18, 0), Time.deltaTime * 5);

        panner.transform.position = Vector3.Lerp(perspTarget, orthoTarget, orthoFactor);
    }


    public Vector2 getSituationalAdjustment() {
        // TODO grounded positional offsets and target context
        return new Vector2(0, 15);
    }

    public void TogglePerspective() {
        if(!canToggle) return;
        if(isTransitioning) return;
        if(isOrtho) {
            StartCoroutine(ToggleCooldown());
            StartCoroutine(Ortho2Perspective());
            StartCoroutine(Pulse(500));
        } else {
            StartCoroutine(ToggleCooldown());
            StartCoroutine(Perspective2Ortho());
        }
    }

    public void SetToOrthographic() {
        if(isOrtho) return;
        StartCoroutine(ToggleCooldown());
        StartCoroutine(Perspective2Ortho());
    }

    IEnumerator ToggleCooldown() {
        canToggle = false;
        yield return new WaitForSeconds(1f);
        canToggle = true;
    }

    IEnumerator Perspective2Ortho() {

        isTransitioning = true;
        isOrtho = true;

        Transform trans = _cam.transform;
        fov = _cam.fieldOfView;
        position = trans.localPosition + trans.forward * focusDistance;
        size = focusDistance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

        for (float t = 0; t < 1.1f; t += Time.deltaTime * perspectiveShiftSpeed * 0.38f) {

            t = Mathf.Clamp(t, 0, 1f);

            float tE = Easing.InCirc(t);
            float f = Mathf.Lerp(fov, 0, tE);
            float d = size / Mathf.Tan(f * 0.5f * Mathf.Deg2Rad);

            orthoFactor = Easing.OutCirc(t);
            _cam.fieldOfView = f;
            rotator.transform.localRotation = Quaternion.Euler(Vector3.Slerp(rotator.transform.localRotation.eulerAngles, Vector3.zero, tE));
            
            Vector3 targetPos = position - trans.forward * d;
            if(!(float.IsNaN(targetPos.x) || float.IsNaN(targetPos.y) || float.IsNaN(targetPos.z)))
                trans.localPosition = targetPos;

            if(t > 0.7) {
                manager.OnPerspectiveChanged();
                fog.fogDensity.value = orthoFogDensity;
                fog.baseHeight.value = orthoFogMin;
                fog.attenuationBoundarySize.value = orthoFogMax;
                StartCoroutine(Pulse(0.4f));
            }

            if(f < 1f || d > 700) break;
            yield return null;
        }

        orthoFactor = 1;
        fakeCharacter.gameObject.SetActive(true);
        _cam.cullingMask = character2D;
        _cam.orthographic = true;
        _cam.orthographicSize = size;
        _cam.fieldOfView = fov;
        isTransitioning = false;
    }


    IEnumerator Ortho2Perspective() {

        isTransitioning = true;
        isOrtho = false;

        fog.fogDensity.value = fogDensity;
        fog.baseHeight.value = fogMin;
        fog.attenuationBoundarySize.value = fogMax;

        _cam.cullingMask = character3D;
        fakeCharacter.gameObject.SetActive(false);

        Transform trans = _cam.transform;
        size = _cam.orthographicSize;
        _cam.orthographic = false;
        orthoFactor = 0;

        for (float t = 0; t < 1.1f; t += Time.deltaTime * perspectiveShiftSpeed * 0.14f) {

            float tE = Easing.OutCirc(t);
            float f = Mathf.Lerp(1f, fov, tE);
            float d = size / Mathf.Tan(f * 0.5f * Mathf.Deg2Rad);

            _cam.fieldOfView = f;
            rotator.transform.localRotation = Quaternion.Euler(Vector3.Slerp(rotator.transform.localRotation.eulerAngles, startRotation, Easing.OutBounce(t)));
            trans.localPosition = position - trans.forward * d;

            if(t > 0.85) {
                isTransitioning = false;
                manager.OnPerspectiveChanged();
            }
            yield return null;
        }
    }


    public void resetCamera() {
        panner.transform.position = characterFocusPoint.position;
    }


    IEnumerator Pulse(float strength) {
        for (float t = 0; t < 1.0f; t += Time.deltaTime * perspectiveShiftSpeed * 2) {
            float scalar = Mathf.Sin(t * Mathf.PI);
            aber.intensity.value = Mathf.Lerp(0.1f, 1f, scalar);
            flasher.intensity = Mathf.LerpUnclamped(0f, strength, scalar);
            yield return null;
        }

        flasher.intensity = 0;
    }


    // this bit adds an editor gizmo to the view frustum its pretty cool :)))
    private void OnDrawGizmosSelected() {
        if (!isTransitioning) {
            isOrtho = _cam.orthographic;
            if (_cam.orthographic) {
                size = _cam.orthographicSize;
            }
            else {
                size = focusDistance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                fov = _cam.fieldOfView;
                position = _cam.transform.position + _cam.transform.forward * focusDistance;
            }
        }
        float a = _cam.aspect;
        Gizmos.color = Color.magenta;
        var u = _cam.transform.up * size;
        var r = _cam.transform.right * size*a;
        Gizmos.DrawLine(position + u + r, position - u + r);
        Gizmos.DrawLine(position + u - r, position - u - r);
        Gizmos.DrawLine(position + u + r, position + u - r);
        Gizmos.DrawLine(position - u + r, position - u - r);
        Gizmos.DrawLine(position + u + r, position - u - r);
        Gizmos.DrawLine(position - u + r, position - u + r);
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(mousePositionWS, 1f);
    }
}