namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;
    using Tobii.Research.Unity;

    [NetworkSettings(sendInterval = 0.02f)]   //Update States at 50fps
    public class SyncActors : NetworkBehaviour
    {
        #region Networked Movement Variables
        //Avatar's Body
        [SyncVar] private Vector3 posAvatar;
        [SyncVar] private Quaternion rotAvatar;

        //Camera
        [SyncVar] private Vector3 posCamera;
        [SyncVar] private Quaternion rotCamera;

        //Finger
        [SyncVar] private Vector3 posFinger;
        [SyncVar] private Quaternion rotFinger;

        //LeftEye
        [SyncVar] public Vector3 gazeLeftEye;           //Local Vector of Gaze Direction.
        [SyncVar] public bool closedLeftEye;            //Flag if left eye is closed.
        [SyncVar] public Vector3 rotLeftEye;            //Local rotation of left eye.

        //RightEye
        [SyncVar] public Vector3 gazeRightEye;          //Local Vector of Gaze Direction.
        [SyncVar] public bool closedRightEye;           //Flag if right eye is closed.
        [SyncVar] public Vector3 rotRightEye;           //Local rotation of right eye.

        //Gaze
        [SyncVar] public string gazeObject;             //Object being gazed
        [SyncVar] public Vector3 gazePoint;             //Point of object gaze.

        //Touch
        [SyncVar] public string touchObject;
        [SyncVar] public string pointObject;

        //Experiment Relevant Variables.
        [SyncVar] public bool readyState;               //Flag indicating if player is ready for multiplayer.
        [SyncVar] public Vector3 calibrationPoint;      //Vector3 to place FixationCross during Experiment. This is set when Player is calibrated.

        [SyncVar] public float localTime;               //Local Time for player.
        #endregion

        public OtherPlayer partnerPlayer;
        public bool partnerFound;

        #region Private Fields
        private Transform myAvatar;
        private Transform myCamera;
        private Transform myFinger;                  //This is updated in Update Loop, below.

        private SyncExperiment SYNC;
        private VRGazeTrail GAZED;
        private Hand HAND;

        //private float _lerpRate = 0.1f;         //% distance change from old value to new value when updating position/rotation.
        #endregion

        private void Awake()
        {
            myAvatar = this.transform.Find("Avatar");
            myCamera = this.transform.Find("Camera");
            myFinger = this.transform.Find("Finger");
            HAND = myFinger.GetComponent<Hand>();
        }
        private void Start()
        {
            SYNC = SyncExperiment.Instance;
            GAZED = VRGazeTrail.Instance;
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
                if (SYNC.networkSync) TransmitVariables();
                TransmitReadiness();
                return;
            }
            if (!isLocalPlayer)
            {
                if (!SYNC.networkSync && partnerFound)
                {
                    if (myAvatar.gameObject.activeInHierarchy)
                    {
                        myAvatar.gameObject.SetActive(false);
                        myFinger.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (!myAvatar.gameObject.activeInHierarchy)
                    {
                        myAvatar.gameObject.SetActive(true);
                        myFinger.gameObject.SetActive(true);
                    }
                }

                if (myAvatar.gameObject.activeInHierarchy) UpdateVariables();
                return;
            }
        }

        #region Server Commands
        //Server runs this.
        [Command]
        void CmdAvatarToServer(Vector3 pos, Quaternion rot)
        {
            posAvatar = pos;
            rotAvatar = rot;
        }
        [Command]
        void CmdCameraToServer(Vector3 pos, Quaternion rot)
        {
            posCamera = pos;
            rotCamera = rot;
        }
        [Command]
        void CmdFingerToServer(Vector3 pos, Quaternion rot)
        {
            posFinger = pos;
            rotFinger = rot;
        }
        [Command]
        void CmdLeftEyeToServer(Vector3 gaze, bool closed, Vector3 rot)
        {
            gazeLeftEye = gaze;
            closedLeftEye = closed;
            rotLeftEye = rot;
        }
        [Command]
        void CmdRightEyeToServer(Vector3 gaze, bool closed, Vector3 rot)
        {
            gazeRightEye = gaze;
            closedRightEye = closed;
            rotRightEye = rot;
        }
        [Command]
        void CmdGazeToServer(string name, Vector3 point)
        {
            gazeObject = name;
            gazePoint = point;
        }
        [Command]
        void CmdTouchPointToServer(string nameT, string nameP)
        {
            touchObject = nameT;
            pointObject = nameP;
        }
        [Command]
        void CmdTimeToServer(float time)
        {
            localTime = time;
        }
        [Command]
        void CmdStateToServer(bool state)
        {
            readyState = state;
        }
        [Command]
        void CmdCalibrationPointToServer(Vector3 point)
        {
            calibrationPoint = point;
        }
        #endregion

        #region Client Messages
        //Client message to Server.
        [ClientCallback]
        void TransmitVariables()
        {
            CmdAvatarToServer(myAvatar.position, myAvatar.rotation);
            CmdCameraToServer(myCamera.position, myCamera.rotation);
            CmdFingerToServer(myFinger.position, myFinger.rotation);
            CmdLeftEyeToServer(gazeLeftEye, closedLeftEye, rotLeftEye);
            CmdRightEyeToServer(gazeRightEye, closedRightEye, rotRightEye);

            try
            {
                CmdGazeToServer(GAZED.LatestHitObject.name, GAZED.LatestHitPoint);
            }
            catch
            {
                CmdGazeToServer("", Vector3.positiveInfinity);
            }
            try
            {
                CmdTouchPointToServer(HAND.ContactName, HAND.PointName);
            }
            catch
            {
                CmdTouchPointToServer("","");
            }

            CmdTimeToServer(Time.timeSinceLevelLoad);
        }
        [ClientCallback]
        void TransmitReadiness()
        {
            CmdStateToServer(readyState);
        }
        [ClientCallback]
        public void TransmitCalibrationPoint(Vector3 point)
        {
            point.x = 0;
            CmdCalibrationPointToServer(point);
        }
        #endregion

        #region Helper Methods
        void UpdateVariables()
        {
            myAvatar.position = SmoothTranslation(myAvatar.position,posAvatar);
            myAvatar.rotation = SmoothRotation(myAvatar.rotation,rotAvatar);
            myCamera.position = SmoothTranslation(myCamera.position,posCamera);
            myCamera.rotation = SmoothRotation(myCamera.rotation, rotCamera);
            myFinger.position = SmoothTranslation(myFinger.position,posFinger);
            myFinger.rotation = SmoothRotation(myFinger.rotation,rotFinger);
        }

        Vector3 SmoothTranslation(Vector3 oldPos, Vector3 newPos)
        {
            return newPos;
            //return Vector3.Lerp(oldPos, newPos, _lerpRate);
        }

        Quaternion SmoothRotation(Quaternion oldRot, Quaternion newRot)
        {
            return newRot;
            //return Quaternion.Lerp(oldRot, newRot, _lerpRate);
        }
        #endregion
    }
}
