using System.Collections.Generic;
using Scenes.Shop;
using Utilities;

namespace Core
{
    public class SessionManager : PersistentSingleton<SessionManager>
    {
        // Key: Player ID (int)
        // Value: Dictionary of Upgrade Type (ShopItemType) and its Level/State (int)
        public Dictionary<int, Dictionary<ShopItemType, int>> PlayerUpgrades =
            new Dictionary<int, Dictionary<ShopItemType, int>>();

        /// <summary>Session-only coin count per player (not in PlayerPrefs).</summary>
        public Dictionary<int, int> PlayerCoins = new Dictionary<int, int>();

        // 3. Setup/Cleanup Method
        public void Initialize(int playerCount)
        {
            PlayerUpgrades.Clear();
            PlayerCoins.Clear();
            for (int id = 1; id <= playerCount; id++)
            {
                // Initialize each player with a dictionary to store their upgrades
                PlayerUpgrades[id] = new Dictionary<ShopItemType, int>();

                // Set all upgrades to 0 initially
                foreach (ShopItemType type in System.Enum.GetValues(typeof(ShopItemType)))
                {
                    if (type != ShopItemType.Exit)
                        PlayerUpgrades[id][type] = 0;
                }
                PlayerCoins[id] = 0;
            }
        }

        public int GetCoins(int playerId)
        {
            return PlayerCoins.TryGetValue(playerId, out int c) ? c : 0;
        }

        public void SetCoins(int playerId, int value)
        {
            if (PlayerCoins.ContainsKey(playerId))
                PlayerCoins[playerId] = value;
        }

        public void AddCoins(int playerId, int amount)
        {
            if (PlayerCoins.ContainsKey(playerId))
                PlayerCoins[playerId] += amount;
        }

        // 4. Accessor/Mutator Method
        public int GetUpgradeLevel(int playerId, ShopItemType type)
        {
            if (PlayerUpgrades.ContainsKey(playerId) && PlayerUpgrades[playerId].ContainsKey(type))
            {
                return PlayerUpgrades[playerId][type];
            }
            return 0; // Default to 0 if not found
        }

        public void SetUpgradeLevel(int playerId, ShopItemType type, int level)
        {
            if (PlayerUpgrades.ContainsKey(playerId))
            {
                PlayerUpgrades[playerId][type] = level;
            }
        }
    }
}
