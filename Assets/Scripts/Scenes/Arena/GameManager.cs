using System;
using System.Collections;
using Core;
using Scenes.Arena.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Utilities;

namespace Scenes.Arena
{
    [DefaultExecutionOrder(-1)]
    public class GameManager : Singleton<GameManager>
    {
        // public static GameManager Instance { get; private set; }

        [SerializeField] private GameObject[] players;

        [SerializeField] private bool shrinkingEnabled;
        [SerializeField] private bool normalLevel;
        [SerializeField] private bool startMoney;
        
        [Header("Assign the 5 players in inspector")]
        public GameObject topLeftPlayer;
        public GameObject topRightPlayer;
        public GameObject bottomLeftPlayer;
        public GameObject bottomRightPlayer;
        public GameObject middlePlayer;
    
        private void Start()
        {
            players = GameObject.FindGameObjectsWithTag("Player");
            int playerCount = PlayerPrefs.GetInt("Players", 2);
            SetupPlayers(playerCount);

            // 🔹 Force upgrades to be reapplied every new game round
            foreach (var p in players)
            {
                var pc = p.GetComponent<PlayerController>();
                if (pc != null)
                    pc.ApplyUpgrades();
            }
            
            // Load settings
            shrinkingEnabled = PlayerPrefs.GetInt("Shrinking", 1) == 1;
            normalLevel      = PlayerPrefs.GetInt("NormalLevel", 1) == 1;
            startMoney       = PlayerPrefs.GetInt("StartMoney", 0) == 1;
            
            if (!normalLevel)
                LoadAlternateLevelSettings();

            if (startMoney)
                GivePlayersStartCoin();
        }

        void SetupPlayers(int count)
        {
            // Turn all off first
            topLeftPlayer.SetActive(false);
            topRightPlayer.SetActive(false);
            bottomLeftPlayer.SetActive(false);
            bottomRightPlayer.SetActive(false);
            middlePlayer.SetActive(false);

            int id = 1;

            switch (count)
            {
                case 2:
                    EnablePlayer(topLeftPlayer, id++);
                    EnablePlayer(bottomRightPlayer, id++);
                    break;

                case 3:
                    EnablePlayer(topLeftPlayer, id++);
                    EnablePlayer(bottomRightPlayer, id++);
                    EnablePlayer(middlePlayer, id++);
                    break;

                case 4:
                    EnablePlayer(topLeftPlayer, id++);
                    EnablePlayer(topRightPlayer, id++);
                    EnablePlayer(bottomLeftPlayer, id++);
                    EnablePlayer(bottomRightPlayer, id++);
                    break;

                case 5:
                    EnablePlayer(topLeftPlayer, id++);
                    EnablePlayer(topRightPlayer, id++);
                    EnablePlayer(bottomLeftPlayer, id++);
                    EnablePlayer(bottomRightPlayer, id++);
                    EnablePlayer(middlePlayer, id++);
                    break;
            }
        }

        private void EnablePlayer(GameObject playerObj, int id)
        {
            playerObj.SetActive(true);
            var movement = playerObj.GetComponent<PlayerController>();
            if (movement != null)
                movement.playerId = id;
        }
        
        public void CheckWinState()
        {
            int aliveCount = 0;
            GameObject lastAlive = null;

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].activeSelf)
                {
                    aliveCount++;
                    lastAlive = players[i];
                }
            }

            if (aliveCount <= 1)
            {
                if (lastAlive != null)
                {
                    var movement = lastAlive.GetComponent<PlayerController>();
                    if (movement != null)
                    {
                        movement.wins++;
                        PlayerPrefs.SetInt(lastAlive.name + "_Wins", movement.wins);
                        PlayerPrefs.Save();

                        int winsNeeded = PlayerPrefs.GetInt("WinsNeeded", 3);
                        if (movement.wins >= winsNeeded)
                        {
                            // Store winner name for display later
                            PlayerPrefs.SetString("WinnerName", lastAlive.name);
                            PlayerPrefs.Save();

                            SceneFlowManager.I.GoToOvers();
                            return;
                        }
                    }
                }

                Invoke(nameof(Standings), 3f);
            }
        }
    
        private void Standings()
        {
            SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        // ------------------- Custom behaviours -------------------

        private void EndGame()
        {
            Debug.Log("[GameManager] Timer expired → game over!");
            // Option A: go directly to Standings
            SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        void LoadAlternateLevelSettings()
        {
            Debug.Log("[GameManager] NormalLevel disabled → loading alternate map settings");
            // TODO: load alternate spawn points, layouts, etc.
        }

        void GivePlayersStartCoin()
        {
            foreach (var p in players)
            {
                var movement = p.GetComponent<PlayerController>();
                if (movement != null)
                {
                    movement.coins += 1;
                    Debug.Log($"{p.name} given 1 start coin (total: {movement.coins})");
                }
            }
        }
    
        public GameObject[] GetPlayers() => players;
    }
}
