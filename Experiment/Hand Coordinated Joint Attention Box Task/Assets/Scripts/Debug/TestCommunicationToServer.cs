using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCommunicationToServer : MonoBehaviour {

    //private SyncExperiment_Debug SYNC;

	// Use this for initialization
	void Start ()
    {
        //SYNC = SyncExperiment_Debug.Instance;	
	}
	
	// Update is called once per frame
	void Update ()
    {
		if (Input.GetKeyDown(KeyCode.Z))
        {
            //SYNC.TransmitSync("Player1", true);
            Debug.Log("Z Key Hit");
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            //SYNC.TransmitSync("Player2", true);
            Debug.Log("M Key Hit");
        }
	}
}
