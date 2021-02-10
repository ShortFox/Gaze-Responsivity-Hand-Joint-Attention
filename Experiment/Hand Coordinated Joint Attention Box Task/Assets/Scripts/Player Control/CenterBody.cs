using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterBody : MonoBehaviour {

    private Transform myCamera;
    private bool isMine;
	// Use this for initialization
	void Start ()
    {
        myCamera = this.transform.parent.Find("Camera");
        if (myCamera.tag == "MainCamera") isMine = true; else isMine = false;
        Center();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isMine) Center();
        }
	}
    public void Center()
    {
        if (isMine)
        {
            this.transform.position = new Vector3(myCamera.position.x, this.transform.position.y, myCamera.position.z);
            Vector3 rotation = Vector3.zero;
            rotation.y = myCamera.eulerAngles.y;
            this.transform.eulerAngles = rotation;
        }
    }
}
