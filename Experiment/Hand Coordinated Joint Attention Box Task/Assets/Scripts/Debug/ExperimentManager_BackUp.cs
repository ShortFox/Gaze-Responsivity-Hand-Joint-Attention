/*
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
    public class ExperimentManager_BackUp : NetworkBehaviour
    {
        public static ExperimentManager_BackUp Instance { get; private set; }
        private ExpInitializer INIT;
        private DataWriter DATA;
        private VREyeTracker EYES;
        private VRGazeTrail GAZED;
        private Hand HAND;

        private bool _isRunning;        //Flag indicating if experiment is being conducted.
        public bool IsRunning { get { return _isRunning; } }

        private bool _saveData;
        private bool SaveData
        {
            get { return _saveData; }
            set
            {
                if (_saveData)
                {
                    if (value != _saveData) DATA.FlushBuffer(_trialNumber);     //If _saveData is being set to False from True, flush buffer to file.
                }
                else if (!_saveData)
                {
                    if (value != _saveData) _timeSinceWrite = 0;
                }
                _saveData = value;
            }
        }

        #region Data Structures
        private TrialInfo _trialInfo;
        #endregion

        #region CommonTask Objects
        [SerializeField] GameObject FixationObject;
        [SerializeField] GameObject[] Blocks;
        [SerializeField] Text[] BlockFaces;
        [SerializeField] Material UnselectedColor;
        [SerializeField] Material CueColor;
        [SerializeField] Material IncorrectColor;
        [SerializeField] Material CorrectColor;
        #endregion

        #region ReachTask Objects
        [SerializeField] GameObject HandInitCube;
        [SerializeField] GameObject HandObj;
        #endregion

        #region Experiment Related and Misc.
        private float _interTrialPeriod = 1f;                                                       //Time before new trial is presented.
        private float _trialFeedbackLength = 0.5f;                                                  //Feedback time once participant makes response.
        private float _trialnitLag { get { return UnityEngine.Random.Range(1f, 2f); } }             //Time that needs to pass before trial initiates.
        private float _handStayTime = 2f;                                                           //Number of seconds participant's hand needs to stay in Init Cube position.
        private float _intentDistance = 0.05f;                                                      //Distance from Hand's initial position in ReachTask to register as intended reach.
        private float _sampleTime = (1 / 90f);                                                        //Sample rate for writing.
        private float _timeSinceWrite;                                                              //Time since last datawrite.
        private int _expPhase;                                                                      //Task-specific phase flag to help decipher parts of task.
        private GameObject _targetObj;                                                              //Target object for trial.
        private Dictionary<InitHeader, Text> _blockFaceDict;                                        //Dictionary for easy access to change block faces.
        private List<TrialInfo> Trials;                                                             //List that stores shuffled trials.
        private int _trialNumber;                                                                   //Trial Number.
        private int _errorNumber;                                                                   //Number of trials resulting in incorrect response;

        public int SubjectNum;                                                                       //Participant subject num.
        #endregion

        private void Awake()
        {
            Instance = this;
            //_sampleTime = Time.fixedDeltaTime;
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
            INIT = ExpInitializer.Instance;     //Initializes, read, and verify INIT file.
            DATA = new DataWriter(SubjectNum);
            EYES = VREyeTracker.Instance;
            GAZED = VRGazeTrail.Instance;
            HAND = HandObj.GetComponent<Hand>();

            TurnOff(Blocks);
            TurnOff(FixationObject);
            TurnOff(HandInitCube);

            _errorNumber = 0;

        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!_isRunning && INIT.IsAvailable)
                {
                    _expPhase = 0;
                    StartCoroutine(RunExperiment());
                }
            }
            if (_isRunning && SaveData)
            {
                //Add first datasample
                if ((DATA.Buffer.Count == 0) && (_timeSinceWrite == 0))
                {
                    //DATA.Buffer.Add(new DataSample(_trialInfo, new HandInfo(HAND), new EyesInfo(EYES.LatestGazeData), new GazedInfo(GAZED), _targetObj, _expPhase, Time.time));
                }
                //Else add datasample at samplerate
                else if (_timeSinceWrite > (_sampleTime))
                {
                    //DATA.Buffer.Add(new DataSample(_trialInfo, new HandInfo(HAND), new EyesInfo(EYES.LatestGazeData), new GazedInfo(GAZED), _targetObj, _expPhase, Time.time));
                    _timeSinceWrite = _timeSinceWrite - _sampleTime;
                }
                else
                {
                    _timeSinceWrite += Time.deltaTime;
                }
            }
        }
        IEnumerator RunExperiment()
        {
            _isRunning = true;
            _trialNumber = 1;

            if (Trials.Count > 0)
            {
                INIT.SubmitError("Trial List already has items");
            }
            else
            {
                yield return StartCoroutine(GetTrialListAndShuffle());
            }

            while (true)
            {
                if (Trials.Count > 0 && INIT.IsAvailable)
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
                            yield return JointTask();
                            break;
                        default:
                            INIT.SubmitError("Error selecting task from Trial List");     //Make INIT file unavailable.
                            break;
                    }
                    yield return new WaitForSeconds(_interTrialPeriod);
                    Trials.RemoveAt(0);
                    _trialNumber++;
                }
                else break;
            }
            _isRunning = false;
            yield return null;
        }

        IEnumerator GetTrialListAndShuffle()
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
                yield return null;
            }

            saccadeTrials.Shuffle();
            reachTrials.Shuffle();
            jointTrials.Shuffle();

            Trials.AddRange(saccadeTrials);
            Trials.AddRange(reachTrials);
            Trials.AddRange(jointTrials);
        }

        #region Tasks
        IEnumerator SaccadeTask()
        {
            //Turn on relevant game objects.
            TurnOn(FixationObject);
            TurnOn(Blocks);

            //Select target object.
            _targetObj = Blocks[_trialInfo.TargetLocation - 1];

            //Wait until participant looks at fixation-object for required amount of time
            float timeElapsed = 0;
            while (timeElapsed < _trialnitLag)
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
                if (GAZED.LatestHitObject.tag == "BlockObjects")
                {
                    gazedObject = GAZED.LatestHitObject.gameObject;
                    break;
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

            //Select target object.
            _targetObj = Blocks[_trialInfo.TargetLocation - 1];

            //Wait until participant places hand in initial position for required amount of time
            float timeElapsed = 0;
            while (timeElapsed < _handStayTime)
            {
                if (HAND.ContactObject == HandInitCube)
                {
                    timeElapsed += Time.deltaTime;
                }
                else { timeElapsed = 0; }
                yield return null;
            }

            TurnOn(FixationObject);
            //Wait until participant looks at fixation-object for required amount of time
            timeElapsed = 0;
            while (timeElapsed < _trialnitLag)
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

            Vector3 initPos = HAND.Position;
            while (true)
            {
                if (Vector3.Distance(initPos, HAND.Position) > _intentDistance)
                {
                    _expPhase = 1;
                    break;
                }
                yield return null;
            }


            //Wait for hand hit on a block object.
            GameObject contactObject = null;
            while (true)
            {
                if (HAND.ContactObject.tag == "BlockObjects")
                {
                    contactObject = GAZED.LatestHitObject.gameObject;
                    break;
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
            TurnOff(HandInitCube);
        }
        IEnumerator JointTask()
        {
            //Turn on relevant game objects.
            TurnOn(Blocks);
            TurnOn(HandInitCube);

            //Select target object.
            _targetObj = Blocks[_trialInfo.TargetLocation - 1];

            //Wait until participant places hand in initial position for required amount of time
            float timeElapsed = 0;
            while (timeElapsed < _handStayTime)
            {
                if (HAND.ContactObject == HandInitCube)
                {
                    timeElapsed += Time.deltaTime;
                }
                else { timeElapsed = 0; }
                yield return null;
            }

            TurnOn(FixationObject);
            //Wait until participant looks at fixation-object for required amount of time
            timeElapsed = 0;
            while (timeElapsed < _trialnitLag)
            {
                if (GAZED.LatestHitObject == FixationObject.transform)
                {
                    timeElapsed += Time.deltaTime;
                }
                else { timeElapsed = 0; }
                yield return null;
            }
            //Set Numbers on blocks.
            yield return StartCoroutine(SetBlockFaces());

            _expPhase = 0;
            SaveData = true;

            Vector3 initPos = HAND.Position;
            while (true)
            {
                if (Vector3.Distance(initPos, HAND.Position) > _intentDistance)
                {
                    _expPhase = 1;
                    break;
                }
                yield return null;
            }

            //Wait for hand hit on a block object.
            GameObject contactObject = null;
            while (true)
            {
                if (HAND.ContactObject.tag == "BlockObjects")
                {
                    contactObject = GAZED.LatestHitObject.gameObject;
                    break;
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
            TurnOff(HandInitCube);
        }
        #endregion

        #region Helper Functions
        /// <summary>
        /// This function adds current _trialInfo to end of task-list, given by task_name. Note, trial will still need to be removed from beginning of list.
        /// </summary>
        /// <param name="task_name">Task name to append incorrect trial.</param>
        private void SendTrialToEnd(string task_name)
        {
            switch (task_name)
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
            foreach (var item in _blockFaceDict)
            {
                item.Value.text = _trialInfo.TrialString[(int)item.Key];
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

        //This might be more appropriate on experimental objects. Unclear.
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
*/