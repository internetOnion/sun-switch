using System.Linq;
using SunSwitch.Art;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Builds the Lightkeeper player-character prefab from the imported model
    /// (see LightkeeperPostprocessor). Safe to run again: materials and the prefab are updated in
    /// place (same GUIDs); the animator controller is only created when it doesn't exist yet, so
    /// gameplay changes made to it are kept.
    ///
    ///   Lightkeeper            model root: Animator (Humanoid avatar, AC_Lightkeeper), ~1.8 m tall,
    ///   │                      faces +Z, pivot at the feet. Add CharacterController + controller here.
    ///   └─ … chest bone
    ///        └─ Lantern        LanternGlow (Day / Night colour) + Lantern_Light point light
    ///
    /// AC_Lightkeeper uses the Starter Assets Third Person parameter names
    /// (Speed, MotionSpeed, Grounded, Jump, FreeFall), so ThirdPersonController drives it unchanged.
    /// </summary>
    public static class LightkeeperBuilder
    {
        const string MaterialDir = "Assets/Art/Materials/Character";
        const string BodyMaterialPath = MaterialDir + "/M_Lightkeeper.mat";
        const string GlowMaterialPath = MaterialDir + "/M_LanternGlow.mat";
        const string ControllerPath = LightkeeperPostprocessor.Folder + "AC_Lightkeeper.controller";
        const string PrefabDir = "Assets/Prefabs/Characters";
        const string PrefabPath = PrefabDir + "/Lightkeeper.prefab";
        const string LitShader = "SunSwitch/Storybook";

        // Where the lantern hangs, relative to the chest bone (metres, after import scale).
        const float LanternLift = 0.1f;      // above the chest bone
        const float LanternGap = 0.1f;       // behind the cape

        // Starter Assets ThirdPersonController defaults: MoveSpeed 2, SprintSpeed 5.335.
        const float WalkSpeed = 2f;
        const float RunSpeed = 5.335f;

        [MenuItem("Sun Switch/Art/Build Lightkeeper")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(LightkeeperPostprocessor.CharacterPath);
            var lanternModel = AssetDatabase.LoadAssetAtPath<GameObject>(LightkeeperPostprocessor.LanternPath);
            if (model == null || lanternModel == null)
            {
                Debug.LogError("[Sun Switch] Lightkeeper or lantern model not imported yet.");
                return;
            }

            EnsureFolder(MaterialDir);
            EnsureFolder(PrefabDir);
            var body = Material(BodyMaterialPath, Color.white, 0.012f);
            var glow = Material(GlowMaterialPath, Color.white, 0.012f);
            glow.SetFloat("_AmbientStrength", 0.6f);
            glow.SetFloat("_ShadowThreshold", -1f);   // the glass never falls into painted shadow
            glow.SetFloat("_BlotchStrength", 0f);
            EditorUtility.SetDirty(glow);
            var controller = Controller();

            var root = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                root.name = "Lightkeeper";
                var animator = root.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                // Body parts are skinned; the cape is a plain mesh riding on the chest bone.
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterials = Enumerable.Repeat(body, Mathf.Max(1, MeshOf(r).subMeshCount)).ToArray();

                AddLantern(root, animator, lanternModel, body, glow);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[Sun Switch] Built {PrefabPath}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }

        static void AddLantern(GameObject root, Animator animator, GameObject lanternModel, Material body, Material glow)
        {
            // The cape hangs from the chest bone, so the lantern does too.
            var chest = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "chest")
                        ?? animator.GetBoneTransform(HumanBodyBones.Chest)
                        ?? animator.GetBoneTransform(HumanBodyBones.Spine);
            float hangY = chest.position.y + LanternLift;

            // "Back" is wherever the cape is (the model's own facing differs from the GameObject's).
            var cape = root.GetComponentsInChildren<Renderer>(true).First(r => r.name.Contains("Cape"));
            var capeVerts = MeshOf(cape).vertices.Select(v => cape.transform.TransformPoint(v)).ToArray();
            var capeCentre = capeVerts.Aggregate(Vector3.zero, (a, v) => a + v) / capeVerts.Length;
            var back = Vector3.ProjectOnPlane(capeCentre - chest.position, Vector3.up).normalized;
            float backDistance = capeVerts
                .Where(v => Mathf.Abs(v.y - hangY) < 0.15f)
                .Select(v => Vector3.Dot(v - chest.position, back))
                .DefaultIfEmpty(0f).Max();

            var lantern = (GameObject)PrefabUtility.InstantiatePrefab(lanternModel);
            lantern.name = "Lantern";
            lantern.transform.SetParent(chest, false);
            var hang = chest.position + back * (backDistance + LanternGap);
            lantern.transform.position = new Vector3(hang.x, hangY, hang.z);
            lantern.transform.rotation = Quaternion.LookRotation(back, Vector3.up);

            Renderer glass = null;
            foreach (var r in lantern.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool isGlass = r.name.Contains("Glow");
                r.sharedMaterial = isGlass ? glow : body;
                r.shadowCastingMode = isGlass ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                if (isGlass) glass = r;
            }

            var lightGo = new GameObject("Lantern_Light");
            lightGo.transform.SetParent(lantern.transform, false);
            lightGo.transform.position = glass != null ? glass.bounds.center : lantern.transform.position;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3.5f;
            light.shadows = LightShadows.None;

            var glowComp = lantern.AddComponent<LanternGlow>();
            var so = new SerializedObject(glowComp);
            so.FindProperty("glass").objectReferenceValue = glass;
            so.FindProperty("glowLight").objectReferenceValue = light;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Mesh MeshOf(Renderer r) =>
            r is SkinnedMeshRenderer s ? s.sharedMesh : r.GetComponent<MeshFilter>().sharedMesh;

        static Material Material(string path, Color color, float inkWidth)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(LitShader));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find(LitShader);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_OutlineWidth", inkWidth);
            mat.SetFloat("_EdgeInkWidth", inkWidth);
            mat.SetFloat("_NoiseScale", 3f);        // finer paper grain on a small character
            mat.SetFloat("_BlotchStrength", 0.1f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static AnimatorController Controller()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) return existing;

            var clips = AssetDatabase.LoadAllAssetsAtPath(LightkeeperPostprocessor.CharacterPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToDictionary(c => c.name);
            AnimationClip Clip(string name)
            {
                if (!clips.TryGetValue(name, out var c)) Debug.LogError($"[Sun Switch] Lightkeeper clip {name} missing.");
                return c;
            }

            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
            ac.parameters = ac.parameters.Select(p =>
            {
                if (p.name == "MotionSpeed") p.defaultFloat = 1f;
                return p;
            }).ToArray();
            ac.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Jump", AnimatorControllerParameterType.Bool);
            ac.AddParameter("FreeFall", AnimatorControllerParameterType.Bool);

            var sm = ac.layers[0].stateMachine;

            var tree = new BlendTree
            {
                name = "Idle Walk Run",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(tree, ac);
            tree.AddChild(Clip("Idle"), 0f);
            tree.AddChild(Clip("Walking_A"), WalkSpeed);
            tree.AddChild(Clip("Running_A"), RunSpeed);

            var move = sm.AddState("Idle Walk Run Blend", new Vector3(300, 0));
            move.motion = tree;
            move.speedParameterActive = true;
            move.speedParameter = "MotionSpeed";
            sm.defaultState = move;

            var jumpStart = sm.AddState("JumpStart", new Vector3(560, -80));
            jumpStart.motion = Clip("Jump_Start");
            var inAir = sm.AddState("InAir", new Vector3(820, 0));
            inAir.motion = Clip("Jump_Idle");
            var land = sm.AddState("JumpLand", new Vector3(560, 80));
            land.motion = Clip("Jump_Land");

            var t = move.AddTransition(jumpStart);
            t.hasExitTime = false; t.duration = 0.1f;
            t.AddCondition(AnimatorConditionMode.If, 0, "Jump");

            t = move.AddTransition(inAir);
            t.hasExitTime = false; t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.If, 0, "FreeFall");

            t = jumpStart.AddTransition(inAir);
            t.hasExitTime = true; t.exitTime = 0.8f; t.duration = 0.1f;

            t = jumpStart.AddTransition(land);
            t.hasExitTime = false; t.duration = 0.1f;
            t.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "Jump");

            t = inAir.AddTransition(land);
            t.hasExitTime = false; t.duration = 0.1f;
            t.AddCondition(AnimatorConditionMode.If, 0, "Grounded");

            t = land.AddTransition(move);
            t.hasExitTime = true; t.exitTime = 0.5f; t.duration = 0.2f;

            EditorUtility.SetDirty(ac);
            return ac;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
