// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.XR
{
    using System;
    using StreamEngine;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Diagnostics;
    using UnityEngine;

    internal class StreamEngineTracker
    {
        public static string IntegrationType = "vr";

        private tobii_wearable_data_callback_t _wearableDataCallback; // Needed to prevent GC from removing callback
        private StreamEngineContext _streamEngineContext;
        private Stopwatch _stopwatch = new Stopwatch();

        private bool _isReconnecting;
        private float _reconnectionTimestamp;

        public TobiiXR_EyeTrackingData LocalLatestData { get; private set; }
        public bool ReceivedDataThisFrame { get; private set; }

        public StreamEngineTracker()
        {
            if (TryConnectToTracker(ref _streamEngineContext, _stopwatch) == false)
            {
                throw new Exception("Failed to connect to tracker");
            }

            _wearableDataCallback = OnWearableData;
            if (SubscribeToWearableData(_streamEngineContext.Device, _wearableDataCallback) == false)
            {
                throw new Exception("Failed to subscribe to tracker");
            }

            LocalLatestData = new TobiiXR_EyeTrackingData();
        }


        public void Tick()
        {
            ReceivedDataThisFrame = false;

            if (_isReconnecting)
            {
                _isReconnecting = IsReconnectingToDevice(_streamEngineContext.Device, ref _reconnectionTimestamp);
                return;
            }
            var result = ProcessCallback(_streamEngineContext.Device, _stopwatch);
            if (result == tobii_error_t.TOBII_ERROR_CONNECTION_FAILED)
            {
                UnityEngine.Debug.Log("Reconnecting...");
                _reconnectionTimestamp = Time.unscaledTime;
                _isReconnecting = true;
            }
        }

        public void Destroy()
        {
            if (_streamEngineContext == null) return;

            var result = Interop.tobii_device_destroy(_streamEngineContext.Device);
            if (result != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                UnityEngine.Debug.LogError(string.Format("Failed to destroy device context. Error {0}", result));
            }

            result = Interop.tobii_api_destroy(_streamEngineContext.Api);
            if (result != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                UnityEngine.Debug.LogError(string.Format("Failed to destroy api context. Error {0}", result));
            }

            UnityEngine.Debug.Log(string.Format("Destroyed SE tracker {0}", _streamEngineContext.Url));

            _streamEngineContext = null;
            _stopwatch = null;
        }
        private void OnWearableData(ref tobii_wearable_data_t data)
        {
            CopyEyeTrackingData(LocalLatestData, ref data);
            ReceivedDataThisFrame = true;
        }
        private static tobii_error_t ProcessCallback(IntPtr deviceContext, Stopwatch stopwatch)
        {
            StartStopwatch(stopwatch);
            var result = Interop.tobii_device_process_callbacks(deviceContext);
            var milliseconds = StopStopwatch(stopwatch);

            if (result != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                UnityEngine.Debug.LogError(string.Format("Failed to process callback. Error {0}", result));
            }

            if (milliseconds > 1)
            {
                UnityEngine.Debug.LogWarning(string.Format("Process callbacks took {0}ms", milliseconds));
            }

            return result;
        }

        private static void CopyEyeTrackingData(TobiiXR_EyeTrackingData latestDataLocalSpace, ref tobii_wearable_data_t data)
        {
            latestDataLocalSpace.CombinedRay.IsValid = data.gaze_direction_combined_validity == tobii_validity_t.TOBII_VALIDITY_VALID && data.gaze_origin_combined_validity == tobii_validity_t.TOBII_VALIDITY_VALID;
            latestDataLocalSpace.CombinedRay.Origin.x = data.gaze_origin_combined_mm_xyz.x * -1 / 1000f;
            latestDataLocalSpace.CombinedRay.Origin.y = data.gaze_origin_combined_mm_xyz.y / 1000f;
            latestDataLocalSpace.CombinedRay.Origin.z = data.gaze_origin_combined_mm_xyz.z / 1000f;
            latestDataLocalSpace.CombinedRay.Direction.x = data.gaze_direction_combined_normalized_xyz.x * -1;
            latestDataLocalSpace.CombinedRay.Direction.y = data.gaze_direction_combined_normalized_xyz.y;
            latestDataLocalSpace.CombinedRay.Direction.z = data.gaze_direction_combined_normalized_xyz.z;

            CopyIndividualEyeData(ref latestDataLocalSpace.Left, ref data.left);
            CopyIndividualEyeData(ref latestDataLocalSpace.Right, ref data.right);
        }

        private static void CopyIndividualEyeData(ref TobiiXR_PerEyeData perEyeData, ref tobii_wearable_eye_t data)
        {
            perEyeData.Ray.IsValid = data.gaze_direction_validity == tobii_validity_t.TOBII_VALIDITY_VALID && data.gaze_origin_validity == tobii_validity_t.TOBII_VALIDITY_VALID;
            perEyeData.Ray.Origin.x = data.gaze_origin_mm_xyz.x * -1 / 1000f;
            perEyeData.Ray.Origin.y = data.gaze_origin_mm_xyz.y / 1000f;
            perEyeData.Ray.Origin.z = data.gaze_origin_mm_xyz.z / 1000f;
            perEyeData.Ray.Direction.x = data.gaze_direction_normalized_xyz.x * -1;
            perEyeData.Ray.Direction.y = data.gaze_direction_normalized_xyz.y;
            perEyeData.Ray.Direction.z = data.gaze_direction_normalized_xyz.z;

            perEyeData.EyeOpennessIsValid = data.eye_openness_validity == tobii_validity_t.TOBII_VALIDITY_VALID;
            perEyeData.EyeOpenness = data.eye_openness;
        }

        private static long StopStopwatch(Stopwatch stopwatch)
        {
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private static void StartStopwatch(Stopwatch stopwatch)
        {
            stopwatch.Reset();
            stopwatch.Start();
        }

        private static bool TryConnectToTracker(ref StreamEngineContext streamEngineContext, Stopwatch stopwatch)
        {
            StartStopwatch(stopwatch);

            IntPtr apiContext;
            if (CreateApiContext(out apiContext) == false) return false;

            List<string> connectedDevices;
            if (GetAvailableTrackers(apiContext, out connectedDevices) == false) return false;

            string hmdEyeTrackerUrl;
            if (GetUrlForHmdTracker(connectedDevices, out hmdEyeTrackerUrl) == false) return false;

            IntPtr deviceContext;
            if (CreateDevice(hmdEyeTrackerUrl, apiContext, out deviceContext) == false) return false;

            streamEngineContext = new StreamEngineContext(apiContext, deviceContext, hmdEyeTrackerUrl);

            var elapsedTime = StopStopwatch(stopwatch);

            UnityEngine.Debug.Log(string.Format("Connected to SE tracker: {0} and it took {1}ms", hmdEyeTrackerUrl, elapsedTime));
            return true;
        }

        private static bool SubscribeToWearableData(IntPtr context, tobii_wearable_data_callback_t wearableDataCallback)
        {
            var result = Interop.tobii_wearable_data_subscribe(context, wearableDataCallback);
            if (result == tobii_error_t.TOBII_ERROR_NO_ERROR) return true;

            UnityEngine.Debug.LogError("Failed to subscribe to wearable stream." + result);
            return false;
        }

        private static bool CreateDevice(string url, IntPtr apiContext, out IntPtr deviceContext)
        {
            var result = Interop.tobii_device_create(apiContext, url, out deviceContext);
            if (result == tobii_error_t.TOBII_ERROR_NO_ERROR) return true;

            UnityEngine.Debug.LogError("Failed to create context. " + result);
            return false;
        }

        private static bool GetUrlForHmdTracker(List<string> connectedDevices, out string url)
        {
            url = "";
            var index = -1;

            for (var i = 0; i < connectedDevices.Count; i++)
            {
                var connectedDeviceUrl = connectedDevices[i];
                var lowerCaseIntegrationType = connectedDeviceUrl.ToLower(CultureInfo.InvariantCulture);

                if (lowerCaseIntegrationType.Contains(IntegrationType) == false) continue;

                url = connectedDeviceUrl;
                index = i;
                break;
            }

            if (index != -1) return true;

            UnityEngine.Debug.LogWarning(string.Format("Failed to find Tobii eye trackers of integration type {0}", IntegrationType));
            return false;
        }

        private static bool GetAvailableTrackers(IntPtr apiContext, out List<string> connectedDevices)
        {
            var result = Interop.tobii_enumerate_local_device_urls(apiContext, out connectedDevices);
            if (result != tobii_error_t.TOBII_ERROR_NO_ERROR)
            {
                UnityEngine.Debug.LogError("Failed to enumerate connected devices. " + result);
                return false;
            }

            if (connectedDevices.Count >= 1) return true;

            UnityEngine.Debug.LogWarning("No connected eye trackers found.");
            return false;
        }

        private static bool CreateApiContext(out IntPtr apiContext)
        {
            var result = Interop.tobii_api_create(out apiContext, null);
            if (result == tobii_error_t.TOBII_ERROR_NO_ERROR) return true;

            UnityEngine.Debug.LogError("Failed to create api context. " + result);
            apiContext = IntPtr.Zero;
            return false;
        }

        private static bool IsReconnectingToDevice(IntPtr deviceContext, ref float reconnectionTimestamp)
        {
            var now = Time.unscaledTime;
            var deltaTime = now - reconnectionTimestamp;

            if (deltaTime < 0.5) return true;

            reconnectionTimestamp = now;

            var nativeContext = deviceContext;
            var result = Interop.tobii_device_reconnect(nativeContext);
            if (result != tobii_error_t.TOBII_ERROR_NO_ERROR) return true;

            UnityEngine.Debug.Log("Reconnected.");
            return false;
        }

        protected class StreamEngineContext
        {
            public IntPtr Device { get; private set; }
            public IntPtr Api { get; private set; }

            public string Url { get; private set; }

            public StreamEngineContext(IntPtr apiContext, IntPtr deviceContext, string url)
            {
                Api = apiContext;
                Device = deviceContext;
                Url = url;
            }
        }
    }
}