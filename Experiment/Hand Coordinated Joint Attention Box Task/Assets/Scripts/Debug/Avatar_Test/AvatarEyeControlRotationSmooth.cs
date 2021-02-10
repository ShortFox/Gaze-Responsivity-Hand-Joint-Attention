// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

using UnityEngine;
using Tobii.Research.Unity;
using Tobii.XR;

/// <summary>
/// Monobehaviour which handles the eye and facial expressions of the "Pete" avatar.
/// </summary>
public class AvatarEyeControlRotationSmooth : MonoBehaviour
{
    private VREyeTracker EYES;
    [Header("Eye Transforms")]

    [SerializeField]
    private Transform _leftEye;
    [SerializeField]
    private Transform _rightEye;

    [Header("Blend Shapes")]

    [SerializeField, Tooltip("Body Skinned Mesh Renderer for blend shapes.")]
    private SkinnedMeshRenderer _bodyBlendShape;
    private const int BlendShapeLeftEyeLid = 0;
    private const int BlendShapeRightEyeLid = 1;

    [SerializeField, Tooltip("Facial blend shapes animation curve.")]
    private AnimationCurve _blendShapeAnimationCurve;

    [SerializeField, Tooltip("Facial blend shape animation time.")]
    private float _blendShapeAnimationTimeSeconds = 0.05f;

    // Running blend shapes animation values.
    private float _blinkL;
    private float _blinkR;
    private float _animationProgressL;
    private float _animationProgressR;

    // Catch change in state flags.
    private bool _leftEyeClosed;
    private bool _rightEyeClosed;


    [Header("Eye Behaviour values")]

    [SerializeField, Tooltip("Cross eye correction for models that look cross eyed. +/-20 degrees.")]
    [Range(-20.0f, 20.0f)]
    private float _crossEyeCorrection;

    [SerializeField, Tooltip("Reduce gaze direction jitters at the cost of responsiveness.")]
    private float _gazeDirectionSmoothTime = 0.03f;

    [SerializeField, Tooltip("Maximum eye vertical angle.")]
    private float _verticalGazeAngleUpperLimitInDegrees = 25;

    [SerializeField, Tooltip("Minimum eye vertical angle.")]
    private float _verticalGazeAngleLowerLimitInDegrees = 30;

    [SerializeField, Tooltip("Maximum eye horizontal angle.")]
    private float _horizontalGazeAngleLimitInDegrees = 35;


    private Vector3 _leftSmoothDampVelocity;
    private Vector3 _rightSmoothDampVelocity;
    private float _leftLidSmoothDampVelocity;
    private float _rightLidSmoothDampVelocity;

    // Gaze direction fall-back.
    private Vector3 _previousSmoothedDirectionL = Vector3.forward;
    private Vector3 _previousSmoothedDirectionR = Vector3.forward;
    private Vector3 _lastGoodDirectionL = Vector3.forward;
    private Vector3 _lastGoodDirectionR = Vector3.forward;

    private const float CrossEyedCorrectionFactor = 100;
    private const float CrossEyednessSnarlTriggerValue = 0.3f;
    private const float AngularNoiseCompensationFactor = 800;

    private Camera myCamera;
    private Transform _cameraTransform;

    private void Start()
    {
        myCamera = this.transform.parent.Find("Camera").GetComponent<Camera>();

        EYES = VREyeTracker.Instance;
        //_cameraTransform = Camera.main.transform;
        _cameraTransform = myCamera.transform;
    }

    private void Update()
    {
        if (!myCamera.enabled)
        {
            this.enabled = false;
            return;
        }

        // Get local copies.
        var eyeDataLeft = EYES.LatestGazeData.Left;
        var eyeDataRight = EYES.LatestGazeData.Right;

        // Get local transform direction.
        var gazeDirectionL = _cameraTransform.InverseTransformDirection(eyeDataLeft.GazeRayWorld.direction);
        var gazeDirectionR = _cameraTransform.InverseTransformDirection(eyeDataRight.GazeRayWorld.direction);

        // If direction data is invalid use other eye's data or if that's invalid use last good data.
        if (!eyeDataLeft.GazeRayWorldValid)
        {
            gazeDirectionL = eyeDataRight.GazeRayWorldValid ? gazeDirectionR : _lastGoodDirectionL;
        }
        if (!eyeDataRight.GazeRayWorldValid)
        {
            gazeDirectionR = eyeDataLeft.GazeRayWorldValid ? gazeDirectionL : _lastGoodDirectionR;
        }

        // Save last good data.
        _lastGoodDirectionL = gazeDirectionL;
        _lastGoodDirectionR = gazeDirectionR;

        // Combine vertical gaze direction by using the average Y.
        var averageYDirection = (gazeDirectionL.y + gazeDirectionR.y) / 2f;
        gazeDirectionL.y = averageYDirection;
        gazeDirectionR.y = averageYDirection;

        // Increase smoothing for noisier higher angles
        var angleL = Vector3.Angle(gazeDirectionL, Vector3.forward);
        var angleR = Vector3.Angle(gazeDirectionR, Vector3.forward);
        var compensatedSmoothTimeL = _gazeDirectionSmoothTime + angleL / AngularNoiseCompensationFactor;
        var compensatedSmoothTimeR = _gazeDirectionSmoothTime + angleR / AngularNoiseCompensationFactor;

        // Apply smoothing from previous frame to this one.
        var smoothedDirectionL = Vector3.SmoothDamp(_previousSmoothedDirectionL, gazeDirectionL,
            ref _leftSmoothDampVelocity, compensatedSmoothTimeL);
        var smoothedDirectionR = Vector3.SmoothDamp(_previousSmoothedDirectionR, gazeDirectionR,
            ref _rightSmoothDampVelocity, compensatedSmoothTimeR);
        _previousSmoothedDirectionL = smoothedDirectionL;
        _previousSmoothedDirectionR = smoothedDirectionR;

        var leftRotation = Quaternion.LookRotation(smoothedDirectionL);
        var rightRotation = Quaternion.LookRotation(smoothedDirectionR);

        // Rotate the eye transforms to match the eye direction.
        _leftEye.transform.localRotation = leftRotation;
        _rightEye.transform.localRotation = rightRotation;

        AnimateEyeLid(eyeDataLeft, eyeDataRight);
    }

    /// <summary>
    /// Clamp vertical gaze angles - needs to be done in degrees.
    /// </summary>
    /// <param name="gazeDirection">Direction vector of the gaze.</param>
    /// <param name="lowerLimit">The lower clamp limit in degrees.</param>
    /// <param name="upperLimit">The upper clamp limit  in degrees.</param>
    /// <returns>The gaze direction clamped between the two degree limits.</returns>
    private static Vector3 ClampVerticalGazeAngles(Vector3 gazeDirection, float lowerLimit, float upperLimit)
    {
        var angleRad = Mathf.Atan(gazeDirection.y / gazeDirection.z);
        var angleDeg = angleRad * Mathf.Rad2Deg;

        var y = Mathf.Tan(upperLimit * Mathf.Deg2Rad) * gazeDirection.z;
        if (angleDeg > upperLimit)
        {
            gazeDirection = new Vector3(gazeDirection.x, y, gazeDirection.z);
        }

        y = Mathf.Tan(-lowerLimit * Mathf.Deg2Rad) * gazeDirection.z;
        if (angleDeg < -lowerLimit)
        {
            gazeDirection = new Vector3(gazeDirection.x, y, gazeDirection.z);
        }

        return gazeDirection;
    }

    /// <summary>
    /// Clamp horizontal gaze angles - needs to be done in degrees.
    /// </summary>
    /// <param name="gazeDirection">Direction vector of the gaze.</param>
    /// <param name="limit">The limit to clamp to in degrees.</param>
    /// <returns>The clamped gaze direction.</returns>
    private static Vector3 ClampHorizontalGazeAngles(Vector3 gazeDirection, float limit)
    {
        var angleRad = Mathf.Atan(gazeDirection.x / gazeDirection.z);
        var angleDeg = angleRad * Mathf.Rad2Deg;

        var x = Mathf.Tan(limit * Mathf.Deg2Rad) * gazeDirection.z;
        if (angleDeg > limit)
        {
            gazeDirection = new Vector3(x, gazeDirection.y, gazeDirection.z);
        }

        if (angleDeg < -limit)
        {
            gazeDirection = new Vector3(-x, gazeDirection.y, gazeDirection.z);
        }

        return gazeDirection;
    }

    /// <summary>
    /// Animate facial expression blend shapes.
    /// </summary>
    /// <param name="newDirectionL">The gaze direction for the left eye.</param>
    /// <param name="newDirectionR">The gaze direction for the right eye.</param>
    /// <param name="eyeDataLeft">Eye data for the left eye.</param>
    /// <param name="eyeDataRight">Eye data for the right eye.</param>
    private void AnimateEyeLid(IVRGazeDataEye leftEye, IVRGazeDataEye rightEye)
    {
        // Determine if Eyes are closed
        //var leftEyeClosed = leftEye.PupilPosiitionInTrackingAreaValid ? false : true;
        //var rightEyeClosed = rightEye.PupilPosiitionInTrackingAreaValid ? false : true;

        // If eye openness data is invalid use last value.\
        //TobiiXR has useful function to determine if eye is blinking.
        var leftEyeClosed = (TobiiXR.EyeTrackingData.Left.EyeOpennessIsValid) ? (TobiiXR.EyeTrackingData.Left.EyeOpenness < 0.5f) : _leftEyeClosed;
        var rightEyeClosed = (TobiiXR.EyeTrackingData.Right.EyeOpennessIsValid) ? (TobiiXR.EyeTrackingData.Right.EyeOpenness < 0.5f) : _rightEyeClosed;

        var leftEyeBlinkValue = leftEyeClosed ? 100 : 0;
        var rightEyeBlinkValue = rightEyeClosed ? 100 : 0;

        // If eye openness has changed reset animation curve.
        if (leftEyeClosed != _leftEyeClosed)
        {
            _animationProgressL = 0;
        }
        if(rightEyeClosed != _rightEyeClosed)
        {
            _animationProgressR = 0;
        }

        _blinkL = Mathf.Lerp(_blinkL, leftEyeBlinkValue, _blendShapeAnimationCurve.Evaluate(_animationProgressL));
        _blinkR = Mathf.Lerp(_blinkR, rightEyeBlinkValue, _blendShapeAnimationCurve.Evaluate(_animationProgressR));

        _bodyBlendShape.SetBlendShapeWeight(BlendShapeLeftEyeLid, _blinkL);
        _bodyBlendShape.SetBlendShapeWeight(BlendShapeRightEyeLid, _blinkR);

        _leftEyeClosed = leftEyeClosed;
        _rightEyeClosed = rightEyeClosed;

        _animationProgressL += Time.deltaTime / _blendShapeAnimationTimeSeconds;
        _animationProgressR += Time.deltaTime / _blendShapeAnimationTimeSeconds;
    }

    /// <summary>
    /// Checks whether the eye is open or not.
    /// </summary>
    /// <param name="eyeData"></param>
    /// <returns>True if eye openness is valid and the openness value is 1.</returns>
    private static bool IsEyeOpen(TobiiXR_PerEyeData eyeData)
    {
        return eyeData.EyeOpennessIsValid && Mathf.Approximately(eyeData.EyeOpenness, 1);
    }
}
