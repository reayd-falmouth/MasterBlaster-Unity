using Scenes.Arena;
using Scenes.Arena.Bomb;
using UnityEngine;
using Unity.MLAgents;

namespace Scenes.Arena.Player.AI
{
    /// <summary>
    /// Adapts BombermanAgent (ML-Agents) to IAIBrain so AIPlayerInput can use a trained policy.
    /// RequestDecision() is called each Tick; last action is read from the agent.
    /// </summary>
    [RequireComponent(typeof(BombermanAgent))]
    public class MLAgentsBrain : MonoBehaviour, IAIBrain
    {
        [Tooltip("Request a new decision every this many seconds (e.g. 0.15).")]
        public float decisionInterval = 0.15f;

        private BombermanAgent _agent;
        private float _nextDecisionTime;

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

            if (Time.time >= _nextDecisionTime)
            {
                _agent.RequestDecision();
                _nextDecisionTime = Time.time + decisionInterval;
            }

            move = _agent.LastMove;
            placeBomb = _agent.LastPlaceBomb;
            detonateHeld = _agent.LastDetonateHeld;
        }
    }
}
