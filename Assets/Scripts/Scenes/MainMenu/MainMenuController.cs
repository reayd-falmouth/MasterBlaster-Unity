using Core;
using Scenes.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [System.Serializable]
        public class MenuOption
        {
            public Text pointerText; // Purple ">" text
            public Text optionLabel; // Left-hand text, e.g. "WINS NEEDED"
            public Text valueLabel; // Right-hand text, e.g. "3"
        }

        public MenuOption[] options;

        private int selectedIndex = 0;

        // Default values
        private int winsNeeded = 3;
        private int players = 2;
        private bool shop = true;
        private bool shrinking = true;
        private bool fastIgnition = true;
        private bool startMoney = false;
        private bool normalLevel = true;
        private bool gambling = true;

        private void Start()
        {
            LoadPrefs();
            UpdateMenuText();
            UpdatePointers();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                selectedIndex = (selectedIndex - 1 + options.Length) % options.Length;
                UpdatePointers();
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                selectedIndex = (selectedIndex + 1) % options.Length;
                UpdatePointers();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                ChangeOption();
                UpdateMenuText();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                SavePrefs();
                SceneFlowManager.I.SignalMenuStart();
            }
        }

        void ChangeOption()
        {
            switch (selectedIndex)
            {
                case 0:
                    winsNeeded = Mathf.Clamp(
                        winsNeeded + (Input.GetKeyDown(KeyCode.RightArrow) ? 1 : -1),
                        3,
                        9
                    );
                    break;
                case 1:
                    players = Mathf.Clamp(
                        players + (Input.GetKeyDown(KeyCode.RightArrow) ? 1 : -1),
                        2,
                        5
                    );
                    break;
                case 2:
                    shop = !shop;
                    break;
                case 3:
                    shrinking = !shrinking;
                    break;
                case 4:
                    fastIgnition = !fastIgnition;
                    break;
                case 5:
                    startMoney = !startMoney;
                    break;
                case 6:
                    normalLevel = !normalLevel;
                    break;
                case 7:
                    gambling = !gambling;
                    break;
            }
        }

        void UpdateMenuText()
        {
            options[0].valueLabel.text = winsNeeded.ToString();
            options[1].valueLabel.text = players.ToString();
            options[2].valueLabel.text = shop ? "ON" : "OFF";
            options[3].valueLabel.text = shrinking ? "ON" : "OFF";
            options[4].valueLabel.text = fastIgnition ? "ON" : "OFF";
            options[5].valueLabel.text = startMoney ? "ON" : "OFF";
            options[6].valueLabel.text = normalLevel ? "YES" : "NO";
            options[7].valueLabel.text = gambling ? "YES" : "NO";
        }

        void UpdatePointers()
        {
            for (int i = 0; i < options.Length; i++)
            {
                options[i].pointerText.text = (i == selectedIndex) ? "> " : "  ";
            }
        }

        void SavePrefs()
        {
            PlayerPrefs.SetInt("WinsNeeded", winsNeeded);
            PlayerPrefs.SetInt("Players", players);
            PlayerPrefs.SetInt("Shop", shop ? 1 : 0);
            PlayerPrefs.SetInt("Shrinking", shrinking ? 1 : 0);
            PlayerPrefs.SetInt("FastIgnition", fastIgnition ? 1 : 0);
            PlayerPrefs.SetInt("StartMoney", startMoney ? 1 : 0);
            PlayerPrefs.SetInt("NormalLevel", normalLevel ? 1 : 0);
            PlayerPrefs.SetInt("Gambling", gambling ? 1 : 0);

            // Reset in-memory session state for new game (wins, coins, upgrades in SessionManager)
            // SessionManager may not exist in the Menu scene; it is created in Shop/Game. Guard so we don't throw and block SignalMenuStart().
            if (SessionManager.Instance != null)
                SessionManager.Instance.Initialize(players);

            // Only the first arena load (from menu) should give start money; Shop → Countdown → Game should not.
            PlayerPrefs.SetInt("GiveStartMoneyNextArena", 1);

            PlayerPrefs.Save();
        }

        void LoadPrefs()
        {
            winsNeeded = PlayerPrefs.GetInt("WinsNeeded", 3);
            players = PlayerPrefs.GetInt("Players", 2);
            shop = PlayerPrefs.GetInt("Shop", 1) == 1;
            shrinking = PlayerPrefs.GetInt("Shrinking", 1) == 1;
            fastIgnition = PlayerPrefs.GetInt("FastIgnition", 1) == 1;
            startMoney = PlayerPrefs.GetInt("StartMoney", 0) == 1;
            normalLevel = PlayerPrefs.GetInt("NormalLevel", 1) == 1;
            gambling = PlayerPrefs.GetInt("Gambling", 1) == 1;
        }
    }
}
