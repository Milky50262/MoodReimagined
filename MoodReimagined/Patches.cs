using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static System.Reflection.Emit.OpCodes;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MoodReimagined
{
    enum TSwitch {
        FindGameObjectString,
        FindGameObjectStloc,
        Iconsfield,
        FindSetSprite,
        End
    }

    [HarmonyPatch(typeof(MoodleManager), nameof(MoodleManager.AddMoodle))]
    internal static class MoodleSpritePatch
    {
        static ManualLogSource Logger = Plugin.Logger;
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILgen) {
            Logger.LogInfo("Inserting function entry in MoodleManager::AddMoodle");
            var startidx = -1;
            var endidx = -1;
            var compidx = -1;
            int ldlocid = -1;
            bool ldloc_x = false;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo GetComp = AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), generics: [typeof(Image)]);
            FieldInfo iconsfield = AccessTools.Field(typeof(MoodleManager), nameof(MoodleManager.icons));
            MethodInfo setsprite = AccessTools.Method(typeof(Image), "set_" + nameof(Image.sprite));
            TSwitch stage = 0;
            for (var i = 0; i < codes.Count; i++) {
                CodeInstruction code = codes[i];
                switch(stage) {
                    case TSwitch.FindGameObjectString:
                        if(code.opcode == Ldstr && (string)code.operand == "MoodleInside") {
                            stage = TSwitch.FindGameObjectStloc;
                            Logger.LogDebug("Found string " + i);
                        }
                    break;
                    case TSwitch.FindGameObjectStloc:
                        OpCode ocode = code.opcode;
                        int codeval = code.opcode.Value;
                        if(Stloc_0.Value <= codeval && Stloc_3.Value >= codeval) {
                            stage = TSwitch.Iconsfield;
                            ldlocid = Ldloc_0.Value + (codeval - Stloc_0.Value);
                            ldloc_x = true;
                            Logger.LogDebug("Found Stloc_X " + i);
                        } else if(ocode == Stloc || ocode == Stloc_S) {
                            stage = TSwitch.Iconsfield;
                            ldlocid = (int)code.operand;
                            Logger.LogDebug("Found Stloc " + i);
                        }
                    break;
                    case TSwitch.Iconsfield:
                        if (!ldloc_x && (code.opcode == Ldloc || code.opcode == Ldloc_S) && (int)code.operand == ldlocid) {
                            startidx = i;
                            Logger.LogDebug("Found start " + i);
                            break;
                        } else if (ldloc_x && code.opcode.Value == ldlocid) {
                            startidx = i;
                            Logger.LogDebug("Found start " + i);
                            break;
                        }

                        if (i - startidx == 1 && code.Calls(GetComp)) {
                            compidx = i;
                            Logger.LogDebug("Found GetComponent " + i);
                        } else if(code.LoadsField(iconsfield)) {
                            stage = TSwitch.FindSetSprite;
                            Logger.LogDebug("Found Dictionary " + i);
                        }
                    break;
                    case TSwitch.FindSetSprite:
                        if (code.Calls(setsprite)) {
                            stage = TSwitch.End;
                            endidx = i;
                            Logger.LogDebug("Found end " + i);
                        } else if(code.IsLdloc()) {
                            stage = TSwitch.End;
                            Logger.LogDebug("Past end " + i);
                        }
                    break;
                    default:
                        i = codes.Count;
                    break;
                }
            }

            if (endidx != -1) {
                LocalBuilder replacement = ILgen.DeclareLocal(typeof(Sprite));
                replacement.SetLocalSymInfo("ReplacementImage");
                Label afterfunct = ILgen.DefineLabel();
                Label endlab = ILgen.DefineLabel();
                codes[compidx + 1].labels.Add(afterfunct);
                codes[endidx].labels.Add(endlab);
                List<CodeInstruction> callfunct = [
                    new(OpCodes.Ldarg_2),
                    new(OpCodes.Ldarg_1),
                    new(OpCodes.Ldloca, replacement),
                    CodeInstruction.Call(typeof(MoodleSpritePatch), nameof(MoodleSpritePatch.TryGetReplacementSprite)),
                    new(OpCodes.Brfalse, afterfunct),
                    new(OpCodes.Ldloc, replacement),
                    new(OpCodes.Br, endlab),
                ];
                codes.InsertRange(compidx + 1, (IEnumerable<CodeInstruction>)callfunct);
            } else {
                Logger.LogError("Not patching");
            }

            return (IEnumerable<CodeInstruction>)codes;
        }

        private static bool TryGetReplacementSprite(string icon, int intensity, out Sprite sprite)
        {
            string intensityKey = intensity > 0 ? $"{icon}{intensity}" : icon;
            return Plugin.SpriteCache.TryGetValue(intensityKey, out sprite)
                || Plugin.SpriteCache.TryGetValue(icon, out sprite);
        }
    }
}
