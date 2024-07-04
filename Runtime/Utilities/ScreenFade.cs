using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _3Dimensions.FishNet_XR.Runtime.Utilities
{
    // This class is used to fade the entire screen to black (or
    // any chosen colour).  It should be used to smooth out the
    // transition between scenes or restarting of a scene.
    public class ScreenFade : MonoBehaviour
    {
        [Serializable]
        public struct FadeParameters
        {
            public int Priority;
            public float Duration;
            public Color ColorFaded;
            public Color ColorNotFaded;
            [HideInInspector] public bool Completed;

            public FadeParameters(float duration, Color colorFaded, int priority)
            {
                Priority = priority;
                Duration = duration;
                ColorFaded = colorFaded;
                ColorNotFaded = new Color(colorFaded.r, colorFaded.g, colorFaded.b, 0);
                Completed = false;
            }
        }
        
        public static ScreenFade Instance {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ScreenFade>();
                }

                return _instance;
            }
        }
        private static ScreenFade _instance;
        
        [SerializeField] private Material screenFadeMaterial;
        private Material runtimeMaterial;

        [SerializeField] private FadeParameters defaultFade;
        [ShowInInspector] private FadeParameters currentFade;

        [SerializeField] private bool fadeInOnSceneLoad = false;      // Whether a fade in should happen as soon as the scene is loaded.
        [SerializeField] private bool fadeInOnStart = false;          // Whether a fade in should happen just but Updates start.

        private bool _faded;                                          // Fading out (true) of in (false)
        
        private float _timer = 0f;
        private Color _startColor;
        private Color _endColor;

        
        private Renderer _renderer;
        private void Awake()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;

            _renderer = GetComponent<Renderer>();
            runtimeMaterial = new Material(screenFadeMaterial);
            _renderer.material = runtimeMaterial;
            
            currentFade = defaultFade;
            SetupFadeColors();
        }
        
        private void Start()
        {
            // If applicable set the immediate colour to be faded out and then fade in.
            if (fadeInOnStart)
            {
                runtimeMaterial.color = defaultFade.ColorFaded;
                FadeIn();
            }
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Destroy(runtimeMaterial);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            currentFade.Completed = _timer > currentFade.Duration;
            
            if (!currentFade.Completed)
            {
                runtimeMaterial.color = Color.Lerp(_startColor, _endColor, _timer / currentFade.Duration);
            }
            else
            {
                runtimeMaterial.color = _endColor;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            // If applicable set the immediate colour to be faded out and then fade in.
            if (fadeInOnSceneLoad)
            {
                runtimeMaterial.color = currentFade.ColorFaded;
                FadeIn();
            }
        }

        /// <summary>
        /// FadeOut with default FadeParameters
        /// </summary>
        [Button]
        public void FadeOut()
        {
            if (!currentFade.Completed && currentFade.Priority <= defaultFade.Priority)
            {
                Debug.LogWarning("Trying to fade out, but another high priority fade is not yet completed.");
                return;
            }
            
            Debug.Log("Fading out...");
            currentFade = defaultFade;
            _faded = true;
            _timer = 0;
            SetupFadeColors();
        }
        
        /// <summary>
        /// FadeOut with custom FadeParameters
        /// </summary>
        /// <param name="fadeParameters"></param>
        [Button]
        public void FadeOut(FadeParameters fadeParameters)
        {
            if (!currentFade.Completed && currentFade.Priority <= fadeParameters.Priority)
            {
                Debug.LogWarning("Trying to fade out, but another high priority fade is not yet completed.");
                return;
            }
            
            Debug.Log("Fading out with custom parameters...");
            currentFade = fadeParameters;
            _faded = true;
            _timer = 0;
            SetupFadeColors();
        }
        
        /// <summary>
        /// FadeIn with default FadeParameters;
        /// </summary>
        [Button]
        public void FadeIn()
        {
            if (!currentFade.Completed && currentFade.Priority <= defaultFade.Priority)
            {
                Debug.LogWarning("Trying to fade out, but another high priority fade is not yet completed.");
                return;
            }
            
            Debug.Log("Fading in with default parameters...");
            currentFade = defaultFade;
            _faded = false;
            _timer = 0;
            SetupFadeColors();
        }
        
        /// <summary>
        /// FadeIn with custom FadeParameters
        /// </summary>
        /// <param name="fadeParameters"></param>
        [Button]
        public void FadeIn(FadeParameters fadeParameters)
        {
            if (!currentFade.Completed && currentFade.Priority <= fadeParameters.Priority)
            {
                Debug.LogWarning("Trying to fade in, but another high priority fade is not yet completed.");
                return;
            }
            
            Debug.Log("Fading in, duration = " + fadeParameters.Duration + "...");
            currentFade = fadeParameters;
            _faded = false;
            _timer = 0;
            SetupFadeColors();
        }

        private void SetupFadeColors()
        {
            _startColor = _faded ? currentFade.ColorNotFaded : currentFade.ColorFaded;
            _endColor = _faded ? currentFade.ColorFaded : currentFade.ColorNotFaded;
        }
    }
}