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

        /// <summary>
        /// Current flow state (used by ContinueOnAnyInput to decide if any key should advance).
        /// </summary>
        public FlowState CurrentState => state;

        /// <summary>
        /// True only for screens where "continue on any input" should advance (Credits, Title).
        /// Menu and other screens must use their own UI (e.g. Return to start).
        /// </summary>
        public static bool ShouldAdvanceOnAnyInput(FlowState state)
        {
            return state == FlowState.Credits
                || state == FlowState.Title
                || state == FlowState.Overs;
        }

        void Start()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            state = StateForSceneName(sceneName);
            // Handle scene name typo: "TItle" (capital I) is the Title screen
            if (state == FlowState.Menu && sceneName == "TItle")
                state = FlowState.Title;
            Debug.Log($"[Flow] Booted in '{sceneName}' → {state}");
        }

        /// <summary>
        /// Returns the next flow state when the current scene signals "done". Uses PlayerPrefs for Gambling/Shop.
        /// Used by SignalScreenDone and by EditMode tests. Countdown always precedes Game (arena).
        /// </summary>
        public static FlowState GetNextState(FlowState currentState)
        {
            switch (currentState)
            {
                case FlowState.Credits:
                    return FlowState.Title;
                case FlowState.Title:
                    return FlowState.Menu;
                case FlowState.Menu:
                    return FlowState.Countdown;
                case FlowState.Countdown:
                    return FlowState.Game;
                case FlowState.Standings:
                    if (PlayerPrefs.GetInt("Gambling", 1) == 1)
                        return FlowState.Wheel;
                    if (PlayerPrefs.GetInt("Shop", 1) == 1)
                        return FlowState.Shop;
                    return FlowState.Countdown;
                case FlowState.Wheel:
                    if (PlayerPrefs.GetInt("Shop", 1) == 1)
                        return FlowState.Shop;
                    return FlowState.Countdown;
                case FlowState.Shop:
                    return FlowState.Countdown;
                case FlowState.Overs:
                    return FlowState.Menu;
                default:
                    return currentState;
            }
        }

        // -------- Public signals from scenes --------
        public void SignalScreenDone()
        {
            GoTo(GetNextState(state));
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
            string sceneName = SceneFor(next);
            AudioController.I?.PreviewSceneMusic(sceneName);
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
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
