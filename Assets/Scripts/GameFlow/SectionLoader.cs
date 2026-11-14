using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SunSwitch.GameFlow
{
    /// <summary>
    /// Loads the tower sections into Main additively on start. GameManager waits for IsReady.
    /// Sections already open in the editor are skipped, so you can play with them open.
    /// </summary>
    public class SectionLoader : MonoBehaviour
    {
        [SerializeField] string[] sceneNames = { "Section1", "Section2", "Section3", "Section4" };

        public bool IsReady { get; private set; }

        IEnumerator Start()
        {
            var loading = new List<AsyncOperation>();
            foreach (var sceneName in sceneNames)
            {
                if (string.IsNullOrEmpty(sceneName) || SceneManager.GetSceneByName(sceneName).isLoaded) continue;
                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    Debug.LogError($"SectionLoader: scene '{sceneName}' is not in Build Settings.", this);
                    continue;
                }
                loading.Add(SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive));
            }

            foreach (var op in loading)
                while (!op.isDone) yield return null;

            IsReady = true;
        }
    }
}
