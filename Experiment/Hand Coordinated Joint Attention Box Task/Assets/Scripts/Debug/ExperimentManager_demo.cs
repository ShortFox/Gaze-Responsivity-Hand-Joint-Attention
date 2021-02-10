namespace MQ.MultiAgent
{
    using System.Collections;
    using UnityEngine;
    using Tobii.Research.Unity;

    public class ExperimentManager_demo : MonoBehaviour
    {
        [SerializeField] GameObject FixationObject;
        [SerializeField] GameObject[] TaskObjects;
        [SerializeField] Material UnselectedColor;
        [SerializeField] Material SelectedColor;

        public static ExperimentManager_demo Instance { get; private set; }

        private VRGazeTrail _gaze;

        private bool _isRunning;

        private void Awake()
        {
            Instance = this;
            _isRunning = false;
        }
        void Start()
        {
            _gaze = VRGazeTrail.Instance;

            TurnOff(TaskObjects);
            TurnOff(FixationObject);
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!_isRunning)
                {
                    RunExperiment();
                }
            }
        }
        void RunExperiment()
        {
            _isRunning = true;
            StartCoroutine(LoopTrials());
            _isRunning = false;
        }

        //Debug function for demoing.
        IEnumerator LoopTrials()
        {
            TurnOn(TaskObjects);

            while (true)
            {
                TurnOn(FixationObject);

                //for Debug ....
                FixationObject.transform.localPosition = new Vector3(0+Random.Range(-0.4f, 0.4f), FixationObject.transform.localPosition.y, FixationObject.transform.localPosition.z);

                //Wait for raycast hit on fixation cross
                //See if there is something preferable to keeping a bool _proceed;
                yield return new WaitUntil(() => _gaze.LatestHitObject == FixationObject.transform);

                TurnOff(FixationObject);
                GameObject obj = SelectObject(TaskObjects);
                ChangeColor(obj, SelectedColor);

                //Wait for raycast hit on selected object.
                yield return new WaitUntil(() => _gaze.LatestHitObject == obj.transform);
                ChangeColor(obj, UnselectedColor);
                yield return new WaitForSeconds(2f);
                
                yield return null;
            }
        }

        bool EventHappened()
        {
            return true;
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
        GameObject SelectObject(GameObject[] objects)
        {
            int indx = UnityEngine.Random.Range(0, objects.Length);
            return objects[indx];
        }
        void ChangeColor(GameObject obj, Material NewColor)
        {
           obj.GetComponent<Renderer>().material = NewColor;
        }
    }
}

