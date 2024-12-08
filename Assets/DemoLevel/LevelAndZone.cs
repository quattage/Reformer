using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelAndZone : MonoBehaviour {

    [SerializeField] private CharacterManager character;

    void OnTriggerExit(UnityEngine.Collider other) {
        RespawnCharacter();
    }

    void Update() {
        if(Input.GetKeyDown(KeyCode.F12)) RespawnCharacter();
    }

    public void RespawnCharacter() {
        character.transform.position = transform.position;
        GameDirector.INSTANCE.SetToOrthographic();
    }
}
