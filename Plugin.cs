using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;

namespace MoodReimagined
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    // [BepInDependency("net.cucorelib", BepInDependency.DependencyFlags.HardDependency)] // Would be nice if you added this ;3 ;3
    // No, it's not needed, feel free to omit it ahaha
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "com.milky50262.moodreimagined";
        public const string ModName = "Mood Reimagined";
        public const string ModVersion = "1.0.0";

        internal static new ManualLogSource Logger;

        private readonly Harmony _harmony = new(ModGUID);

        public static Plugin Instance { get; private set; }
        public static string PluginDirectory => Path.GetDirectoryName(Instance.Info.Location);

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            _harmony.PatchAll();

            Logger.LogInfo($"Mood reimag(in)ed!");
        }
    }
}
