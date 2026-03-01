using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace Core
{
    public enum FlowState
    {
        Credits,
        Title,
        Menu,
        Countdown,
        Game,
        Standings,
        Wheel,
        Shop,
        Overs
    }

    public class SceneFlowManager : PersistentSingleton<SceneFlowManager>
    {
        public static SceneFlowManager I => Instance;

        [Header("Scene Names")]
        [SerializeField]
        string creditsScene = "Credits";

        [SerializeField]
        string titleScene = "Title";

        [SerializeField]
        string menuScene = "Menu";

        [SerializeField]
        string countdownScene = "Countdown";

        [SerializeField]
        string gameScene = "Game";

        [SerializeField]
        string standingsScene = "Standings";

        [SerializeField]
        string wheelScene = "Wheel";

        [SerializeField]
        string shopScene = "Shop";

        [SerializeField]
        string oversScene = "Overs";

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

        SceneNamesConfig GetConfig() =>
            new SceneNamesConfig
            {
                Credits = creditsScene,
                Title = titleScene,
                Menu = menuScene,
                Countdown = countdownScene,
                Game = gameScene,
                Standings = standingsScene,
                Wheel = wheelScene,
                Shop = shopScene,
                Overs = oversScene
            };

        string SceneFor(FlowState s) => SceneFlowMapper.SceneFor(s, GetConfig());

        public FlowState StateForSceneName(string n) =>
            SceneFlowMapper.StateForSceneName(n, GetConfig());

        public void GoToOvers()
        {
            GoTo(FlowState.Overs);
        }
    }
}
