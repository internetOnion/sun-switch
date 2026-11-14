using SunSwitch.Core;
using UnityEditor;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Shared prefab set-up for the phase core (Assets/Scripts/Core): every phased base prefab
    /// gets a PhaseObject that lists all its renderers and colliders; each Day / Night variant
    /// then only overrides the tag and the ghost material (Both variants have no ghost).
    /// </summary>
    internal static class PhaseHookup
    {
        const string GhostDir = "Assets/Art/Materials/Phase";

        /// Adds (or refreshes) the PhaseObject on a base prefab root.
        public static void AddPhaseObject(GameObject root)
        {
            var po = root.TryGetComponent<PhaseObject>(out var existing) ? existing : root.AddComponent<PhaseObject>();
            var so = new SerializedObject(po);
            so.FindProperty("phaseTag").enumValueIndex = (int)PhaseTag.Both;
            so.FindProperty("ghostMaterial").objectReferenceValue = null;
            Fill(so.FindProperty("renderers"), root.GetComponentsInChildren<Renderer>(true));
            Fill(so.FindProperty("colliders"), root.GetComponentsInChildren<Collider>(true));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// Sets the tag ("Day", "Night" or "Both") and matching ghost material on a variant.
        public static void SetPhase(GameObject root, string phase)
        {
            if (!root.TryGetComponent<PhaseObject>(out var po))
            {
                Debug.LogError($"[Sun Switch] {root.name} has no PhaseObject; rebuild its base prefab first.", root);
                return;
            }
            var tag = (PhaseTag)System.Enum.Parse(typeof(PhaseTag), phase);
            var ghost = tag == PhaseTag.Both ? null : AssetDatabase.LoadAssetAtPath<Material>($"{GhostDir}/M_Ghost_{phase}.mat");
            if (tag != PhaseTag.Both && ghost == null)
                Debug.LogError($"[Sun Switch] {GhostDir}/M_Ghost_{phase}.mat missing. Run Update Platform Prefabs.");

            var so = new SerializedObject(po);
            so.FindProperty("phaseTag").enumValueIndex = (int)tag;
            so.FindProperty("ghostMaterial").objectReferenceValue = ghost;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// Drops components whose script was deleted (e.g. the old art-side KillZone / Spinner).
        public static void RemoveMissingScripts(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }

        static void Fill(SerializedProperty array, Object[] items)
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
