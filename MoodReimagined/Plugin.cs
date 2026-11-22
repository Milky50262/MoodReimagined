using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using CUCoreLib.Helpers;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;

namespace MoodReimagined
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency("net.cucorelib", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "com.milky50262.moodreimagined";
        public const string ModName = "Mood Reimagined";
        public const string ModVersion = "2.2";

        internal static new ManualLogSource Logger;

        private readonly Harmony _harmony = new(ModGUID);

        public static Plugin Instance { get; private set; }
        public static string PluginDirectory => Path.GetDirectoryName(Instance.Info.Location);

        internal static readonly Dictionary<string, Sprite> SpriteCache = new(System.StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            _harmony.PatchAll();

            LoadSprites();
            Logger.LogInfo($"Mood reimag(in)ed!");
        }

        internal static void LoadSprites()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.IndexOf(".Moodles.", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                string[] parts = resourceName.Split('.');
                string key = parts.Length >= 2 ? parts[parts.Length - 2] : Path.GetFileNameWithoutExtension(resourceName);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                Sprite sprite = AssetLoader.LoadEmbeddedSprite(resourceName, 33.333333f * 8f /* for 160x160px */, assembly);
                if (sprite != null)
                {
                    SpriteCache[key.ToLowerInvariant()] = sprite;
                }
            }
        }
    }
}
