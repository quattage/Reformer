
using System;
using System.Collections;
using UnityEngine;

public class LightGlitcher : MonoBehaviour {

    private float _matOrig;
    private Material _mat;
    [SerializeField] private Light[] _lights;
    private float[] _origs;

    private float time;
    private bool isOnBreak = false;

    [SerializeField] private float m_speed = 1;
    [SerializeField] private float m_contrast = 1;
    [SerializeField] private float m_breakTime = 1;

    void Awake() {
        _mat = GetComponent<MeshRenderer>().material;
        _matOrig = _mat.GetFloat("_Glow");
        _mat.SetFloat("_Glow", 0);
        if(_lights == null) return;
        _origs = new float[_lights.Length];
        for(int x = 0; x < _lights.Length; x++)
            _origs[x] = _lights[x].intensity;
    }

    void Update() {
        if(isOnBreak) return;
        time += Time.deltaTime * m_speed;
        if(time >= 1) {
            time = 0;
            StartCoroutine(takeBreak());
        }

        float mul = Mathf.Sin(time * Mathf.PI);
        _mat.SetFloat("_Glow", _matOrig * mul * m_contrast);
        if(_lights == null) return;
        for(int x = 0; x < _lights.Length; x++)
            _lights[x].intensity = _origs[x] * mul * m_contrast;
    }

    IEnumerator takeBreak() {
        isOnBreak = true;
        yield return new WaitForSeconds(m_breakTime);
        isOnBreak = false;
    }
}
