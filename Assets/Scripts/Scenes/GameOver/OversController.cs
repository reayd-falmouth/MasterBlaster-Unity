using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.GameOver
{
    public class WinnerController : MonoBehaviour
    {
        public Text winnerText;

        private void Start()
        {
            string winnerName = PlayerPrefs.GetString("WinnerName", "Unknown");
            winnerText.text = $"{winnerName} Wins the Match!";
        }

        // Optional: press a key to go back to main menu
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SceneFlowManager.I.GoTo(FlowState.Menu);
            }
        }
    }
}
