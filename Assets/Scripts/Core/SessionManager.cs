using System.Collections.Generic;
using Scenes.Shop;
using UnityEngine;
using UnityEngine.InputSystem;
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

        /// <summary>Session-only win count per player (matches won this session).</summary>
        public Dictionary<int, int> PlayerWins = new Dictionary<int, int>();

        /// <summary>Session-only: player ID who won the match (0 = none). Set when transitioning to Overs, cleared on new game.</summary>
        public int MatchWinnerPlayerId;

        /// <summary>Session-only: display name of match winner. Set when transitioning to Overs, cleared on new game.</summary>
        public string MatchWinnerName;

        /// <summary>Player ID -> device index (1+ = gamepad only). Missing or -1 means AI.</summary>
        private Dictionary<int, int> _playerDeviceIndex = new Dictionary<int, int>();

        // 3. Setup/Cleanup Method
        public void Initialize(int playerCount)
        {
            PlayerUpgrades.Clear();
            PlayerCoins.Clear();
            PlayerWins.Clear();
            MatchWinnerPlayerId = 0;
            MatchWinnerName = null;
            _playerDeviceIndex.Clear();
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
                PlayerWins[id] = 0;
            }
        }

        public int GetCoins(int playerId)
        {
            return PlayerCoins.TryGetValue(playerId, out int c) ? c : 0;
        }

        public void SetCoins(int playerId, int value)
        {
            PlayerCoins[playerId] = value;
        }

        public void AddCoins(int playerId, int amount)
        {
            int current = PlayerCoins.TryGetValue(playerId, out int c) ? c : 0;
            PlayerCoins[playerId] = current + amount;
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
            if (!PlayerUpgrades.ContainsKey(playerId))
                PlayerUpgrades[playerId] = new Dictionary<ShopItemType, int>();
            PlayerUpgrades[playerId][type] = level;
        }

        public int GetWins(int playerId)
        {
            return PlayerWins.TryGetValue(playerId, out int w) ? w : 0;
        }

        public void SetWins(int playerId, int value)
        {
            PlayerWins[playerId] = value;
        }

        public void AddWin(int playerId)
        {
            int current = PlayerWins.TryGetValue(playerId, out int w) ? w : 0;
            PlayerWins[playerId] = current + 1;
        }

        /// <summary>Store the match winner for the Overs screen. Call when transitioning to Overs.</summary>
        public void SetMatchWinner(int playerId, string winnerName)
        {
            MatchWinnerPlayerId = playerId;
            MatchWinnerName = winnerName;
        }

        /// <summary>Returns the stored match winner player ID (0 if none set).</summary>
        public int GetMatchWinnerPlayerId()
        {
            return MatchWinnerPlayerId;
        }

        /// <summary>Returns the stored match winner display name, or "Unknown" if null/empty.</summary>
        public string GetMatchWinnerName()
        {
            return string.IsNullOrEmpty(MatchWinnerName) ? "Unknown" : MatchWinnerName;
        }

        /// <summary>Assign input devices to players. Only controllers (gamepads) are allowed; keyboard is not assigned.
        /// Device 1 = first gamepad, 2 = second, etc. Slots without a controller get AI. Call after Initialize(playerCount).</summary>
        public void AssignInputDevices(int playerCount)
        {
            _playerDeviceIndex.Clear();
            int joystickCount = GetConnectedJoystickCount();
            int nextDevice = 1;

            var allGamepads = Gamepad.all;
            Debug.Log($"[SessionManager] AssignInputDevices — playerCount={playerCount}, gamepads detected={allGamepads.Count}");
            for (int i = 0; i < allGamepads.Count; i++)
                Debug.Log($"  Gamepad[{i}]: {allGamepads[i].displayName} ({allGamepads[i].GetType().Name})");

            for (int id = 1; id <= playerCount; id++)
            {
                if (nextDevice <= joystickCount)
                {
                    _playerDeviceIndex[id] = nextDevice;
                    Debug.Log($"  Player {id} → Gamepad[{nextDevice - 1}]: {allGamepads[nextDevice - 1].displayName}");
                    nextDevice++;
                }
                else
                {
                    _playerDeviceIndex[id] = -1; // AI
                    Debug.Log($"  Player {id} → AI (no gamepad available)");
                }
            }
        }

        /// <summary>Uses the new Input System's Gamepad list so one physical controller is counted once (legacy GetJoystickNames can over-report).</summary>
        private static int GetConnectedJoystickCount()
        {
            return Gamepad.all.Count;
        }

        /// <summary>Number of connected gamepads/controllers. Use to require at least one before playing.</summary>
        public static int GetConnectedControllerCount()
        {
            return GetConnectedJoystickCount();
        }

        /// <summary>Returns assigned device index for the player (1+ = gamepad; keyboard not used), or null if this player is AI.</summary>
        public int? GetAssignedDevice(int playerId)
        {
            if (!_playerDeviceIndex.TryGetValue(playerId, out int index) || index < 0)
                return null;
            return index;
        }

        // ── Online: network client → arena player ID mapping ─────────────────────────

        private Dictionary<ulong, int> _networkClientToPlayer = new Dictionary<ulong, int>();

        /// <summary>Host-only: record which arena player ID belongs to NGO client <paramref name="clientId"/>.</summary>
        public void AssignNetworkClient(ulong clientId, int playerId)
        {
            _networkClientToPlayer[clientId] = playerId;
        }

        /// <summary>Returns the arena player ID for <paramref name="clientId"/>, or null if unmapped.</summary>
        public int? GetPlayerIdForClient(ulong clientId)
        {
            return _networkClientToPlayer.TryGetValue(clientId, out int id) ? id : (int?)null;
        }

        /// <summary>Removes the mapping when a client disconnects.</summary>
        public void RemoveNetworkClient(ulong clientId)
        {
            _networkClientToPlayer.Remove(clientId);
        }
    }
}
