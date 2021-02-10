namespace MQ.MultiAgent
{
    using UnityEngine;
    using UnityEngine.UI;
    using Tobii.Research.Unity;

    public class HMDPositionPlacement : EyePositionGuide
    {
        [SerializeField] protected RectTransform TargetLeft;
        [SerializeField] protected RectTransform TargetRight;

        [SerializeField] RectTransform PupilLeft;
        [SerializeField] RectTransform PupilRight;

        [SerializeField] Text Status;

        private Image _leftImage;
        private Image _rightImage;
        private Vector2 _sizeOfparent;

        protected override void Start()
        {
            base.Start();
            _leftImage = PupilLeft.GetComponent<Image>();
            _rightImage = PupilRight.GetComponent<Image>();
            _sizeOfparent = PupilLeft.parent.GetComponent<RectTransform>().sizeDelta;
            _sizeOfparent.y = -1 * _sizeOfparent.y;
            SetParent();
        }
        //Find VR Headset and set Canvas to it.
        protected virtual void SetParent()
        {
            transform.parent = VRUtility.EyeTrackerOriginVive;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = new Vector3(0, 0, 1.5f);
        }
        protected override void Update()
        {
            base.Update();
            //UpdateLensSeperation();
            UpdatePlacement();
        }
        protected virtual void UpdateLensSeperation()
        {
            if (Time.frameCount % 45 == 0)
            {
                var hmdLcsInMM = VRUtility.LensCupSeparation * 1000f;
                var lHPos = new Vector3(-hmdLcsInMM, 0);
                var rHPos = new Vector3(hmdLcsInMM, 0);

                TargetLeft.localPosition = lHPos;
                TargetRight.localPosition = rHPos;
            }
        }
        private void UpdatePlacement()
        {
            PupilLeft.anchoredPosition = Vector2.Scale(LeftPupilXY, _sizeOfparent);
            PupilRight.anchoredPosition = Vector2.Scale(RightPupilXY, _sizeOfparent);

            //var leftDistance = Vector2.Distance(LensCenter, LeftPupilXY);
            //var rightDistance = Vector2.Distance(LensCenter, RightPupilXY);

            _leftImage.color = LeftColor;
            _rightImage.color = RightColor;

            // Info to the user
            Status.text = PlacementStatus;
        }
    }

}

