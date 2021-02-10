namespace MQ.MultiAgent
{
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.Networking.NetworkSystem;

    public class NetworkManagement : NetworkManager
    {
        private NetworkDiscovery HostFinder;

        private int port_num = 7777;       //Port number to connect to
        private int prefabIndx;

        private void Start()
        {
            HostFinder = this.GetComponent<FindHost>();
        }

        #region Host Functions
        /// <summary>
        /// Host Startup.
        /// </summary>
        public void StartupHost()
        {
            NetworkManager.singleton.networkPort = port_num;
            NetworkManager.singleton.StartHost();
        }
        public override void OnStartHost()
        {
            HostFinder.StopBroadcast();
            HostFinder.Initialize();
            HostFinder.StartAsServer();
        }
        #endregion

        #region Client Functions
        /// <summary>
        /// Client Startup
        /// </summary>
        public void JoinGame()
        {
            NetworkManager.singleton.networkPort = port_num;
            NetworkManager.singleton.StartClient();
        }
        public override void OnStopClient()
        {
            HostFinder.StopBroadcast();
        }
        #endregion

        #region Player Setup and Spawn
        public override void OnClientConnect(NetworkConnection conn)
        {
            if (TitleScreenData.Gender == "Female")
            {
                prefabIndx = spawnPrefabs.FindIndex(
                    delegate (GameObject obj)
                    {
                        return obj.name == "Player_Female";
                    });
            }
            else
            {
                prefabIndx = spawnPrefabs.FindIndex(
                    delegate (GameObject obj)
                    {
                        return obj.name == "Player_Male";
                    });
            }

            IntegerMessage msg = new IntegerMessage(prefabIndx);

            if (!clientLoadedScene) ClientScene.AddPlayer(conn, 0, msg);

        }
        public override void OnClientSceneChanged(NetworkConnection conn)
        {
            IntegerMessage msg = new IntegerMessage(prefabIndx);

            ClientScene.Ready(conn);

            //bool addPlayer = (ClientScene.localPlayers.Count == 0);
            bool foundPlayer = false;

            for (int i =0; i< ClientScene.localPlayers.Count; i++)
            {
                if (ClientScene.localPlayers[i].gameObject != null)
                {
                    foundPlayer = true;
                    break;
                }
            }
            if (!foundPlayer) ClientScene.AddPlayer(conn, 0, msg);

        }
        //Taken from here: https://docs.unity3d.com/Manual/UNetPlayersCustom.html
        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader extraMsg)
        {
            GameObject player;

            int prefabIndx = 0;
            if (extraMsg != null)
            {
                var id = extraMsg.ReadMessage<IntegerMessage>();
                prefabIndx = id.value;
                Debug.Log("prefabIndx: " + prefabIndx);
            }

            if (numPlayers > 0)
            {
                player = (GameObject)Instantiate(spawnPrefabs[prefabIndx], Vector3.zero, Quaternion.identity);
                player.GetComponent<PlayerProperties>().playerID = 2;
            }
            else
            {
                player = (GameObject)Instantiate(spawnPrefabs[prefabIndx], Vector3.zero, Quaternion.identity);
                player.GetComponent<PlayerProperties>().playerID = 1;
            }

            /*
            GameObject playerGender;
            if (TitleScreenData.Gender == "Female")
            {
                playerGender = spawnPrefabs.Find(
                    delegate (GameObject obj)
                    {
                        return obj.name == "Player_Female";
                    });
            }
            else
            {
                playerGender = spawnPrefabs.Find(
                    delegate (GameObject obj)
                    {
                        return obj.name == "Player_Male";
                    });
            }

            if (numPlayers > 0)
            {
                player = (GameObject)Instantiate(playerGender, Vector3.zero, Quaternion.identity);
                player.GetComponent<PlayerProperties>().playerID = 2;
            }
            else
            {
                player = (GameObject)Instantiate(playerGender, Vector3.zero, Quaternion.identity);
                player.GetComponent<PlayerProperties>().playerID = 1;
            }
            */
            NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
        }
            #endregion
        }
}
