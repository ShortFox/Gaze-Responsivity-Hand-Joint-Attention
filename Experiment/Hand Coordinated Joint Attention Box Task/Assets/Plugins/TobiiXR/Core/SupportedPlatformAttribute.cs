using System;

namespace Tobii.XR
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SupportedPlatformAttribute : Attribute
    {
        public readonly XRBuildTargetGroup [] Targets;

        public SupportedPlatformAttribute(params XRBuildTargetGroup [] targets)
        {
            Targets = targets;
        }
    }

    public enum XRBuildTargetGroup
    {
        Standalone,
        Android
    }

}