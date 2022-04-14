using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace BetterWheelBarrow
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ManualLogSource logger;
        static WheelBarrow wheelBarrow;

        private void Awake()
        {
            // Plugin startup logic
            logger = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded");
            Logger.LogInfo($"Patching...");
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Patched");
        }

        [HarmonyPatch(typeof(HandToFront), "RaycastCode")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RaycastCode_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions)
            .MatchForward(false, // false = move at the start of the match, true = move at the end of the match
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_parent")),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "GetComponent", null, new[] { typeof(WheelBarrow) })),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(i => i.opcode == OpCodes.Brfalse || i.opcode == OpCodes.Brfalse_S),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldflda),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(HandToFront), "GenericHoldingRenderBlueprint"))
            );

            object label = matcher.Advance(3) //Advance to Brfalse_S
                .Instruction.operand; //Grab the label

            matcher.Advance(1).InsertAndAdvance( //Add our additional check
                new CodeInstruction(OpCodes.Ldstr, "Run"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(vp_Input), "GetButton")),
                new CodeInstruction(OpCodes.Brtrue, label)
            );

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(HandToFront), "Update")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions)
            .MatchForward(false, // false = move at the start of the match, true = move at the end of the match
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_parent")),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "GetComponent", null, new[] { typeof(WheelBarrow) })),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(i => i.opcode == OpCodes.Brfalse || i.opcode == OpCodes.Brfalse_S),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(HandToFront), "GenericThrowableInput"))
            );

            object label = matcher.Advance(3) //Advance to Brfalse_S
                .Instruction.operand; //Grab the label

            matcher.Advance(1).InsertAndAdvance( //Add our additional check
                new CodeInstruction(OpCodes.Ldstr, "Run"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(vp_Input), "GetButton")),
                new CodeInstruction(OpCodes.Brtrue, label)
            );

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(vp_FPInput), "RotatePushObject")]
        [HarmonyPrefix]
        static void RotatePushObject_Prefix(int ___pushItemID, HandToFront ____handy, ref float trgtX, float trgtZ)
        {
            if (___pushItemID == 64) //Wheelbarrow
            {
                if (vp_Input.GetButton("Zoom"))
                {
                    if (wheelBarrow == null)
                    {
                        wheelBarrow = ____handy.GetComponent<WorldRefs>()._wheelBarrow;
                    }

                    if (wheelBarrow._cargo.Count > 0)
                    {
                        wheelBarrow.CargoReturnPhysics();
                        wheelBarrow._cargo.ForEach(i => wheelBarrow.RemoveFromCargo(i, true));
                    }

                    trgtX = 45f;
                }
            }
        }

    }
}
