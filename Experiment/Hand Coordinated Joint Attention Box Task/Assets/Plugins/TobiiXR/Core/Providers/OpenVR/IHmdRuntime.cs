// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.XR
{
    using UnityEngine;

    public interface IHmdRuntime
    {
        float InterpupilarDistanceInMeters();

        Transform GetHmdOrigin();

        void Destroy();
    }
}