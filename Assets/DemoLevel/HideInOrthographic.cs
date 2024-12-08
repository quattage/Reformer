
using UnityEngine;

public class HideInOrthographic : MonoBehaviour {

    private MeshRenderer rend;

    void Awake() {
        rend = GetComponent<MeshRenderer>();
    }

    void Update() {
        if(GameDirector.INSTANCE.isOrtho) rend.enabled = false;
    }
}
