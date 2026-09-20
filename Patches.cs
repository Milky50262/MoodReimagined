using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using CUCoreLib.Helpers;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MoodReimagined
{
    [HarmonyPatch(typeof(MoodleManager), nameof(MoodleManager.AddMoodle))]
    internal static class MoodleSpritePatch
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new(System.StringComparer.OrdinalIgnoreCase);
        private static bool _loaded;

        private static void Postfix(MoodleManager __instance, int intensity, string icon)
        {
            LoadSprites();

            if (__instance.moodles == null || __instance.moodles.childCount == 0)
            {
                return;
            }

            Transform moodleTransform = __instance.moodles.GetChild(__instance.moodles.childCount - 1);
            Image innerImage = moodleTransform.childCount > 0
                ? moodleTransform.GetChild(0).GetComponent<Image>()
                : null;

            if (innerImage == null)
            {
                return;
            }

            if (TryGetReplacementSprite(icon, intensity, out Sprite replacement))
            {
                innerImage.sprite = replacement;
                innerImage.SetNativeSize();
            }
        }

        private static bool TryGetReplacementSprite(string icon, int intensity, out Sprite sprite)
        {
            string intensityKey = intensity > 0 ? $"{icon}{intensity}" : icon;
            return SpriteCache.TryGetValue(intensityKey, out sprite)
                || SpriteCache.TryGetValue(icon, out sprite);
        }

        private static void LoadSprites()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
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
