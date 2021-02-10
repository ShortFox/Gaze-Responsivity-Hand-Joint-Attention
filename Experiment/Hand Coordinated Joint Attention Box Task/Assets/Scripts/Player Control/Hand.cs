namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Hand : PolhemusStream
    {
        #region Hand Position, Rotation, and Contact Object Data and Methods
        private Vector3 _position;
        public Vector3 Position { get { return _position; } set { _position = value; } }
        private Quaternion _rotation;
        public Quaternion Rotation { get { return _rotation; } set { _rotation = value; } }
        private GameObject _contactObject;
        public GameObject ContactObject { get { return _contactObject; } }
        public string ContactName { get { return ContactObject == null ? "" : ContactObject.name; } }
        private string _pointObject;
        public string PointName { get { return _pointObject; } }

        #endregion

        #region Polhemus Interaction
        private bool isMine;                                        //Updates Object Position/Rotation. (True if Parent is LocalPlayer). Defined in Start.
        private int _sensorIndx = 0;                                //Sensor index of first Sensor.
        private int[] _posDimIndx = new int[3] { 0, 2, 1 };         //Polhemus Stream Data Index for Position.
        private int[] _rotDimIndx = new int[4] { 0, 1, 3, 2 };      //Polhemus Stream Data Index for Rotation.
        private Vector3 _posAxisCorrection;                         //Axis Sign Correction for Position.
        private Vector4 _orientAxisCorrection;                      //Axis Sign Correction for Rotation.
        private float _unitConversion = 0.0254f;                    //Unit Conversion from Inch to Meter.

        private Vector3 _center;                                    //The zero-ed position of sensor.
        private Vector3 _offset;                                    //The offset for Sensor.
        private Vector3[] _candidateOffets;                         //Potential offsets for player's controller. 

        private Transform myCamera;
        private Transform myInitCube;
        #endregion

        #region Unity Methods

        private void OnTriggerEnter(Collider other)
        {
            _contactObject = other.gameObject;
        }
        private void OnTriggerExit(Collider other)
        {
            _contactObject = null;
        }
        private void Awake()
        {
            _posAxisCorrection = new Vector3(1, -1, -1);
            _orientAxisCorrection = new Vector4(1, -1, 1, 1);

            _candidateOffets = new Vector3[] { new Vector3(0, 0.8f, 0.3f), new Vector3(0, 0.8f, -0.3f) };
        }
        protected override void Start()
        {
            myCamera = this.transform.parent.Find("Camera");
            if (myCamera.tag == "MainCamera") isMine = true; else isMine = false;
            myInitCube = this.transform.parent.Find("FingerCubeReady");

            if (isMine)
            {
                base.Start();
                Center();
            }
        }
        private void Update()
        {
            if (isMine)
            {
                UpdateLocation();
                this.transform.position = _position;
                this.transform.rotation = _rotation;
                _pointObject = PointObject();
            }
            else
            {
                _position = this.transform.position;
                _rotation = this.transform.rotation;
            }
        }
        #endregion

        #region Helper Methods
        string PointObject()
        {
            string output = "";

            RaycastHit hitinfo;
            if (Physics.Raycast(this.transform.position, transform.TransformDirection(Vector3.right), out hitinfo,20))
            {
                output = hitinfo.transform.name;
            }

            return output;
        }
        void UpdateLocation()
        {
            if (active[_sensorIndx])
            {
                Vector3 pol_position = positions[_sensorIndx] - _center;
                Vector3 unity_position;
                unity_position.x = pol_position[_posDimIndx[0]] * _posAxisCorrection[0];
                unity_position.y = pol_position[_posDimIndx[1]] * _posAxisCorrection[1];
                unity_position.z = pol_position[_posDimIndx[2]] * _posAxisCorrection[2];

                Vector4 pol_rotation = orientations[_sensorIndx];
                Quaternion unity_rotation;
                unity_rotation.w = pol_rotation[_rotDimIndx[0]] * _orientAxisCorrection[0];
                unity_rotation.x = pol_rotation[_rotDimIndx[1]] * _orientAxisCorrection[1];
                unity_rotation.y = pol_rotation[_rotDimIndx[2]] * _orientAxisCorrection[2];
                unity_rotation.z = pol_rotation[_rotDimIndx[3]] * _orientAxisCorrection[3];

                _position = (unity_position * _unitConversion) + _offset;
                _rotation = unity_rotation;

                //Unclear what the following conditional refers to but it is used in Polhemus demo script.
                if (digio[_sensorIndx] != 0)
                {
                    Center();
                }
            }
        }

        //Centers motion data
        //Edit this to be fixed in real-world.
        public void Center()
        {
            if (active[_sensorIndx])
            {
                _center = positions[_sensorIndx];

                Vector3 offsetLocat = Vector3.zero;
                float closestDist = float.MaxValue;

                foreach (Vector3 offsetPos in _candidateOffets)
                {
                    if (Vector3.Distance(myCamera.position, offsetPos) < closestDist)
                    {
                        offsetLocat = offsetPos;
                        closestDist = Vector3.Distance(myCamera.position, offsetPos);
                    }
                }
                _offset = offsetLocat;
                myInitCube.position = offsetLocat;
            }
        }
        #endregion
    }
}

