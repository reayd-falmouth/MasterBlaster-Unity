using Core;
using UnityEngine;

namespace Scenes.Arena.Map
{
    [RequireComponent(typeof(Collider2D))]
    public class Indestructible : MonoBehaviour
    {
        private AnimatedSpriteRenderer anim;

        private void Awake()
        {
            anim = GetComponent<AnimatedSpriteRenderer>();
        }

        private void Start()
        {
            if (anim != null)
                anim.StartAnimation();
        }
    }
}
