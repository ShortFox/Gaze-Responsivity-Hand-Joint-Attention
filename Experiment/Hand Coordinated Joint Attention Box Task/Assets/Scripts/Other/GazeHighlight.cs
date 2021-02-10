namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Tobii.Research.Unity;

    public class GazeHighlight : MonoBehaviour
    {

        [SerializeField] Material NoGazeColor;
        [SerializeField] Material GazeColor;

        private bool _isViewed;
        private bool IsViewed
        {
            get { return _isViewed; }
            set
            {
                if (_isViewed != value)
                {
                    if (value) ChangeColor(GazeColor);
                    else ChangeColor(NoGazeColor);
                    _isViewed = value;
                }
            }
        }

        private VRGazeTrail _gazed;
        private VRGazeTrail GAZED
        {
            get
            {
                if (_gazed == null)
                {
                    _gazed = VRGazeTrail.Instance;
                }
                return _gazed;
            }
        }

        void Start()
        {
            ChangeColor(NoGazeColor);
        }
        private void Update()
        {
            if (GAZED != null)
            {
                if (GAZED.LatestHitObject == this.transform) IsViewed = true;
                else IsViewed = false;
            }
        }
        void ChangeColor(Material NewColor)
        {
            this.GetComponent<Renderer>().material = NewColor;
        }
    }
}

