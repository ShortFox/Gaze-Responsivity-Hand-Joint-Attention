namespace MQ.MultiAgent
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using Tobii.Research.Unity;
    using UnityEngine.Networking;

    [RequireComponent(typeof(NetworkIdentity))]
    public class ExperimentManager : NetworkBehaviour
    {
        public static ExperimentManager Instance { get; private set; }
        private ExpInitializer INIT;
        private DataWriter WRITER;
        private DataHolder DATA;
        private VREyeTracker EYES;
        private VRGazeTrail GAZED;
        private Hand HAND;
        private SyncExperiment SYNCEXP;
        private SyncActors SYNCACT;

        private bool _isRunning;        //Flag indicating if experiment is being conducted.
        private bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                if (value != _isRunning)
                {
                    if (value) UI_Experimenter.Instance.MirrorsActive = false;
                    else UI_Experimenter.Instance.MirrorsActive = true;
                    _isRunning = value;
                }
            }
        }

        private bool _saveData;
        private bool SaveData
        {
            get { return _saveData; }
            set
            {
                if (_saveData)
                {
                    if (value != _saveData)
                    {
                        WRITER.Buffer.Add(DATA.LatestDataString(_targetObj.transform, _expPhase, Time.timeSinceLevelLoad));     //Add final data to determine what object player looked/touched.
                        WRITER.FlushBuffer(DATA, _trialInfo, _trialNumber);                                                     //If _saveData is being set to False from True, flush buffer to file.
                    }
                }
                else if (!_saveData)
                {
                    if (value != _saveData)
                    {
                        _timeSinceWrite = 0;
                        WRITER.Buffer.Add(DATA.LatestDataString(_targetObj.transform, _expPhase, Time.timeSinceLevelLoad));     //Add first data row.
                    }
                }
                _saveData = value;
            }
        }

        public Transform TargetObj { get { return _targetObj.transform; } }

        #region Data Structures
        private TrialInfo _trialInfo;
        public TrialInfo CurrentTrialInfo
        {
            get { return _trialInfo; }
        }
        #endregion

        private GameObject Player;

        #region CommonTask Objects
        [SerializeField] GameObject FixationObject;
        [SerializeField] GameObject[] Blocks;
        [SerializeField] Text[] BlockFaces;
        private string _blockObjectsTag = "BlockObjects";
        [SerializeField] Material UnselectedColor;
        [SerializeField] Material CueColor;
        [SerializeField] Material IncorrectColor;
        [SerializeField] Material CorrectColor;
        [SerializeField] Material Black;
        #endregion

        #region ReachTask Objects
        private GameObject HandInitCube;
        private GameObject HandObj;
        #endregion

        #region Joint Task Variables
        public bool JointProceed;        //Proceed flag to start trial.
        public int SyncFlag;        //Int variable that keeps track when Player states are synchronized.
        #endregion

        #region Experiment Related and Misc.
        private float _interTrialPeriod = 1f;                                                       //Time before new trial is presented.
        private float _trialFeedbackLength = 0.5f;                                                  //Feedback time once participant makes response.
        private float _trialInitLag { get { return UnityEngine.Random.Range(1f, 2f); } }            //Time that needs to pass before trial initiates.
        private float _intentDistance = 0.05f;                                                      //Distance from Hand's initial position in ReachTask to register as intended reach.
        private float _writeRate = (1/60f);                                                         //Sample rate for writing.
        private float _timeSinceWrite;                                                              //Time since last datawrite.
        private int _expPhase;                                                                      //Task-specific phase flag to help decipher parts of task.
        private GameObject _targetObj;                                                              //Target object for trial.
        private Dictionary<InitHeader, Text> _blockFaceDict;                                        //Dictionary for easy access to change block faces.
        private List<TrialInfo> Trials;                                                             //List that stores shuffled trials.
        private int _trialNumber;                                                                   //Trial Number.
        private int _errorNumber;                                                                   //Number of trials resulting in incorrect response;
        #endregion

        private void Awake()
        {
            Instance = this;
            Trials = new List<TrialInfo>();
            _blockFaceDict = new Dictionary<InitHeader, Text>
            {
                {InitHeader.B1_val_A,BlockFaces[0]},
                {InitHeader.B2_val_A,BlockFaces[1]},
                {InitHeader.B3_val_A,BlockFaces[2]},
                {InitHeader.B3_val_B,BlockFaces[3]},
                {InitHeader.B2_val_B,BlockFaces[4]},
                {InitHeader.B1_val_B,BlockFaces[5]},
            };
        }
        private void Start()
        {
            Player = GameObject.FindGameObjectWithTag("Player");
            HandInitCube = Player.transform.Find("FingerCubeReady").gameObject;
            HandObj = Player.transform.Find("Finger").gameObject;

            INIT = ExpInitializer.Instance;     //Initializes, read, and verify INIT file.
            SYNCEXP = SyncExperiment.Instance;
            WRITER = new DataWriter(SYNCEXP.subjectNum, Player.name);
            EYES = VREyeTracker.Instance;
            GAZED = VRGazeTrail.Instance;
            HAND = HandObj.GetComponent<Hand>();
            SYNCACT = HAND.transform.parent.GetComponent<SyncActors>();

            //TurnOff(Blocks);
            TurnOff(FixationObject);
            TurnOff(HandInitCube);

            if (INIT.IsAvailable) GetTrialListAndShuffle();
            _errorNumber = 0;
            _trialNumber = 1;
            _expPhase = 0;
            JointProceed = false;
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!IsRunning && INIT.IsAvailable)
                {
                    if (!SYNCEXP.networkSync) StartCoroutine(RunSoloExperiment());
                }
            }
            if (!IsRunning && INIT.IsAvailable && SYNCEXP.networkSync)
            {
                StartCoroutine(RunJointExperiment());
            }

            if (IsRunning && SaveData)
            {
                _timeSinceWrite += Time.deltaTime;

                if (_timeSinceWrite >=_writeRate)
                {
                    WRITER.Buffer.Add(DATA.LatestDataString(_targetObj.transform, _expPhase, Time.timeSinceLevelLoad));
                    _timeSinceWrite = _timeSinceWrite - _writeRate;
                }                
            }
        }
        IEnumerator RunSoloExperiment()
        {
            IsRunning = true;
            UI_Experimenter.Instance.MirrorsActive = false;

            while (true)
            {
                if (Trials.Count>0 && INIT.IsAvailable && !SYNCACT.readyState)
                {
                    _trialInfo = Trials[0];
                    switch (_trialInfo.Task)
                    {
                        case "solo_saccade":
                            yield return SaccadeTask();
                            break;
                        case "solo_point":
                            yield return ReachTask();
                            break;
                        case "joint":
                            SYNCACT.readyState = true;
                            Debug.Log("Ready for Joint");
                            break;
                        default:
                            INIT.SubmitError("Error selecting task from Trial List");     //Make INIT file unavailable.
                            break;
                    }
                    if (SYNCACT.readyState) break;

                    yield return new WaitForSeconds(_interTrialPeriod);

                    //Peek at next Trial to determine if task is changing. If task is changing, break out of Coroutine.
                    if (Trials.Count > 1)
                    {
                        if (_trialInfo.Task != Trials[1].Task)
                        {
                            Trials.RemoveAt(0);
                            _trialNumber++;
                            UI_Experimenter.Instance.MirrorsActive = true;
                            TurnOn(Blocks);
                            TurnOn(HandInitCube);
                            break;
                        }
                    }
                    Trials.RemoveAt(0);
                    _trialNumber++;
                }
                else break;
            }
            IsRunning = false;
            yield return null;
        }

        IEnumerator RunJointExperiment()
        {
            IsRunning = true;
            UI_Experimenter.Instance.MirrorsActive = false;
            _trialNumber = 1001;        //Trial Number increased by 1000 to ensure trial syncing.

            while (true)
            {
                if (SYNCEXP.networkSync)
                {
                    SyncFlag = 0;
                    SYNCEXP.SyncTrialInfo(_trialNumber, Trials[0]);

                    while (!JointProceed)
                    {
                        yield return null;
                    }
                    JointProceed = false;
                    _trialNumber = SYNCEXP.trialNumber;

                    _trialInfo.Task = SYNCEXP.task;
                    _trialInfo.TrialID = SYNCEXP.trialID;
                    _trialInfo.TargetLocation = SYNCEXP.targetLocation;
                    _trialInfo.RoleA = SYNCEXP.roleA;
                    _trialInfo.B1_Val_A = SYNCEXP.b1_val_A;
                    _trialInfo.B2_Val_A = SYNCEXP.b2_val_A;
                    _trialInfo.B3_Val_A = SYNCEXP.b3_val_A;
                    _trialInfo.RoleB = SYNCEXP.roleB;
                    _trialInfo.B3_Val_B = SYNCEXP.b3_val_B;
                    _trialInfo.B2_Val_B = SYNCEXP.b2_val_B;
                    _trialInfo.B1_Val_B = SYNCEXP.b1_val_B;

                    switch (_trialInfo.Task)
                    {
                        case "joint":
                            yield return JointTask();       //Will have to find ways to organize the Joint Task.
                            break;
                        default:
                            INIT.SubmitError("Error selecting task from Trial List");     //Make INIT file unavailable.
                            break;
                    }
                    yield return new WaitForSeconds(_interTrialPeriod);

                    Debug.Log("End of trial");
                    if (!isServer) continue;
                    Debug.Log("I should not be here");

                    Trials.RemoveAt(0);
                    _trialNumber++;
                }
                else break;
            }
            IsRunning = false;
            yield return null;
        }

        #region Tasks
        IEnumerator SaccadeTask()
        {
            //Turn on relevant game objects.
            TurnOn(FixationObject);
            FixationObject.transform.position = SYNCACT.partnerPlayer.SyncValues.calibrationPoint;
            TurnOn(Blocks);

            //Select target object.
            _targetObj = Blocks[_trialInfo.TargetLocation - 1];

            //Wait until participant looks at fixation-object for required amount of time
            float timeElapsed = 0;
            while (timeElapsed < _trialInitLag)
            {
                if (GAZED.LatestHitObject == FixationObject.transform)
                {
                    timeElapsed += Time.deltaTime;
                }
                else { timeElapsed = 0; }
                yield return null;
            }
            _expPhase = 0;
            SaveData = true;

            //Change target object color
            ChangeColor(_targetObj, CueColor);

            //Wait for raycast hit on a block object.
            GameObject gazedObject = null;
            while (true)
            {
                if (GAZED.LatestHitObject != null)
                {
                    if (GAZED.LatestHitObject.tag == _blockObjectsTag)
                    {
                        gazedObject = GAZED.LatestHitObject.gameObject;
                        break;
                    }
                }
                yield return null;
            }
            SaveData = false;

            //Provide feedback to participant.
            if (gazedObject == _targetObj)
            {
                ChangeColor(gazedObject, CorrectColor);
            }
            else //Object is incorrect. Add trial to end of task list.
            {
                ChangeColor(_targetObj, UnselectedColor);
                ChangeColor(gazedObject, IncorrectColor);
                SendTrialToEnd(_trialInfo.Task);
            }

            yield return new WaitForSeconds(_trialFeedbackLength);

            //Reset objects.
            ChangeColor(gazedObject, UnselectedColor);
            TurnOff(FixationObject);
            TurnOff(Blocks);
        }
        IEnumerator ReachTask()
        {
            //Turn on relevant game objects.
            TurnOn(Blocks);
            TurnOn(HandInitCube);
            ChangeColor(HandInitCube, Black);

            //Select target object.
            _targetObj = Blocks[_trialInfo.TargetLocation - 1];

            //Wait until participant places hand in initial position for required amount of time and looks at the fixation object.
            float timeElapsed = 0;
            while (timeElapsed < _trialInitLag)
            {
                //if ((HAND.ContactObject == HandInitCube) && (GAZED.LatestHitObject == HandInitCube.transform))
                if (HAND.ContactObject == HandInitCube)
                {
                    timeElapsed += Time.deltaTime;
                }
                else { timeElapsed = 0; }
                yield return null;
            }

            ChangeColor(HandInitCube, CorrectColor);
            TurnOn(FixationObject);

            while (GAZED.LatestHitObject != FixationObject.transform)
            {
                yield return null;
            }
            /*
            //Wait until participant looks at fixation-object for required amount of time
            timeElapsed = 0;
            while (timeElapsed < _trialInitLag)
            {
                if (GAZED.LatestHitObject == FixationObject.transform)
                {
                    timeElapsed += Time.deltaTime;
                }
                else { timeElapsed = 0; }
                yield return null;
            }
            */

            _expPhase = 0;
            SaveData = true;

            //Change target object color
            ChangeColor(_targetObj, CueColor);

            //Wait for hand hit on a block object.
            Vector3 initPos = HAND.Position;
            GameObject contactObject = null;
            while (true)
            {
                if (_expPhase == 0)
                {
                    if (Vector3.Distance(initPos, HAND.Position) > _intentDistance) _expPhase = 1;
                }

                if (HAND.ContactObject != null)
                {
                    if (HAND.ContactObject.tag == _blockObjectsTag)
                    {
                        contactObject = HAND.ContactObject;
                        break;
                    }
                }
                yield return null;
            }
            SaveData = false;

            //Provide feedback to participant.
            if (contactObject == _targetObj)
            {
                ChangeColor(contactObject, CorrectColor);
            }
            else
            {
                ChangeColor(_targetObj, UnselectedColor);
                ChangeColor(contactObject, IncorrectColor);
                SendTrialToEnd(_trialInfo.Task);
            }

            yield return new WaitForSeconds(_trialFeedbackLength);

            //Reset objects.
            ChangeColor(contactObject, UnselectedColor);
            TurnOff(FixationObject);
            TurnOff(Blocks);
            ChangeColor(HandInitCube, Black);
            TurnOff(HandInitCube);
        }
        IEnumerator JointTask()
        {
            Debug.Log("Start Joint Task");
            int syncState = SyncFlag;

            //Wait for joint gaze
            /*
            while (!SYNCEXP.jointGaze)
            {
                yield return null;
            }
            */

            //Turn on relevant game objects.
            TurnOn(Blocks);
            TurnOn(HandInitCube);

            //Select target object.
            _targetObj = Blocks[_trialInfo.TargetLocation - 1];

            //Wait until participant places hand in initial position for required amount of time.
            SYNCEXP.SyncTouchTime(HandInitCube.transform, _trialInitLag);
            while (syncState == SyncFlag)
            {
                yield return null;
            }
            ChangeColor(HandInitCube, CorrectColor);

            while (!SYNCEXP.jointGaze)
            {
                yield return null;
            }
            /*
            syncState = SyncFlag;       //See if this works.

            //Wait Until participants look at each other.
            SYNCEXP.SyncJointGazeTime(_trialInitLag);
            while (syncState == SyncFlag)
            {
                yield return null;
            }
            */

            //Set Numbers on blocks.
            yield return StartCoroutine(SetBlockFaces());

            _expPhase = 0;
            SaveData = true;

            //Wait for hand hit on a block object.
            Vector3 initPos = HAND.Position;
            GameObject contactObject = null;
            while (true)
            {
                if (_expPhase == 0)
                {
                    if (Vector3.Distance(initPos, HAND.Position) > _intentDistance) _expPhase = 1;
                }

                string touchObj = SYNCEXP.touchObjName;
                GameObject obj = GameObject.Find(touchObj);

                if (obj != null)
                {
                    if (obj.tag == _blockObjectsTag)
                    {
                        contactObject = obj;
                        break;
                    }
                }
                yield return null;
            }
            SaveData = false;

            //Provide feedback to participant.
            if (contactObject == _targetObj)
            {
                ChangeColor(contactObject, CorrectColor);
            }
            else
            {
                ChangeColor(contactObject, IncorrectColor);
                SendTrialToEnd(_trialInfo.Task);
            }

            yield return new WaitForSeconds(_trialFeedbackLength);

            //Reset objects.
            ChangeColor(contactObject, UnselectedColor);
            yield return StartCoroutine(ResetBlockFaces());
            TurnOff(FixationObject);
            TurnOff(Blocks);
            ChangeColor(HandInitCube, Black);
            TurnOff(HandInitCube);
        }
        #endregion

        #region Helper Functions
        public void InitializeDataHolder()
        {
            DATA = new DataHolder(this, SYNCEXP, new EyesData(EYES, GAZED), new HandData(HAND), SYNCACT.partnerPlayer);
        }
        /// <summary>
        /// This function adds current _trialInfo to end of task-list, given by task_name. Note, trial will still need to be removed from beginning of list.
        /// </summary>
        /// <param name="task_name">Task name to append incorrect trial.</param>
        private void SendTrialToEnd(string task_name)
        {
            switch(task_name)
            {
                case "joint":   //Add at end of List
                    Trials.Add(_trialInfo);
                    break;
                default:        //Otherwise add after last instance of task_name that appears in list.
                    int end_indx = 
                    Trials.FindLastIndex(
                        delegate (TrialInfo trial)
                        {
                            return trial.Task == task_name;
                        });

                    Trials.Insert(end_indx + 1, _trialInfo);
                    break;
            }
            _errorNumber++;
        }
        IEnumerator SetBlockFaces()
        {
            string[] trialArray = _trialInfo.CreateTrialString();
            foreach (var item in _blockFaceDict)
            {
                item.Value.text = trialArray[(int)item.Key];
            }
            yield return null;
        }
        IEnumerator ResetBlockFaces()
        {
            foreach (var item in _blockFaceDict.Values)
            {
                item.text = "";
            }
            yield return null;
        }
        void GetTrialListAndShuffle()
        {
            List<TrialInfo> saccadeTrials = new List<TrialInfo>();
            List<TrialInfo> reachTrials = new List<TrialInfo>();
            List<TrialInfo> jointTrials = new List<TrialInfo>();

            while (!INIT.IsFinished)
            {
                TrialInfo trial = new TrialInfo(INIT.CurrentItem);
                switch (trial.Task)
                {
                    case "solo_saccade":
                        saccadeTrials.Add(trial);
                        break;
                    case "solo_point":
                        reachTrials.Add(trial);
                        break;
                    case "joint":
                        jointTrials.Add(trial);
                        break;
                    default:
                        INIT.SubmitError("Error selecting task");     //Make INIT file unavailable.
                        break;
                }
                INIT.NextItem();
            }

            saccadeTrials.Shuffle();
            reachTrials.Shuffle();
            jointTrials.Shuffle();

            Trials.AddRange(saccadeTrials);
            Trials.AddRange(reachTrials);
            Trials.AddRange(jointTrials);
        }
        void TurnOff(GameObject[] objects)
        {
            foreach (GameObject obj in objects) obj.SetActive(false);
        }
        void TurnOff(GameObject obj)
        {
            obj.SetActive(false);
        }
        void TurnOn(GameObject[] objects)
        {
            foreach (GameObject obj in objects) obj.SetActive(true);
        }
        void TurnOn(GameObject obj)
        {
            obj.SetActive(true);
        }
        void ChangeColor(GameObject obj, Material NewColor)
        {
            obj.GetComponent<Renderer>().material = NewColor;
        }
        #endregion
    }
}