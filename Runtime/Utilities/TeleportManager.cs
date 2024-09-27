using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
namespace _3Dimensions.FishNet_XR.Runtime.Utilities
{
    public class TeleportManager : MonoBehaviour
    {
        public UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportationProvider;
        public Color teleportFadeColor = Color.black;
        public int fadePriority = 10;
        public float fadeSceneLoadDelayTime = 1;

        private bool _teleporting;
        private bool sceneLoading;
        private float _elapsedSceneLoadTime;

        private bool CanFade => !sceneLoading && _elapsedSceneLoadTime > fadeSceneLoadDelayTime;
        
        private void Start()
        {
            sceneLoading = false;
            InstanceFinder.SceneManager.OnLoadEnd += OnLoadEnd;
            InstanceFinder.SceneManager.OnLoadStart += OnLoadStart;
        }
        private void OnLoadStart(SceneLoadStartEventArgs obj)
        {
            Debug.Log("OnLoadStart", this);
            _elapsedSceneLoadTime = 0;
            sceneLoading = true;
        }

        private void OnLoadEnd(SceneLoadEndEventArgs sceneLoadEndEventArgs)
        {
            Debug.Log("OnLoadEnd", this);
            _elapsedSceneLoadTime = 0;
            sceneLoading = false;
        }
        
        private void LateUpdate()
        {
            if (!sceneLoading) _elapsedSceneLoadTime += Time.deltaTime;
            
            //Screen fade when teleporting
            if (teleportationProvider.locomotionPhase == LocomotionPhase.Started && !_teleporting)
            {
                _teleporting = true;
                if (CanFade) ScreenFade.Instance.FadeOut(new ScreenFade.FadeParameters(teleportationProvider.delayTime, teleportFadeColor, fadePriority));
            }

            if (teleportationProvider.locomotionPhase == LocomotionPhase.Idle && _teleporting)
            {
                _teleporting = false;
                if (CanFade) ScreenFade.Instance.FadeIn(new ScreenFade.FadeParameters(teleportationProvider.delayTime, teleportFadeColor, fadePriority));
            }
        }
    }
}
