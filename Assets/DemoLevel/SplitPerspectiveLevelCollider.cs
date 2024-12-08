
using UnityEngine;

[ExecuteInEditMode]
public class SplitPerspectiveLevelCollider : MonoBehaviour {

        [field:SerializeField] public Collider PerspCollider { get; private set; }
        [field:SerializeField] public Collider OrthoCollider { get; private set; }

        [field:SerializeField] public float WorldDepth { get; private set; }

        public void initialSetup(GameObject persp, GameObject ortho) {
            PerspCollider = persp.GetComponent<Collider>();
            OrthoCollider = ortho.GetComponent<Collider>();
            WorldDepth = persp.transform.TransformPoint(PerspCollider.bounds.center - persp.transform.position).z;
        }

//         public void Update() {
// #if UNITY_EDITOR
//             WorldDepth = PerspCollider.transform.TransformPoint(PerspCollider.bounds.center - PerspCollider.transform.position).z;
// #endif
//         }

        void OnDrawGizmosSelected() {
            
        }

        public  Vector3 TransformWorldDepth(GameObject obj) {
            obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, WorldDepth);
            return obj.transform.position;
        }

        public Vector3 TransformWorldDepth(Transform trn) {
            Debug.Log("Transformed " + transform.name + " to " + WorldDepth);
            trn.position = new Vector3(trn.position.x, trn.position.y, WorldDepth);
            return trn.position;
        }

        public Vector3 TransformWorldDepth(Vector3 pos) {
            pos.z = WorldDepth;
            return pos;
        }

        public bool NeedsTransformation(GameObject obj) {
            return Mathf.Abs(WorldDepth - obj.transform.position.z) > 0.1f;
        }

        public bool NeedsTransformation(Transform trn) {
            return Mathf.Abs(WorldDepth - trn.position.z) > 0.1f;
        }

        public bool NeedsTransformation(Vector3 pos) {
            return Mathf.Abs(WorldDepth - pos.z) > 0.1f;
        }
    }