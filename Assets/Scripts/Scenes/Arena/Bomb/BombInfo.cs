using UnityEngine;

namespace Scenes.Arena.Bomb
{
    /// <summary>
    /// Attached to bomb instances so AI (and others) can read explosion radius. Set by BombController when spawning.
    /// </summary>
    public class BombInfo : MonoBehaviour
    {
        public int explosionRadius = 1;
    }
}
