using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tobii.XR
{
    public static class EditorUtils
    {
        public static readonly IEditorSettings _editorSettings = new EditorSettings();
        public const string COMPILERFLAGPREFIX = "TOBIIXR_";

        internal static void UpdateCompilerFlags(TobiiXR_Settings settings)
        {
            UpdateCompilerFlags(settings, _editorSettings);
        }

        internal static void UpdateCompilerFlags(TobiiXR_Settings settings, IEditorSettings editorSettings)
        {
            SetCompilerflagForBuildTarget(settings.EyeTrackingProviderTypeAndroid, editorSettings, BuildTargetGroup.Android);
            SetCompilerflagForBuildTarget(settings.EyeTrackingProviderTypeStandAlone, editorSettings, BuildTargetGroup.Standalone);
        }

        private static void SetCompilerflagForBuildTarget(string eyetrackerProviderType, IEditorSettings editorSettings, BuildTargetGroup target)
        {
            var flags = editorSettings.GetScriptingDefineSymbolsForGroup(target).Split(';').ToList();
            var type = Assembly.GetAssembly(typeof(NoseDirectionProvider)).GetType(eyetrackerProviderType);
            var attribute = Attribute.GetCustomAttribute(type, typeof(CompilerFlagAttribute)) as CompilerFlagAttribute;

            if (attribute != null)
            {
                if (flags.Contains(attribute.Flag)) return;
                flags.RemoveAll(flag => flag.StartsWith(COMPILERFLAGPREFIX));
                flags.Add(attribute.Flag);
                if (!attribute.Flag.StartsWith(COMPILERFLAGPREFIX)) Debug.LogError(string.Format("Provider {0} uses CompilerFlag {1} which does not use TOBIIXR_ prefix, this will cause problems when switching providers!", type.Name, attribute.Flag));
                editorSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", flags.ToArray()));
            }
            else
            {
                if (flags.Any(f => f.StartsWith(COMPILERFLAGPREFIX)))
                {
                    flags.RemoveAll(flag => flag.StartsWith(COMPILERFLAGPREFIX));
                    editorSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", flags.ToArray()));
                }
            }
        }

        public static IEnumerable<Type> EyetrackingProviderTypes()
        {
            var type = typeof(IEyeTrackingProvider);
            var types = (AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => type.IsAssignableFrom(p) && p.IsClass));
            return types;
        }

        public static IEnumerable<Type> EyetrackingProviderTypes(BuildTargetGroup buildTarget)
        {
            var include = new List<Type>();
            var providers = EyetrackingProviderTypes().ToList();
            foreach (var provider in providers)
            {
                var attribute = Attribute.GetCustomAttribute(provider, typeof(SupportedPlatformAttribute)) as SupportedPlatformAttribute;

                if (attribute == null || attribute.Targets.Select(ConvertFromXRTargetGroup).Contains(buildTarget))
                {
                    include.Add(provider);
                }
            }
            return include;

        }
        private static BuildTargetGroup ConvertFromXRTargetGroup(XRBuildTargetGroup xrBuildTargetGroup)
        {
            return xrBuildTargetGroup == XRBuildTargetGroup.Android ? BuildTargetGroup.Android : BuildTargetGroup.Standalone;
        }

        private class EditorSettings : IEditorSettings
        {
            public void SetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup, string defines)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
            }

            public string GetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup)
            {
                return PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            }
        }

        
    }
}