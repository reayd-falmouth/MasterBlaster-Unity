using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace Core
{
    public enum FlowState { Credits, Title, Menu, Countdown, Game, Standings, Wheel, Shop, Overs }

    public class SceneFlowManager : PersistentSingleton<SceneFlowManager>
    {
        public static SceneFlowManager I => Instance;

        [Header("Scene Names")]
        [SerializeField] string creditsScene   = "Credits";
        [SerializeField] string titleScene     = "Title";
        [SerializeField] string menuScene      = "Menu";    
        [SerializeField] string countdownScene = "Countdown";
        [SerializeField] string gameScene      = "Game";
        [SerializeField] string standingsScene = "Standings";
        [SerializeField] string wheelScene     = "Wheel";
        [SerializeField] string shopScene      = "Shop";
        [SerializeField] string oversScene      = "Overs";
    
        FlowState state;

        void Start()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            state = StateForSceneName(sceneName);
            Debug.Log($"[Flow] Booted in '{sceneName}' → {state}");
        }

        // -------- Public signals from scenes --------
        public void SignalScreenDone()
        {
            switch (state)
            {
                case FlowState.Credits:
                    GoTo(FlowState.Title); 
                    break;

                case FlowState.Title:
                    GoTo(FlowState.Menu);  
                    break;

                case FlowState.Menu:
                    SignalMenuStart();         
                    break;

                case FlowState.Countdown:
                    GoTo(FlowState.Game);  
                    break;

                case FlowState.Standings:
                    // If gambling enabled → Wheel first
                    if (PlayerPrefs.GetInt("Gambling", 1) == 1)
                        GoTo(FlowState.Wheel);
                    else if (PlayerPrefs.GetInt("Shop", 1) == 1)
                        GoTo(FlowState.Shop);
                    else
                        GoTo(FlowState.Game);
                    break;

                case FlowState.Wheel:
                    // After wheel → shop if enabled, otherwise straight to game
                    if (PlayerPrefs.GetInt("Shop", 1) == 1)
                        GoTo(FlowState.Shop);
                    else
                        GoTo(FlowState.Game);
                    break;

                case FlowState.Shop:
                    GoTo(FlowState.Countdown);
                    break;
            }
        }

        public void SignalMenuStart()
        {
            GoTo(FlowState.Countdown);
        }

        public void SignalRoundFinished()
        {
            GoTo(FlowState.Standings);
        }

        // -------- Core --------
        public void GoTo(FlowState next)
        {
            Debug.Log($"[Flow] {state} → {next}");
            state = next;
            SceneManager.LoadScene(SceneFor(next), LoadSceneMode.Single);
        }

        string SceneFor(FlowState s) => s switch
        {
            FlowState.Credits    => creditsScene,
            FlowState.Title      => titleScene,
            FlowState.Menu       => menuScene,
            FlowState.Countdown  => countdownScene,
            FlowState.Game       => gameScene,
            FlowState.Standings  => standingsScene,
            FlowState.Wheel      => wheelScene,
            FlowState.Shop       => shopScene,
            FlowState.Overs      => oversScene,
            _ => menuScene
        };

        public FlowState StateForSceneName(string n)
        {
            if (n == creditsScene)   return FlowState.Credits;
            if (n == titleScene)     return FlowState.Title;
            if (n == menuScene)      return FlowState.Menu;
            if (n == countdownScene) return FlowState.Countdown;
            if (n == gameScene)      return FlowState.Game;
            if (n == standingsScene) return FlowState.Standings;
            if (n == wheelScene)     return FlowState.Wheel;
            if (n == shopScene)      return FlowState.Shop;
            if (n == oversScene)      return FlowState.Overs;
            return FlowState.Menu;
        }
    
        public void GoToOvers()
        {
            GoTo(FlowState.Overs);
        }

    }
}