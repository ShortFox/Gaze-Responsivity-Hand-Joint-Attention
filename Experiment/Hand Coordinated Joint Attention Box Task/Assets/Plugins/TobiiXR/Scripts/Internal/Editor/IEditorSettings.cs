using UnityEditor;

namespace Tobii.XR
{
    public interface IEditorSettings
    {
        void SetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup, string defines);
        string GetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup);
    }
}