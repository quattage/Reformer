
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PerspectiveCollisions : Editor {
    
    [MenuItem("GameObject/REFORMER/Make SPLE", false, 10)]
    public static void AddSplitPerspectiveLevelElement() {
        GameObject targetMesh = Selection.activeGameObject;

        PrefabInstanceStatus prefabStatus = PrefabUtility.GetPrefabInstanceStatus(targetMesh);
        if(prefabStatus != PrefabInstanceStatus.NotAPrefab)
            PrefabUtility.UnpackPrefabInstance(targetMesh, PrefabUnpackMode.Completely, InteractionMode.UserAction);

        GameObject parent = new GameObject(targetMesh.name + "_SPLE");
        parent.transform.position = targetMesh.transform.position;
        parent.transform.rotation = targetMesh.transform.rotation;
        targetMesh.transform.parent = parent.transform;

        MeshCollider collider = targetMesh.GetComponent<MeshCollider>();
        if(collider == null) collider = targetMesh.AddComponent<MeshCollider>();
        collider.convex = false;

        MeshRenderer renderer = targetMesh.GetComponent<MeshRenderer>();
        renderer.receiveShadows = true;
        renderer.staticShadowCaster = true;
        string prename = targetMesh.name;
        targetMesh.name += "_MESH";

        GameObject perspChild = new GameObject(prename + "_PERSP");
        perspChild.transform.localScale = targetMesh.transform.localScale;
        perspChild.transform.position = targetMesh.transform.position;
        perspChild.transform.rotation = targetMesh.transform.rotation;
        perspChild.transform.parent = parent.transform;
        perspChild.layer = LayerMask.NameToLayer("CollidablePerspective");

        MeshCollider smp = perspChild.AddComponent<MeshCollider>();
        smp.sharedMesh = targetMesh.GetComponent<MeshFilter>().sharedMesh;
        

        GameObject orthoChild = new GameObject(prename + "_ORTHO");
        orthoChild.transform.localScale = targetMesh.transform.localScale;
        orthoChild.transform.position = targetMesh.transform.position;
        orthoChild.transform.rotation = targetMesh.transform.rotation;
        orthoChild.transform.parent = parent.transform;
        orthoChild.layer = LayerMask.NameToLayer("CollidableOrthographic");
        
    

        MeshCollider smc = orthoChild.AddComponent<MeshCollider>();
        smc.sharedMesh = targetMesh.GetComponent<MeshFilter>().sharedMesh;

        Vector3 scaleFactor = new Vector3(1f, 1f, 5000f);
        scaleFactor = orthoChild.transform.InverseTransformVector(scaleFactor);
        
        // orthoChild.transform.localScale = Vector3.Scale(orthoChild.transform.localScale, scaleFactor);

        SplitPerspectiveLevelCollider splc = parent.AddComponent<SplitPerspectiveLevelCollider>();
        splc.initialSetup(targetMesh, orthoChild);
    }

    [MenuItem("GameObject/REFORMER/Add SPLE", true)]
    private static bool ValidateModifySelectedObject() {
        if(Selection.activeGameObject == null) return false;
        return Selection.activeGameObject.GetComponent<MeshRenderer>() != null;
    }
}
