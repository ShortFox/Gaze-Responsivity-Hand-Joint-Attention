namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class PolhemusRead : PolhemusStream
    {
        public static PolhemusRead Instance { get; private set; }
        private GameObject[] _objects;

        private int _sensorIndx;
        private int SensorIndx
        {
            get { return _sensorIndx; }
            set
            {
                if (value > active.Length-1)
                {
                    _sensorIndx = 0;
                }
                else
                { _sensorIndx = value; }
            }
        }
        private Vector3 _posAxisCorrection;
        private Vector4 _orientAxisCorrection;

        private float _unitConversion = 0.0254f;
        
        private float _flipX;
        public float FlipX
        {
            get { return _flipX; }
            set
            {
                _flipX = Mathf.Sign(value);
                _posAxisCorrection.x = _flipX;
            }
        }
        private float _flipY;
        public float FlipY
        {
            get { return _flipY; }
            set
            {
                _flipY = value;
                _posAxisCorrection.y = _flipY;
            }
        }
        private float _flipZ;
        public float FlipZ
        {
            get { return _flipZ; }
            set
            {
                _flipZ = value;
                Debug.Log("Z: " + _flipZ);
                _posAxisCorrection.z = _flipZ;
            }
        }

        private Vector3 _center;

        private Vector3 _offset;

        public int w_dim;
        public int x_dim;
        public int y_dim;
        public int z_dim;

        void Awake()
        {
            Instance = this;
            SensorIndx = 0;

            _flipX = -1;
            _flipY = 1;
            _flipZ = 1;
            _posAxisCorrection = new Vector3(_flipX,_flipY,_flipZ);
            _orientAxisCorrection = new Vector4(1, -1, 1, 1);

            _objects = new GameObject[1] { this.gameObject };

            _offset = _objects[0].transform.position;
        }
        protected override void Start()
        {
            base.Start();
            CenterObjects();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateLocation();
        }
        void UpdateLocation()
        {
            if (active[SensorIndx])
            {
                Vector3 pol_position = positions[SensorIndx] - _center;
                Vector3 unity_position;
                unity_position.x = pol_position[0] * _posAxisCorrection[0];
                unity_position.y = pol_position[2] * _posAxisCorrection[1];
                unity_position.z = pol_position[1] * _posAxisCorrection[2];

                Vector4 pol_rotation = orientations[SensorIndx];
                Quaternion unity_rotation;
                unity_rotation.w = pol_rotation[w_dim] *  _orientAxisCorrection[0];
                unity_rotation.x = pol_rotation[x_dim] *  _orientAxisCorrection[1];
                unity_rotation.y = pol_rotation[y_dim] * _orientAxisCorrection[2];
                unity_rotation.z = pol_rotation[z_dim] *  _orientAxisCorrection[3];


                try
                {
                    _objects[SensorIndx].transform.position = (unity_position*_unitConversion)+_offset;
                    _objects[SensorIndx].transform.rotation = unity_rotation;
                }
                catch
                {
                    Debug.Log("There are fewer GameObjects than Polhemus sensors");
                }

                //Unclear what the following conditional refers to but it is used in Polhemus demo script.
                if (digio[SensorIndx] != 0)
                {
                    CenterObjects();
                }
            }
            SensorIndx++;
        }
        //Centers motion data
        public void CenterObjects()
        {
            for (var i = 0; i < active.Length; i++)
            {
                if (active[i])
                {
                    _center = positions[i];
                    break;
                }
            }
        }
    }

}
