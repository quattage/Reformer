using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleBlitTarget : MonoBehaviour {

    [SerializeField] public bool useBlit = true;
    [SerializeField] private Camera _overlaySource;
    [SerializeField] private int _pixelResolution;
    private Material mat;

    void Awake() {
        Shader blit = Shader.Find("Reformer/CustomBlit");
        mat = new Material(blit) { hideFlags = HideFlags.HideAndDontSave };
    }

    void OnRenderImage(RenderTexture from, RenderTexture to) {
        if(!useBlit) return;
        mat.SetFloat("_Res", _pixelResolution);
        mat.SetTexture("_OverlayTexture", _overlaySource.activeTexture);
        Graphics.Blit(from, to, mat);
    }
}
