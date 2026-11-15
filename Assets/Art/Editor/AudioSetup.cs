using System;
using System.Collections.Generic;
using SunSwitch.LookAndFeel;
using SunSwitch.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Sun Switch > Audio > Add Audio To Open Scene
    /// Sets up AudioManager, MusicPlayer, and connects character footsteps and landing audio.
    /// Also saves Assets/Prefabs/Audio/AudioSystem.prefab for reuse in any scene.
    /// </summary>
    public static class AudioSetup
    {
        const string SfxDir = "Assets/Art/Audio/SFX";
        const string MusicDir = "Assets/Art/Audio/Music";
        const string PrefabDir = "Assets/Prefabs/Audio";
        const string PrefabPath = PrefabDir + "/AudioSystem.prefab";

        [MenuItem("Sun Switch/Audio/Add Audio To Open Scene")]
        public static void AddToOpenScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            SetUpAudio(scene);
            SetUpPlayerAudio(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Sun Switch] Audio system configured in {scene.name}.");
        }

        public static GameObject SetUpAudio(UnityEngine.SceneManagement.Scene scene)
        {
            var existingManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
            GameObject audioObj = existingManager != null ? existingManager.gameObject : null;

            if (audioObj == null)
            {
                var existingMusic = UnityEngine.Object.FindFirstObjectByType<MusicPlayer>(FindObjectsInactive.Include);
                if (existingMusic != null) audioObj = existingMusic.gameObject;
            }

            if (audioObj == null)
            {
                audioObj = new GameObject("AudioSystem");
                Undo.RegisterCreatedObjectUndo(audioObj, "Create AudioSystem");
                if (scene.IsValid() && audioObj.scene != scene)
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(audioObj, scene);
            }

            // Setup AudioManager
            var audioManager = audioObj.GetComponent<AudioManager>();
            if (audioManager == null) audioManager = Undo.AddComponent<AudioManager>(audioObj);

            ConfigureAudioManager(audioManager);

            // Setup MusicPlayer
            var musicPlayer = audioObj.GetComponent<MusicPlayer>();
            if (musicPlayer == null) musicPlayer = Undo.AddComponent<MusicPlayer>(audioObj);

            ConfigureMusicPlayer(musicPlayer);

            // Save / update prefab
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabDir)) AssetDatabase.CreateFolder("Assets/Prefabs", "Audio");
            PrefabUtility.SaveAsPrefabAssetAndConnect(audioObj, PrefabPath, InteractionMode.AutomatedAction);

            EditorUtility.SetDirty(audioObj);
            return audioObj;
        }

        static void ConfigureAudioManager(AudioManager manager)
        {
            var so = new SerializedObject(manager);
            var soundsProp = so.FindProperty("sounds");

            var sfxList = new List<(SfxId id, string file, float volume)>
            {
                (SfxId.SwitchDay, "SwitchDay.wav", 0.85f),
                (SfxId.SwitchNight, "SwitchNight.wav", 0.85f),
                (SfxId.SwitchBlocked, "SwitchBlocked.wav", 0.75f),
                (SfxId.Jump, "Jump.wav", 0.50f),
                (SfxId.JumpPad, "JumpPad.wav", 0.85f),
                (SfxId.Death, "Death.wav", 0.80f),
                (SfxId.Checkpoint, "Checkpoint.wav", 0.85f),
                (SfxId.Shard, "Shard.wav", 0.75f),
                (SfxId.UIClick, "UIClick.wav", 0.65f),
                (SfxId.Win, "Win.wav", 0.90f),
                (SfxId.GameOver, "GameOver.wav", 0.85f)
            };

            soundsProp.arraySize = sfxList.Count;
            for (int i = 0; i < sfxList.Count; i++)
            {
                var entry = soundsProp.GetArrayElementAtIndex(i);
                var (id, file, vol) = sfxList[i];
                entry.FindPropertyRelative("id").enumValueIndex = (int)id;
                entry.FindPropertyRelative("volume").floatValue = vol;

                var clipsProp = entry.FindPropertyRelative("clips");
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxDir}/{file}");
                if (clip != null)
                {
                    clipsProp.arraySize = 1;
                    clipsProp.GetArrayElementAtIndex(0).objectReferenceValue = clip;
                }
            }

            so.ApplyModifiedProperties();
        }

        static void ConfigureMusicPlayer(MusicPlayer player)
        {
            var so = new SerializedObject(player);
            var dayClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{MusicDir}/Music_Day.wav");
            var nightClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{MusicDir}/Music_Night.wav");
            var menuClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{MusicDir}/Music_Menu.wav");

            if (dayClip != null) so.FindProperty("dayLayer").objectReferenceValue = dayClip;
            if (nightClip != null) so.FindProperty("nightLayer").objectReferenceValue = nightClip;
            if (menuClip != null) so.FindProperty("menuTrack").objectReferenceValue = menuClip;
            so.FindProperty("volume").floatValue = 0.5f;
            so.FindProperty("fadeTime").floatValue = 0.5f;

            so.ApplyModifiedProperties();
        }

        public static void SetUpPlayerAudio(UnityEngine.SceneManagement.Scene scene)
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<LightkeeperController>(FindObjectsInactive.Include);
            if (controller == null) return;

            var so = new SerializedObject(controller);
            var footstepsProp = so.FindProperty("FootstepAudioClips");
            var landProp = so.FindProperty("LandingAudioClip");

            var landClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Land.wav");
            if (landClip != null) landProp.objectReferenceValue = landClip;

            var footsteps = new List<AudioClip>();
            for (int i = 1; i <= 10; i++)
            {
                string pad = i < 10 ? $"0{i}" : $"{i}";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Footstep_{pad}.wav");
                if (clip != null) footsteps.Add(clip);
            }

            if (footsteps.Count > 0)
            {
                footstepsProp.arraySize = footsteps.Count;
                for (int i = 0; i < footsteps.Count; i++)
                    footstepsProp.GetArrayElementAtIndex(i).objectReferenceValue = footsteps[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }
    }
}
