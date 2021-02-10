namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;

    public class PlayerProperties : NetworkBehaviour
    {

        [SyncVar]
        public int playerID;
        private string playerName;

        [SerializeField]
        GameObject[] playerComponents;

        private Transform myCamera;
        public Transform myFace { get; private set; }
        public Transform myBody { get; private set; }
        public Transform myHand { get; private set; }

        #region Unity Methods
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            myCamera = this.transform.Find("Camera");
            myCamera.gameObject.GetComponent<Camera>().enabled = true;
            myCamera.gameObject.tag = "MainCamera";
            this.gameObject.tag = "Player";
            SetName(playerID);
            AddPlayerComponents();

            //Name Face, Body and Hand.
            myBody = this.transform.Find("Avatar").Find("mixamorig:Hips").Find("mixamorig:Spine");
            myBody.GetComponent<BoxCollider>().enabled = false;

            myFace = myBody.Find("mixamorig:Spine1").Find("mixamorig:Spine2").Find("mixamorig:Neck").Find("mixamorig:Head");
            myFace.GetComponent<BoxCollider>().enabled = false;


            myHand = myBody.Find("mixamorig:Spine1").Find("mixamorig:Spine2").Find("mixamorig:RightShoulder").Find("mixamorig:RightArm").Find("mixamorig:RightForeArm").Find("mixamorig:RightHand").Find("mixamorig:RightHandIndex1");
            myHand.GetComponent<BoxCollider>().enabled = false;

            SetObjName(myFace, "Face", playerID);
            SetObjName(myBody, "Body", playerID);
            SetObjName(myHand, "Hand", playerID);
        }

        private void Update()
        {
            //Update Variables if not the local player
            if (!isLocalPlayer)
            {
                if (playerName == null)
                {
                    this.gameObject.tag = "OtherPlayer";

                    var myPlayer = GameObject.FindGameObjectWithTag("Player");
                    myPlayer.GetComponent<SyncActors>().partnerPlayer = new OtherPlayer(this.gameObject);
                    ExperimentManager.Instance.InitializeDataHolder();
                    this.GetComponent<SyncActors>().partnerFound = true;

                    SetName(playerID);

                    //Name Face, Body and Hand.
                    myBody = this.transform.Find("Avatar").Find("mixamorig:Hips").Find("mixamorig:Spine");
                    myFace = myBody.Find("mixamorig:Spine1").Find("mixamorig:Spine2").Find("mixamorig:Neck").Find("mixamorig:Head");
                    myHand = myBody.Find("mixamorig:Spine1").Find("mixamorig:Spine2").Find("mixamorig:RightShoulder").Find("mixamorig:RightArm").Find("mixamorig:RightForeArm").Find("mixamorig:RightHand").Find("mixamorig:RightHandIndex1");

                    SetObjName(myFace, "Face", playerID);
                    SetObjName(myBody, "Body", playerID);
                    SetObjName(myHand, "Hand", playerID);
                }
            }
        }
        #endregion

        #region Helper Functions
        private void AddPlayerComponents()
        {
            foreach (GameObject obj in playerComponents)
            {
                Instantiate(obj, Vector3.zero, Quaternion.identity);
            }

        }
        private void SetName(int ID)
        {
            playerName = "Player" + ID.ToString();
            this.transform.name = playerName;
        }
        
        private void SetObjName(Transform obj, string objName, int ID)
        {
            string name = playerName + objName;
            obj.name = name;
        }
        #endregion


    }
}

