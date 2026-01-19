using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.Shop
{
    public class ShopController : MonoBehaviour
    {
        [System.Serializable]
        public class ShopItem
        {
            public ShopItemType type;
            public string name;
            public int cost;
            public Text pointerText; // purple arrow
            public Text labelText;   // "Speed Boost"
            public Text costText;    // "x3"
        }
    
        public ShopItem[] items;

        private int selectedIndex = 0;
        private int playerCount;
        private int currentPlayer = 1; // 1-based index

        [Header("UI References")]
        public Transform coinContainer;
        public Sprite coinSprite;
        public Text headingText;   // <-- add this
    
        private void Start()
        {
            playerCount = PlayerPrefs.GetInt("Players", 2);
            currentPlayer = 1; // start with Player 1
            // 🔹 Initialise upgrades for all players (coins are left alone)
            SessionManager.Instance.Initialize(playerCount);
            
            UpdateMenuText();
            UpdatePointers();
            RefreshCoinsDisplay();
            UpdateHeading(); // <--
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                selectedIndex = (selectedIndex - 1 + items.Length) % items.Length;
                UpdatePointers();
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                selectedIndex = (selectedIndex + 1) % items.Length;
                UpdatePointers();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                AttemptPurchase(selectedIndex);
                RefreshCoinsDisplay();
            }
        }
    
        private void InitialisePlayerPrefs(int playerCount)
        {
            for (int playerId = 1; playerId <= playerCount; playerId++)
            {
                foreach (ShopItemType type in System.Enum.GetValues(typeof(ShopItemType)))
                {
                    if (type == ShopItemType.Exit)
                        continue; // no prefs needed
                    
                    string key = $"Player{playerId}_{type}";
                    PlayerPrefs.SetInt(key, 0);
                }
            }

            PlayerPrefs.Save();
            Debug.Log("[ShopController] PlayerPrefs initialised for all players (coins unchanged).");
        }
        
        void UpdateMenuText()
        {
            foreach (var item in items)
            {
                item.labelText.text = item.name;

                if (item.costText != null)   // only update cost if it exists
                    item.costText.text = $"{item.cost}";
                else
                    item.labelText.text = item.name; // just show "Exit"
            }
        }

        void UpdatePointers()
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i].pointerText.text = (i == selectedIndex) ? "> " : "  ";
            }
        }

        void UpdateHeading()
        {
            if (headingText != null)
            {
                headingText.text = $"PLAYER {currentPlayer} ENTERS SHOP";
            }
        }

        void RefreshCoinsDisplay()
        {
            // Clear old coins
            foreach (Transform child in coinContainer)
                Destroy(child.gameObject);

            int playerId = currentPlayer;
            int coins = PlayerPrefs.GetInt($"Player{playerId}_Coins", 0);

            for (int i = 0; i < coins; i++)
            {
                var coinGO = new GameObject($"Coin{i}");
                var img = coinGO.AddComponent<Image>();
                img.sprite = coinSprite;
                coinGO.transform.SetParent(coinContainer, false);
            }
        }

        void AttemptPurchase(int index)
        {
            var item = items[index];

            // Exit option: go to next player
            if (item.name == "EXIT")
            {
                if (currentPlayer < playerCount)
                {
                    currentPlayer++;
                    Debug.Log($"Next shop turn: Player {currentPlayer}");
                    // Reset selection to top
                    selectedIndex = 0;
                    UpdatePointers();
                    RefreshCoinsDisplay();
                    UpdateHeading(); // <--
                }
                else
                {
                    // Last player done → leave shop
                    SceneFlowManager.I.SignalScreenDone();
                }
                return;
            }

            // Normal purchase flow
            int playerId = currentPlayer;
            int coins = PlayerPrefs.GetInt($"Player{playerId}_Coins", 0);

            if (coins >= item.cost)
            {
                coins -= item.cost;
                PlayerPrefs.SetInt($"Player{playerId}_Coins", coins);
                AudioController.I.PlayBuy();

                ApplyUpgrade(playerId, item.type);

                PlayerPrefs.Save();
                Debug.Log($"Player {playerId} bought {item.name}!");
            }
            else
            {
                AudioController.I.PlayNoBuy();
                Debug.Log($"Player {playerId} cannot afford {item.name}!");
            }
        }

        void ApplyUpgrade(int playerId, ShopItemType type)
        {
            string key = $"Player{playerId}_{type}"; // e.g. "Player1_SpeedUp"

            switch (type)
            {
                // 🔁 Stackable items
                case ShopItemType.ExtraBomb:
                case ShopItemType.PowerUp:
                case ShopItemType.SpeedUp:
                    PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);
                    break;

                // ✅ Toggle / single-use states
                case ShopItemType.Superman:
                case ShopItemType.Ghost:
                case ShopItemType.Protection:
                case ShopItemType.Controller:
                case ShopItemType.Timebomb:
                    PlayerPrefs.SetInt(key, 1);
                    break;

                // 🚪 Exit → nothing to save
                case ShopItemType.Exit:
                    break;
            }
        }
    }
}
