using Scenes.Arena;
using Scenes.Arena.Bomb;
using Scenes.Arena.Player.AI;
using UnityEngine;

namespace Scenes.Arena.Player
{
    /// <summary>
    /// Input provider driven by an AI brain. Ticks the brain each frame and exposes move/bomb/detonate for the rest of the player.
    /// </summary>
    public class AIPlayerInput : MonoBehaviour, IPlayerInput
    {
        private IAIBrain _brain;
        private BombController _bombController;
        private Vector2 _lastMove;
        private bool _pendingBombDown;
        private bool _detonateHeld = true;

        public void Init(IAIBrain brain)
        {
            _brain = brain;
            _bombController = GetComponent<BombController>();
        }

        private void Update()
        {
            if (_brain == null) return;

            var gm = GameManager.Instance;
            GameObject[] allPlayers = gm != null ? gm.GetPlayers() : null;

            _brain.Tick(
                transform,
                _bombController,
                allPlayers,
                out Vector2 move,
                out bool placeBomb,
                out bool detonateHeld
            );

            _lastMove = move;
            if (placeBomb)
                _pendingBombDown = true;
            _detonateHeld = detonateHeld;
        }

        public Vector2 GetMoveDirection()
        {
            return _lastMove;
        }

        public bool GetBombDown()
        {
            if (!_pendingBombDown) return false;
            _pendingBombDown = false;
            return true;
        }

        public bool GetDetonateHeld()
        {
            return _detonateHeld;
        }
    }
}
