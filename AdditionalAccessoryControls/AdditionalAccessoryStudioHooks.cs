using AIChara;
using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.Text;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryStudioHooks
    {

        public static void PatchMe()
        {
            Harmony harmony = new Harmony(AdditionalAccessoryControlsPlugin.GUID);
            harmony.PatchAll(typeof(AdditionalAccessoryStudioHooks));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(FKCtrl), "LateUpdate")]
        static void FKLateUpdatePostfix(FKCtrl __instance)
        {
            AdditionalAccessoryAdvancedParentSkinnedMeshHelper.ExternalUpdate(__instance.gameObject.GetComponent<ChaControl>(), false, true, false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ShowAccessory))]
        public static void ShowOCIAccessory(OCIChar __instance)
        {
            try
            {
                AdditionalAccessoryControlsController aacController = __instance.charInfo.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                aacController.HandleVisibilityRules(accessory: true);
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Visibility Updates Not Thrown. {e.Message} {e.StackTrace}");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(OCIChar), "LoadClothesFile")]
        static void OnStudioCoordLoadPrefix(OCIChar __instance)
        {
            try
            {
#if DEBUG
                AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Saving Coord Preload Snapshots");
#endif
                // Find Controller
                AdditionalAccessoryControlsController aacController = __instance.charInfo.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                if (aacController != null)
                {
                    aacController.MaterialEditorHelper.UpdateOnCoordinateLoadSnapshot();
                    aacController.DBHelper.UpdateOnCoordinateLoadSnapshot();
                }
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Character Accessories may not be restored after this load. {e.Message} {e.StackTrace}");
            }
        }
    }
}
