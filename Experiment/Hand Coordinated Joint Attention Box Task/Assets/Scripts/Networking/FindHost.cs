namespace MQ.MultiAgent
{
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;

    public class FindHost : NetworkDiscovery
    {
        //Button to Connect to Host
        [SerializeField] Button JoinButton;

        // Use this for initialization
        void Awake()
        {
            JoinButton.interactable = false;
            base.Initialize();
            base.StartAsClient();       //Native behavior is to search for hosts.
        }

        public override void OnReceivedBroadcast(string fromAddress, string data)
        {
            NetworkManager.singleton.networkAddress = fromAddress;
            JoinButton.interactable = true;
        }
    }

}
