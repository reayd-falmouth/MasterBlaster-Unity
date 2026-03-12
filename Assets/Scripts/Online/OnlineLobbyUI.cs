using UnityEngine;
using UnityEngine.UI;

namespace Online
{
    /// <summary>
    /// Simple UI panel for hosting or joining an online game.
    /// Wire buttons and input field in the Inspector.
    /// </summary>
    public class OnlineLobbyUI : MonoBehaviour
    {
        [Header("Create Game")]
        [SerializeField] private Button createButton;

        [Header("Join Game")]
        [SerializeField] private InputField joinCodeInput;
        [SerializeField] private Button joinButton;

        [Header("Status")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text joinCodeDisplay;

        private void Awake()
        {
            createButton.onClick.AddListener(OnCreateClicked);
            joinButton.onClick.AddListener(OnJoinClicked);
        }

        private async void OnCreateClicked()
        {
            SetStatus("Creating lobby...");
            createButton.interactable = false;
            joinButton.interactable = false;

            try
            {
                await NetworkLobbyManager.Instance.CreateLobbyAsync();
                string code = NetworkLobbyManager.Instance.LobbyJoinCode;
                joinCodeDisplay.text = $"Lobby code: {code}";
                SetStatus("Hosting — waiting for players.");
            }
            catch (System.Exception e)
            {
                SetStatus($"Error: {e.Message}");
                createButton.interactable = true;
                joinButton.interactable = true;
            }
        }

        private async void OnJoinClicked()
        {
            string code = joinCodeInput.text.Trim().ToUpper();
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("Enter a lobby code first.");
                return;
            }

            SetStatus($"Joining lobby {code}...");
            createButton.interactable = false;
            joinButton.interactable = false;

            try
            {
                await NetworkLobbyManager.Instance.JoinLobbyAsync(code);
                SetStatus("Connected!");
            }
            catch (System.Exception e)
            {
                SetStatus($"Error: {e.Message}");
                createButton.interactable = true;
                joinButton.interactable = true;
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
            Debug.Log($"[OnlineLobbyUI] {msg}");
        }
    }
}
