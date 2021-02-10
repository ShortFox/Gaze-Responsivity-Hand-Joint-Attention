using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Research.Unity;

public class TestCommunicationToServer_Eyes : MonoBehaviour {

    private VRGazeTrail GAZED;
    private SyncExperiment_Debug SYNC;


    // Use this for initialization
    void Start ()
    {
        GAZED = VRGazeTrail.Instance;
        SYNC = SyncExperiment_Debug.Instance;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (GAZED.LatestHitObject == null)
        {
            SYNC.TransmitSync(this.name, "");
        }
        else
        {  
            if (GAZED.LatestHitObject.name == "Cube")
            {
                SYNC.TransmitSync(this.name, GAZED.LatestHitObject.name);
            }
            else
            {
                SYNC.TransmitSync(this.name, "");
            }
        }
	}
}
