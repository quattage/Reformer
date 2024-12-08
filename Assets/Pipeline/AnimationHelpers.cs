

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Animancer;
using UnityEditor;
using UnityEngine;

public class AnimationHelpers : Editor {

    [MenuItem("Reformer/Auto Apply Animations")]
    public static void AddAnims() {




        GameObject active = Selection.activeGameObject;
        if(active == null) return;

        CharacterManager manager = active.GetComponent<CharacterManager>();
        if(manager == null) return;


        MethodInfo getActiveFolderPath = typeof(ProjectWindowUtil).GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
        string path = (string) getActiveFolderPath.Invoke(null, null);
        
        if(string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) {
            Debug.LogWarning("Can't add animations - '" + path + "' is not a valid asset path!");
            return;
        }

        // string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { path });
        // AnimationClip[] clips = guids.Select(guid => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
        // manager.anims = new ClipTransition[clips.Length];
        // for(int x = 0; x < clips.Length; x++) {
        //     manager.anims[x] = new ClipTransition();
        //     manager.anims[x].Clip = AdjustClip(clips[x]);
        // }

        // Debug.Log("Applied " + clips.Length + "AnimationClips to '" + active.name + "'");
    }

    private static AnimationClip AdjustClip(AnimationClip clip) {

        SerializedObject clipS = new SerializedObject(clip);
        SerializedProperty props = clipS.FindProperty("m_AnimationClipSettings");

        if(clip.name.ToUpper().Contains("IDLE")) {
            SerializedProperty loopTime = props.FindPropertyRelative("m_LoopTime");
            loopTime.boolValue = true;
            clipS.ApplyModifiedProperties();
        }

        float trimTime = 2 / clip.frameRate;
        SerializedProperty startTime = props.FindPropertyRelative("m_StartTime");
        startTime.floatValue += trimTime;
        clipS.ApplyModifiedProperties();

        return clip;
    }
}
