//-----------------------------------------------------------------------
// Adapted from Tobii.Research.Unity.VRPositioningGuide
//-----------------------------------------------------------------------

namespace MQ.MultiAgent
{
    using UnityEngine;
    using UnityEngine.UI;
    using Tobii.Research.Unity;

    public class EyePositionGuide : MonoBehaviour
    {
        //Color For Pupils. _center is used to calculate Color.
        private Color _leftColor;
        protected Color LeftColor { get { return _leftColor; } }
        private Color _rightColor;
        protected Color RightColor { get { return _rightColor; } }
        private Vector2 _center;
        protected Vector3 LensCenter { get { return _center; } }

        private string _status;
        protected string PlacementStatus { get { return _status; } }

        //Check if eyeTracker is enabled and if calibration has already been performed.
        private VREyeTracker _eyeTracker;
        private VRCalibration _calibration;

        //Left and Right pupil position.
        private Vector2 _leftPupilXY;
        protected Vector2 LeftPupilXY { get { return _leftPupilXY; } }
        private Vector2 _rightPupilXY;
        protected Vector2 RightPupilXY { get { return _rightPupilXY; } }

        protected virtual void Start()
        {
            _eyeTracker = VREyeTracker.Instance;
            _calibration = VRCalibration.Instance;
            _center = new Vector2(0.5f, 0.5f);
        }
        protected virtual void Update()
        {
            if (_eyeTracker == null || !_eyeTracker.Connected || (_calibration != null && _calibration.CalibrationInProgress))
            {
                return;
            }

            UpdateVariables();
        }
        private void UpdateVariables()
        {
            //Get Pupil position.
            var data = _eyeTracker.LatestGazeData;
            _leftPupilXY = data.Left.PupilPosiitionInTrackingAreaValid ? data.Left.PupilPosiitionInTrackingArea : _leftPupilXY;
            _rightPupilXY = data.Right.PupilPosiitionInTrackingAreaValid ? data.Right.PupilPosiitionInTrackingArea : _rightPupilXY;

            //Calculate distance from center of eyepiece.
            var leftDistance = Vector2.Distance(_center, _leftPupilXY);
            var rightDistance = Vector2.Distance(_center, _rightPupilXY);

            //Color of pupils
            _leftColor = data.Left.PupilPosiitionInTrackingAreaValid ? Color.Lerp(Color.green, Color.red, leftDistance / 0.35f) : Color.clear;
            _rightColor = data.Right.PupilPosiitionInTrackingAreaValid ? Color.Lerp(Color.green, Color.red, rightDistance / 0.35f) : Color.clear;

            // Info to the user
            _status = (leftDistance + rightDistance < 0.25f) ? "Good" : "Please Fix";
        }
    }
}

