using Scenes.Arena;
using Scenes.Arena.Bomb;
using UnityEngine;
using Unity.MLAgents;

namespace Scenes.Arena.Player.AI
{
    /// <summary>
    /// Adapts BombermanAgent (ML-Agents) to IAIBrain so AIPlayerInput can use a trained policy.
    /// Decisions are driven by the agent's DecisionRequester (Academy step cycle); we only read LastMove here.
    /// </summary>
    [RequireComponent(typeof(BombermanAgent))]
    public class MLAgentsBrain : MonoBehaviour, IAIBrain
    {
        private BombermanAgent _agent;
        private float _lastLogTime = -999f;

        private void Awake()
        {
            _agent = GetComponent<BombermanAgent>();
        }

        public void Tick(
            Transform self,
            BombController bombController,
            GameObject[] allPlayers,
            out Vector2 move,
            out bool placeBomb,
            out bool detonateHeld
        )
        {
            if (_agent == null)
            {
                move = Vector2.zero;
                placeBomb = false;
                detonateHeld = true;
                return;
            }

            move = _agent.LastMove;
            placeBomb = _agent.LastPlaceBomb;
            detonateHeld = _agent.LastDetonateHeld;

            if (Time.time - _lastLogTime >= 2f)
            {
                _lastLogTime = Time.time;
                Debug.Log($"[MLAgentsBrain] {gameObject.name} → LastMove={move} (zero={move.sqrMagnitude < 0.01f})");
            }
        }
    }
}
