using System.IO;
using HarmonyLib;

namespace SupermarketSimulatorCheatMenu
{
    [HarmonyPatch(typeof(WarningSystem), "SpawnCustomerSpeech")]
    public static class SpawnCustomerSpeechHook
    {
        private static void Prefix(WarningSystem __instance)
        {
            Helper.TrySetField<WarningSystem, float>(__instance, "m_CustomerSpeechLifetime", 15);
        }
    }
}
