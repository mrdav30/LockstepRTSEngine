using System.Reflection;
using UnityEditor;

namespace RTSLockstep
{
	public static class DevelopmentHelpers
	{
        public static void ClearConsole()
        {
            var assembly = Assembly.GetAssembly(typeof(SceneView));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }
    }
}