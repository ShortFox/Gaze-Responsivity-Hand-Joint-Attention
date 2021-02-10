namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Eyes : MonoBehaviour
    {
        private Transform Left;
        public Vector3 LeftPos { get { return Left.localPosition; } }
        public Vector3 LeftRot { get { return Left.localEulerAngles; } }

        private Transform Right;
        public Vector3 RightPos { get { return Right.localPosition; } }
        public Vector3 RightRot { get { return Right.localEulerAngles; } }

        private SyncActors SYNC;
        public Vector3 GazedLocat { get { return SYNC.gazePoint; } }
        public string GazedName { get { return SYNC.gazeObject; } }

        private EyeControl _eyeControl;

        // Use this for initialization
        void Start()
        {
            SYNC = this.transform.parent.GetComponent<SyncActors>();
            _eyeControl = this.GetComponent<EyeControl>();

            Left = _eyeControl.LeftEye;
            Right = _eyeControl.RightEye;
        }
    }
}

