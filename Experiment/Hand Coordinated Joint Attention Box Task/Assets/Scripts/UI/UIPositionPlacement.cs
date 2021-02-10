namespace MQ.MultiAgent
{
    using UnityEngine;
    using UnityEngine.UI;
    using Tobii.Research.Unity;

    public class UIPositionPlacement : HMDPositionPlacement
    {
        protected override void SetParent()
        {
            return;
        }
        protected override void UpdateLensSeperation()
        {
            //Work on later.
            /*
            if (Time.frameCount % 45 == 0)
            {
                var hmdLcsInMM = VRUtility.LensCupSeparation * 1000f;
                var lHPos = new Vector3(-hmdLcsInMM, 0);
                var rHPos = new Vector3(hmdLcsInMM, 0);

                TargetLeft.localPosition = lHPos;
                TargetRight.localPosition = rHPos;
            }
            */
        }
    }
}

