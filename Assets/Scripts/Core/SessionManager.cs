using System.Collections.Generic;
using Scenes.Shop;
using Utilities;


namespace Core
{
    public class SessionManager:  PersistentSingleton<SessionManager>
    {
        // Key: Player ID (int)
        // Value: Dictionary of Upgrade Type (ShopItemType) and its Level/State (int)
        public Dictionary<int, Dictionary<ShopItemType, int>> PlayerUpgrades = 
            new Dictionary<int, Dictionary<ShopItemType, int>>();
        
        // 3. Setup/Cleanup Method
        public void Initialize(int playerCount)
        {
            PlayerUpgrades.Clear();
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
            }
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