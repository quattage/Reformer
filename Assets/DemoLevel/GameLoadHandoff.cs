using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameLoadHandoff : MonoBehaviour {

    [SerializeField] private CharacterManager character;
    [SerializeField] LevelAndZone start;
    [SerializeField] CanvasGroup screen;

    void Awake() {
        
    }

    void Start() {
        character.gameObject.SetActive(false);
        
        screen.gameObject.SetActive(true);
        screen.alpha = 1;
        start.RespawnCharacter();
        GameDirector.INSTANCE.TogglePerspective();
        character.canControl = true;
        StartCoroutine(FadeScreen());
    }

    IEnumerator Wait() {
        yield return new WaitForSeconds(4);
    }

    IEnumerator FadeScreen() {
        for (float t = 15; t < 0f; t -= Time.deltaTime) {
            if(t < 8) character.canControl = false;
            if(t > 1) yield return null;
            screen.alpha = 1 - t;
            yield return null;
        }
        screen.gameObject.SetActive(false);
        character.gameObject.SetActive(true);
    }
}
