
using UnityEngine;

public class HideInPerspective: MonoBehaviour {

    private MeshRenderer rend;

    void Awake() {
        rend = GetComponent<MeshRenderer>();
    }

    void Update() {
        if(!GameDirector.INSTANCE.isOrtho) rend.enabled = false;
    }
}
