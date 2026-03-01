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
            public Text labelText; // "Speed Boost"
            public Text costText; // "x3"
        }

        public ShopItem[] items;

        private int selectedIndex = 0;
        private int playerCount;
        private int currentPlayer = 1; // 1-based index

        [Header("UI References")]
        public Transform coinContainer;
        public Sprite coinSprite;
        public Text headingText; // <-- add this

        /// <summary>
        /// Returns the pointer text for an item at the given index ("> " if selected, "  " otherwise).
        /// Used by UpdatePointers and by tests.
        /// </summary>
        public static string GetPointerTextForIndex(int index, int selectedIndex)
        {
            return index == selectedIndex ? "> " : "  ";
        }

        /// <summary>
        /// Returns the coin count to display for a player (from SessionManager). Used by RefreshCoinsDisplay and by tests.
        /// </summary>
        public static int GetCoinsToDisplayForPlayer(int playerId)
        {
            return SessionManager.Instance != null ? SessionManager.Instance.GetCoins(playerId) : 0;
        }

        private void Awake()
        {
            if (items != null && items.Length > 0)
                UpdatePointers();
        }

        private void Start()
        {
            playerCount = PlayerPrefs.GetInt("Players", 2);
            currentPlayer = 1; // start with Player 1
            // Only initialize SessionManager if not yet set (e.g. first time); do not wipe state between rounds
            if (
                SessionManager.Instance.PlayerUpgrades == null
                || SessionManager.Instance.PlayerUpgrades.Count == 0
                || !SessionManager.Instance.PlayerUpgrades.ContainsKey(1)
            )
            {
                SessionManager.Instance.Initialize(playerCount);
            }

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
            Debug.Log(
                "[ShopController] PlayerPrefs initialised for all players (coins unchanged)."
            );
        }

        void UpdateMenuText()
        {
            foreach (var item in items)
            {
                item.labelText.text = item.name;

                if (item.costText != null) // only update cost if it exists
                    item.costText.text = $"{item.cost}";
                else
                    item.labelText.text = item.name; // just show "Exit"
            }
        }

        void UpdatePointers()
        {
            if (items == null)
                return;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].pointerText != null)
                    items[i].pointerText.text = GetPointerTextForIndex(i, selectedIndex);
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
            if (coinContainer == null)
                return;

            // Clear old coins immediately so scene placeholders don't stick (Destroy is deferred)
            for (int i = coinContainer.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(coinContainer.GetChild(i).gameObject);

            int playerId = currentPlayer;
            int coins = GetCoinsToDisplayForPlayer(playerId);

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

            // Normal purchase flow (coins in SessionManager)
            int playerId = currentPlayer;
            int coins =
                SessionManager.Instance != null ? SessionManager.Instance.GetCoins(playerId) : 0;

            if (ShopPurchaseLogic.CanAfford(coins, item.cost))
            {
                coins -= item.cost;
                if (SessionManager.Instance != null)
                    SessionManager.Instance.SetCoins(playerId, coins);
                AudioController.I.PlayBuy();

                ApplyUpgrade(playerId, item.type);

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
            if (type == ShopItemType.Exit)
                return;

            int currentLevel = SessionManager.Instance.GetUpgradeLevel(playerId, type);
            int newLevel = ShopPurchaseLogic.GetNewLevelAfterPurchase(type, currentLevel);
            SessionManager.Instance.SetUpgradeLevel(playerId, type, newLevel);
        }
    }
}
