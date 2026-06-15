using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Composition root for the Menu scene (the app's entry point). Builds the
    /// audio system (for UI clicks) and the meta-game front-end. PLAY loads the
    /// gameplay Boot scene; the death/win "HOME" buttons return here.
    /// </summary>
    public class MenuBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;

            var systems = new GameObject("MenuSystems");
            systems.AddComponent<GameAudio>().Init();

            MenuController.Create();
        }
    }
}
