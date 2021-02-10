using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(NetworkBehaviour))]
public class SyncExperiment_Debug : NetworkBehaviour {

    public static SyncExperiment_Debug Instance { get; private set; }


    [SerializeField] Material RedColor;
    [SerializeField] Material GreenColor;

    [SyncVar] public bool networkSynced;

    [SyncVar] public string syncA;
    [SyncVar] public string syncB;

    private void Awake()
    {
        Instance = this;
    }
    // Update is called once per frame
    void Update ()
    {
        if (isClient)
        {
            if (networkSynced) ChangeColor(GreenColor);
            else ChangeColor(RedColor);
        }

        if (isServer)
        {
            networkSynced = CheckSync();
        }
    }

    #region Server Commands
    [Command]
    void CmdTransmitSync(string playerID, string name)
    {
        switch (playerID)
        {
            case "Player1":
                syncA = name;
                break;
            case "Player2":
                syncB = name;
                break;
            default:
                Debug.LogError("Error with CmdTransmitSync. PlayerID not what expected.");
                break;
        }
    }
    #endregion

    #region Client Messages
    [ClientCallback]
    public void TransmitSync(string playerID, string name)
    {
        CmdTransmitSync(playerID, name);
    }
    #endregion

    private bool CheckSync()
    {
        if (syncA == syncB)
        {
            if (syncA == "") return false;
            else return true;
        }
        else return false;
    }
    void ChangeColor(Material NewColor)
    {
        this.GetComponent<Renderer>().material = NewColor;
    }
}
