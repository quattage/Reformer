
using UnityEngine;

public class CharacterFollower : MonoBehaviour {

    [SerializeField] private Transform followerTarget;


    [SerializeField] Transform[] lockedZ;
    
    private Vector3 offset;

    void Start() {
        offset = transform.position;
        offset -= followerTarget.transform.position;
    }


    void Update() {
        transform.position = followerTarget.position;
        transform.position += offset;

        foreach(Transform transform in lockedZ)
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
    }
}
