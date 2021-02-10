namespace MQ.MultiAgent
{
    using System;
    using UnityEngine;
    using Tobii.Research.Unity;

    public enum InitHeader
    {
        TaskName,
        TrialID,
        TargetLocation,
        RoleA,
        B1_val_A,
        B2_val_A,
        B3_val_A,
        RoleB,
        B3_val_B,
        B2_val_B,
        B1_val_B
    }

    #region Data Structures
    public struct TrialInfo
    {
        public string Task;
        public int TrialID;
        public int TargetLocation;
        public string RoleA;
        public int B1_Val_A;
        public int B2_Val_A;
        public int B3_Val_A;
        public string RoleB;
        public int B3_Val_B;
        public int B2_Val_B;
        public int B1_Val_B;
        public string[] TrialString;

        public TrialInfo(string[] trialInfo)
        {
            Task = trialInfo[(int)InitHeader.TaskName];
            TrialID = Convert.ToInt16(trialInfo[(int)InitHeader.TrialID]);
            TargetLocation = Convert.ToInt16(trialInfo[(int)InitHeader.TargetLocation]);
            try
            {
                RoleA = trialInfo[(int)InitHeader.RoleA];
                B1_Val_A = Convert.ToInt16(trialInfo[(int)InitHeader.B1_val_A]);
                B2_Val_A = Convert.ToInt16(trialInfo[(int)InitHeader.B2_val_A]);
                B3_Val_A = Convert.ToInt16(trialInfo[(int)InitHeader.B3_val_A]);
                RoleB = trialInfo[(int)InitHeader.RoleB];
                B3_Val_B = Convert.ToInt16(trialInfo[(int)InitHeader.B3_val_B]);
                B2_Val_B = Convert.ToInt16(trialInfo[(int)InitHeader.B2_val_B]);
                B1_Val_B = Convert.ToInt16(trialInfo[(int)InitHeader.B1_val_B]);
                TrialString = trialInfo;
            }
            catch
            {
                RoleA = null;
                B1_Val_A = 0;
                B2_Val_A = 0;
                B3_Val_A = 0;
                RoleB = null;
                B3_Val_B = 0;
                B2_Val_B = 0;
                B1_Val_B = 0;
                TrialString = null;
            }

        }

        public string Header
        {
            get
            {
                return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                                       "Task",
                                       "TrialID",
                                       "TargetLocation",
                                       "RoleA",
                                       "B1_Val_A",
                                       "B2_Val_A",
                                       "B3_Val_A",
                                       "RoleB",
                                       "B3_Val_B",
                                       "B2_Val_B",
                                       "B1_Val_B");
            }
        }

        public string[] CreateTrialString()
        {
            TrialString = new string[11]
            {
                Task,
                TrialID.ToString(),
                TargetLocation.ToString(),
                RoleA,
                B1_Val_A.ToString(),
                B2_Val_A.ToString(),
                B3_Val_A.ToString(),
                RoleB,
                B3_Val_B.ToString(),
                B2_Val_B.ToString(),
                B1_Val_B.ToString()
            };
            return TrialString;
        }
        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
            Task,
            TrialID.ToString(),
            TargetLocation.ToString(),
            RoleA,
            B1_Val_A.ToString(),
            B2_Val_A.ToString(),
            B3_Val_A.ToString(),
            RoleB,
            B3_Val_B.ToString(),
            B2_Val_B.ToString(),
            B1_Val_B.ToString());
        }
    }
    #endregion

    #region Classes
    /// <summary>
    /// Static Class organizing Title Screen Data.
    /// </summary>
    public static class TitleScreenData
    {
        public static int SubjectNum;
        public static string Gender;
    }
    /// <summary>
    /// Class holding all relevant data.
    /// </summary>
    public class DataHolder
    {
        ExperimentManager Experiment;
        SyncExperiment Sync;
        EyesData Eyes;
        HandData Hand;
        OtherPlayer Partner;
        TaskDynamics TaskRelated;

        public DataHolder(ExperimentManager experiment, SyncExperiment sync, EyesData eyes, HandData hand, OtherPlayer partner)
        {
            Experiment = experiment;
            Sync = sync;
            Eyes = eyes;
            Hand = hand;
            Partner = partner;
            TaskRelated = new TaskDynamics(experiment, eyes, hand, partner);
        }

        private string _header;
        public string Header
        {
            get
            {
                if (_header == null)
                {
                    _header += Experiment.CurrentTrialInfo.Header + ",";
                    _header += Eyes.Header+",";
                    _header += Hand.Header+",";
                    _header += Partner.Header+",";
                    _header += "JointAttention,JointGaze,TargetName,TargetPosX,TargetPosY,TargetPosZ,ExpPhase,UnityTime,";
                    _header += TaskRelated.Header;
                }
                return _header;
            }
        }
        public string LatestDataString(Transform target, int phase, float time)
        {
            string output = "";

            output += Experiment.CurrentTrialInfo.ToString() + ",";
            output += Eyes.LatestDataString() + ",";
            output += Hand.LatestDataString() + ",";
            output += Partner.LatestDataString() + ",";
            output += (Sync.jointAttention ? 1 : 0).ToString() + "," + (Sync.jointGaze ? 1 : 0).ToString() + ",";
            output += (target.name) + "," + target.position.x.ToString("F4") + "," + target.position.y.ToString("F4") + "," + target.position.z.ToString("F4") + ",";
            output += phase.ToString() + "," + time.ToString("F4")+",";
            output += TaskRelated.LatestDataString();

            return output;
        }
    }
    /// <summary>
    /// Class containnig task-relevant dynamics.
    /// </summary>
    public class TaskDynamics
    {
        ExperimentManager Experiment;
        EyesData Eyes;
        HandData Finger;
        OtherPlayer Partner;

        public TaskDynamics(ExperimentManager exp, EyesData eyes, HandData hand, OtherPlayer partner)
        {
            Experiment = exp;
            Eyes = eyes;
            Finger = hand;
            Partner = partner;
        }

        IVRGazeData EyeData { get { return Eyes.LatestEyesData; } }
        Vector3 TargetPos { get { return Experiment.TargetObj.position; } }

        float HandToTargetDist
        {
            get
            {
                return Vector3.Distance(Finger.Hand.Position, TargetPos);
            }
        }

        private Vector3 _vector2hand;
        float HandAngleToTarget
        {
            get
            {
                _vector2hand = (TargetPos - Finger.Hand.Position).normalized;
                return Vector3.Angle(_vector2hand, Finger.Hand.transform.right);
            }
        }

        private Vector3 _vector2eyes;
        float GazeAngleToTarget
        {
            get
            {
                _vector2eyes = (TargetPos - EyeData.CombinedGazeRayWorld.origin).normalized;
                return Vector3.Angle(_vector2eyes, EyeData.CombinedGazeRayWorld.direction);
            }
        }

        private Vector3 _vector2partnerHand;
        float GazeAngleToHand
        {
            get
            {
                _vector2eyes = (Partner.Hand.Position - EyeData.CombinedGazeRayWorld.origin).normalized;
                return Vector3.Angle(_vector2eyes, EyeData.CombinedGazeRayWorld.direction);
            }
        }

        private Vector3 _vector2partnerHead;
        float GazeAngleToHead
        {
            get
            {
                _vector2eyes = (Partner.Head.position - EyeData.CombinedGazeRayWorld.origin).normalized;
                return Vector3.Angle(_vector2eyes, EyeData.CombinedGazeRayWorld.direction);
            }
        }

        public string LatestDataString()
        {
            string output = "";

            output += (HandToTargetDist.ToString("F4") + "," + HandAngleToTarget.ToString("F4") + "," + GazeAngleToTarget.ToString("F4") + ",");

            try
            {
                output += (GazeAngleToHand.ToString("F4") + "," + GazeAngleToHead.ToString("F4"));
            }
            catch
            {
                output += ",";
            }

            return output;
        }
        private string _header;
        public string Header
        {
            get
            {
                if (_header == null)
                {
                    _header = "HandToTargetDist,HandAngleToTarget,GazeAngleToTarget,GazeAngleToPartnerHand,GazeAngleToPartnerHead";
                }
                return _header;
            }
        }
    }
    /// <summary>
    /// Class containing functionality to convert player's hand data to string output.
    /// </summary>
    public class HandData
    {
        public Hand Hand;

        public HandData(Hand hand)
        {
            Hand = hand;
        }
        public string LatestDataString()
        {
            string output = "";

            output += (Hand.Position.x.ToString("F4") + "," + Hand.Position.y.ToString("F4") + "," + Hand.Position.z.ToString("F4") + "," + Hand.Rotation.eulerAngles.x.ToString("F4") + "," + Hand.Rotation.eulerAngles.y.ToString("F4") + "," + Hand.Rotation.eulerAngles.z.ToString("F4") + ",");
            output += Hand.ContactName + "," + Hand.PointName;
            return output;
        }
        private string _header;
        public string Header
        {
            get
            {
                if (_header == null)
                {
                    _header = "HandPosX,HandPosY,HandPosZ,HandEulerX,HandEulerY,HandEulerZ,HandContactObj,HandPointObj";
                }
                return _header;
            }
        }
    }
    /// <summary>
    /// Class containing functionality to convert player's eye data to string output.
    /// </summary>
    public class EyesData
    {
        Transform Head;
        public VREyeTracker Eyes;
        public VRGazeTrail Target;
        public EyesData(VREyeTracker eyes, VRGazeTrail gazed)
        {
            Head = GameObject.FindGameObjectWithTag("MainCamera").transform;
            Eyes = eyes;
            Target = gazed;
        }

        public IVRGazeData LatestEyesData
        {
            get { return Eyes.LatestProcessedGazeData; }
        }

        public string LatestDataString()
        {
            IVRGazeData data = LatestEyesData;
            string gazedName = (Target.LatestHitObject == null) ? "" : Target.LatestHitObject.name;
            Vector3 gazedPoint = Target.LatestHitPoint;

            string output = "";

            //Get Head
            output += string.Format("{0},{1},{2},{3},{4},{5},", Head.position.x.ToString("F4"), Head.position.y.ToString("F4"), Head.position.z.ToString("F4"), Head.eulerAngles.x.ToString("F4"), Head.eulerAngles.y.ToString("F4"), Head.eulerAngles.z.ToString("F4"));

            //Get Pose.
            if (data.Pose.Valid) output += string.Format("{0},{1},{2},{3},{4},{5},{6},", data.Pose.Position.x.ToString("F4"), data.Pose.Position.y.ToString("F4"), data.Pose.Position.z.ToString("F4"), data.Pose.Rotation.eulerAngles.x.ToString("F4"), data.Pose.Rotation.eulerAngles.y.ToString("F4"), data.Pose.Rotation.eulerAngles.z.ToString("F4"), (data.Pose.Valid ? 1 : 0).ToString());
            else output += (",,,,,," + (data.Pose.Valid ? 1 : 0).ToString() + ",");

            //Get Gaze and Gazed Object.
            if (data.CombinedGazeRayWorldValid) output += string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},",
                data.Left.GazeOrigin.x.ToString("F4"),
                data.Left.GazeOrigin.y.ToString("F4"),
                data.Left.GazeOrigin.z.ToString("F4"),
                data.Left.GazeDirection.x.ToString("F4"),
                data.Left.GazeDirection.y.ToString("F4"),
                data.Left.GazeDirection.z.ToString("F4"),
                data.Right.GazeOrigin.x.ToString("F4"),
                data.Right.GazeOrigin.y.ToString("F4"),
                data.Right.GazeOrigin.z.ToString("F4"),
                data.Right.GazeDirection.x.ToString("F4"),
                data.Right.GazeDirection.y.ToString("F4"),
                data.Right.GazeDirection.z.ToString("F4"),
                data.CombinedGazeRayWorld.origin.x.ToString("F4"),
                data.CombinedGazeRayWorld.origin.y.ToString("F4"),
                data.CombinedGazeRayWorld.origin.z.ToString("F4"),
                data.CombinedGazeRayWorld.direction.x.ToString("F4"),
                data.CombinedGazeRayWorld.direction.y.ToString("F4"),
                data.CombinedGazeRayWorld.direction.z.ToString("F4"),
                (data.CombinedGazeRayWorldValid ? 1 : 0).ToString(),
                gazedName,
                gazedPoint.x.ToString("F4"),
                gazedPoint.y.ToString("F4"),
                gazedPoint.z.ToString("F4"));
            else output += string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                (data.CombinedGazeRayWorldValid ? 1 : 0).ToString(),
                "",
                "",
                "",
                "");

            output += data.TimeStamp.ToString("F4");

            return output;
        }

        private string _header;
        public string Header
        {
            get
            {
                if (_header != null) return _header;
                else
                {
                    _header = "";
                    _header += "HeadX,HeadY,HeadZ,HeadEulerX,HeadEulerY,HeadEulerZ,";
                    _header += "PoseX,PoseY,PoseZ,PoseEulerX,PoseEulerY,PoseEulerZ,PoseValid,";
                    _header += "LeftGazeOriginX,LeftGazeOriginY,LeftGazeOriginZ,LeftGazeDirectionX,LeftGazeDirectionY,LeftGazeDirectionZ,";
                    _header += "RightGazeOriginX,RightGazeOriginY,RightGazeOriginZ,RightGazeDirectionX,RightGazeDirectionY,RightGazeDirectionZ,";
                    _header += "CombinedGazeOriginX,CombinedGazeOriginY,CombinedGazeOriginZ,CombinedGazeDirectionX,CombinedGazeDirectionY,CombinedGazeDirectionZ,";
                    _header += "GazeValid,GazedObject,GazedPointX,GazedPointY,GazedPointZ,EyeTimeStamp";
                    return _header;
                }
            }
        }
    }
    /// <summary>
    /// Class containing functionality to convert player's partner data to string output.
    /// </summary>
    public class OtherPlayer
    {
        private GameObject Player;
        public SyncActors SyncValues;
        public Transform Head;
        public Hand Hand;
        //public Eyes Eyes;

        public OtherPlayer(GameObject otherPlayer)
        {
            if (otherPlayer.tag != "OtherPlayer") Debug.LogError("Error Creating OtherPlace Class. Expecting GameObject with tag 'OtherPlayer'");
            Player = otherPlayer;
            SyncValues = Player.GetComponent<SyncActors>();
            Head = Player.transform.Find("Camera");
            Hand = Player.transform.Find("Finger").GetComponent<Hand>();
            //Eyes = Player.transform.Find("Avatar").GetComponent<Eyes>();
        }
        public string LatestDataString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30}",
                    Head.position.x.ToString("F4"),
                    Head.position.y.ToString("F4"),
                    Head.position.z.ToString("F4"),
                    Head.eulerAngles.x.ToString("F4"),
                    Head.eulerAngles.y.ToString("F4"),
                    Head.eulerAngles.z.ToString("F4"),
                    SyncValues.gazeLeftEye.x.ToString("F4"),
                    SyncValues.gazeLeftEye.y.ToString("F4"),
                    SyncValues.gazeLeftEye.z.ToString("F4"),
                    SyncValues.rotLeftEye.x.ToString("F4"),
                    SyncValues.rotLeftEye.y.ToString("F4"),
                    SyncValues.rotLeftEye.z.ToString("F4"),
                    SyncValues.gazeRightEye.x.ToString("F4"),
                    SyncValues.gazeRightEye.y.ToString("F4"),
                    SyncValues.gazeRightEye.z.ToString("F4"),
                    SyncValues.rotRightEye.x.ToString("F4"),
                    SyncValues.rotRightEye.y.ToString("F4"),
                    SyncValues.rotRightEye.z.ToString("F4"),
                    SyncValues.gazeObject,
                    SyncValues.gazePoint.x.ToString("F4"),
                    SyncValues.gazePoint.y.ToString("F4"),
                    SyncValues.gazePoint.z.ToString("F4"),
                    Hand.Position.x.ToString("F4"),
                    Hand.Position.y.ToString("F4"),
                    Hand.Position.z.ToString("F4"),
                    Hand.Rotation.eulerAngles.x.ToString("F4"),
                    Hand.Rotation.eulerAngles.y.ToString("F4"),
                    Hand.Rotation.eulerAngles.z.ToString("F4"),
                    SyncValues.touchObject,
                    SyncValues.pointObject,
                    SyncValues.localTime);
        }
        private string _header;
        public string Header
        {
            get
            {
                if (_header == null)
                {
                    _header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30}",
                        "PartnerHeadPosX",
                        "PartnerHeadPosY",
                        "PartnerHeadPosZ",
                        "PartnerHeadEulerX",
                        "PartnerHeadEulerY",
                        "PartnerHeadEulerZ",
                        "PartnerLeftEyeGazeVectorX",
                        "PartnerLeftEyeGazeVectorY",
                        "PartnerLeftEyeGazeZ",
                        "PartnerLeftEyeEulerX",
                        "PartnerLeftEyeEulerY",
                        "PartnerLeftEyeEulerZ",
                        "PartnerRightEyeGazeVectorX",
                        "PartnerRightEyeGazeVectorY",
                        "PartnerRightEyeGazeVectorZ",
                        "PartnerRightEyeEulerX",
                        "PartnerRightEyeEulerY",
                        "PartnerRightEyeEulerZ",
                        "PartnerGazedObject",
                        "PartnerGazedPointX",
                        "PartnerGazedPointY",
                        "PartnerGazedPointZ",
                        "PartnerHandPosX",
                        "PartnerHandPosY",
                        "PartnerHandPosZ",
                        "PartnerHandEulerX",
                        "PartnerHandEulerY",
                        "PartnerHandEulerZ",
                        "PartnerHandContactName",
                        "PartnerPointName",
                        "PartnerLocalTime");
                }
                return _header;
            }
        }
    }
    #endregion
}