using AIChara;
#if AI
using AIProject;
#endif
using CharaCustom;
using HarmonyLib;
using Manager;
using Studio;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AdditionalAccessoryControls
{

    static class AdditionalAccessoryHooks
    {

        

        public static void PatchMe()
        {
            Harmony harmony = new Harmony(AdditionalAccessoryControlsPlugin.GUID);
            harmony.PatchAll(typeof(AdditionalAccessoryHooks));           
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CmpBase), "EnableDynamicBones")]
        static void MoreAccessoriesLateUpdateForcePostfixPostfix(CmpBase __instance, ref bool enable)
        {
            if (!AdditionalAccessoryControlsPlugin.MoreAccessoriesDynamicBonesFix.Value)
                return;

            if (__instance.GetType() != typeof(CmpAccessory))
                return;

            int slot = int.Parse(__instance.gameObject.name.Substring(7));            
            if (slot >= 20 && __instance.isVisible)
            {
                enable = !enable;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DynamicBone), "UpdateDynamicBones")]
        static void DynamicBoneUpdate(DynamicBone __instance)
        {
            AdditionalAccessoryControlDynamicBoneUpdateManager.InvokeUpdateListeners(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DynamicBone_Ver02), "UpdateDynamicBones")]
        static void DynamicBoneV2Update(DynamicBone_Ver02 __instance)
        {
            AdditionalAccessoryControlDynamicBoneUpdateManager.InvokeUpdateListeners(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
        static void ClothesStateChange(ChaControl __instance, int clothesKind, byte state, bool next)
        {
            try
            {
                AdditionalAccessoryControlsController aacController = __instance.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                aacController.HandleVisibilityRules(clothes: true);
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Visibility Updates Not Thrown. {e.Message} {e.StackTrace}");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothes), new[] { typeof(int), typeof(int), typeof(bool) })]
        public static void ChangingClothes(ChaControl __instance, int kind)
        {
            try
            {
                AdditionalAccessoryControlsController aacController = __instance.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                aacController.HandleVisibilityRules(clothes: true);
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Visibility Updates Not Thrown. {e.Message} {e.StackTrace}");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetAccessoryStateAll))]
        public static void ShowAccessoryAll(ChaControl __instance)
        {
            try
            {
                AdditionalAccessoryControlsController aacController = __instance.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                aacController.HandleVisibilityRules(startup: true, accessory: true);
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Visibility Updates Not Thrown. {e.Message} {e.StackTrace}");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetAccessoryState))]
        public static void ShowAccessory(ChaControl __instance)
        {
            try
            {
                AdditionalAccessoryControlsController aacController = __instance.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                aacController.HandleVisibilityRules(accessory: true);
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Visibility Updates Not Thrown. {e.Message} {e.StackTrace}");
            }
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


        [HarmonyPrefix, HarmonyPatch(typeof(CvsC_CreateCoordinateFile), "CreateCoordinateFileBefore")]
        static void beforeCoordinateCapture()
        {
            try
            {
                ChaControl chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
                AdditionalAccessoryControlsController aacController = chaCtrl.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                aacController.HideCharacterAccessories();
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Character accessories may not have been hidden prior to save. {e.Message} {e.StackTrace}");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile")]
        static void OnCoordSavePrefix(ChaFileCoordinate __instance)
        {
            try
            {
                ChaControl owner = null;
                foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
                {
                    if (pair.Value.nowCoordinate == __instance || pair.Value.chaFile.coordinate == __instance)
                    {
                        owner = pair.Value;
                        break;
                    }
                }

                if (owner == null)
                {
                    AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Unidentified Coordinate being saved: {__instance.coordinateName} - {__instance.coordinateFileName}");
                    return;
                }

                // Find Controller
                AdditionalAccessoryControlsController aacController = owner.gameObject.GetComponent<AdditionalAccessoryControlsController>();

                aacController.ClearCharacterAccessoriesForSave(__instance);
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Character accessories may not have been cleared prior to save. {e.Message} {e.StackTrace}");
            }

        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile")]
        static void OnCoordSavePostfix(ChaFileCoordinate __instance)
        {
            try
            {
                ChaControl owner = null;
                foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
                {
                    if (pair.Value.nowCoordinate == __instance || pair.Value.chaFile.coordinate == __instance)
                    {
                        owner = pair.Value;
                        break;
                    }
                }

                if (owner == null)
                {
                    AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Unidentified Coordinate being saved: {__instance?.coordinateName} - {__instance?.coordinateFileName}");
                    return;
                }

                // Find Controller
                AdditionalAccessoryControlsController aacController = owner.gameObject.GetComponent<AdditionalAccessoryControlsController>();

                aacController.RestoreCharacterAccessoriesForSave(__instance);
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Character Accessories may not be restored after this save. {e.Message} {e.StackTrace}");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), "LoadFile", new Type[] { typeof(System.IO.Stream), typeof(int) })]
        static void OnCoordLoadPrefix(ChaFileCoordinate __instance)
        {
            try
            { 
                ChaControl owner = null;
                foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
                {
                    if (pair.Value.nowCoordinate == __instance || pair.Value.chaFile.coordinate == __instance)
                    {
                        owner = pair.Value;
                        break;
                    }
                }

                if (owner == null)
                {
                    return;
                }

                // Find Controller
                AdditionalAccessoryControlsController aacController = owner.gameObject.GetComponent<AdditionalAccessoryControlsController>();
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

#if HS2
        private static bool HStartInit = false;
        private static List<ChaControl> dirtyChars = new List<ChaControl>();

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "Start")]
        static void HSceneLoad(HScene __instance, ChaControl[] ___chaFemales, ChaControl[] ___chaMales)
        {
#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo("HScene Load Event");
#endif
            HStartInit = true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        static void HSceneStart(HScene __instance, ChaControl[] ___chaFemales, ChaControl[] ___chaMales)
        {
            try
            {
                foreach (ChaControl female in ___chaFemales)
                {
                    if (female != null && female.objTop == null)
                    {
                        dirtyChars.Add(female);
                    }
                }
                foreach (ChaControl male in ___chaMales)
                {
                    if (male != null && male.objTop == null)
                    {
                        dirtyChars.Add(male);
                    }
                }
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Visibility Updates Not Thrown. {e.Message} {e.StackTrace}");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        static void HSceneStartPost(HScene __instance, ChaControl[] ___chaFemales, ChaControl[] ___chaMales)
        {
            try
            {
                if (HStartInit)
                {
                    HStartInit = false;
                    foreach (ChaControl female in ___chaFemales)
                    {
                        if (female != null)
                        {
#if DEBUG
                            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Sending event for: {female?.fileParam?.fullname}");
#endif
                            AdditionalAccessoryControlsController aacController = female.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                            aacController.HandleVisibilityRules(hstart: true);
                        }
                    }
                    foreach (ChaControl male in ___chaMales)
                    {
                        if (male != null)
                        {
#if DEBUG
                            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Sending event for: {male?.fileParam?.fullname}");
#endif
                            AdditionalAccessoryControlsController aacController = male.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                            aacController.HandleVisibilityRules(hstart: true);
                        }
                    }
                }

                foreach (ChaControl control in dirtyChars)
                {
#if DEBUG
                    AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Sending event for: {control?.fileParam?.fullname}");
#endif
                    AdditionalAccessoryControlsController aacController = control.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                    aacController.HandleVisibilityRules(hstart: true);
                }

                dirtyChars.Clear();
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Visibility Updates Not Thrown. {e.Message} {e.StackTrace}");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProcADV")]
        static void HSceneEnd(HScene __instance, ChaControl[] ___chaFemales, ChaControl[] ___chaMales)
        {
            try
            {
#if DEBUG
                AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo("HScene End Event");
#endif
                foreach (ChaControl female in ___chaFemales)
                {
                    if (female == null) continue;

                    AdditionalAccessoryControlsController aacController = female.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                    aacController.HandleVisibilityRules(hend: true);
                }
                foreach (ChaControl male in ___chaMales)
                {
                    if (male == null) continue;

                    AdditionalAccessoryControlsController aacController = male.gameObject.GetComponent<AdditionalAccessoryControlsController>();
                    aacController.HandleVisibilityRules(hend: true);
                }
            }
            catch (Exception e)
            {
                AdditionalAccessoryControlsPlugin.Instance.Log.LogWarning($"Exception in AACP Hook, Visibility Updates Not Thrown. {e.Message} {e.StackTrace}");
            }
        }
#endif

#if AI
        private static List<ChaControl> hActors = new List<ChaControl>();

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneManager), "HsceneInit", new Type[] { typeof(AgentActor[]) })]
        static void HSceneInitAgent(AgentActor[] agent)
        {
#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo("HScene Init Multi-Agent");
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Sending event for: {Singleton<Manager.Map>.Instance.Player?.ChaControl?.fileParam?.fullname}");
#endif
            Singleton<Manager.Map>.Instance.Player.ChaControl.gameObject.GetComponent<AdditionalAccessoryControlsController>().HandleVisibilityRules(hstart: true);
            hActors.Add(Singleton<Manager.Map>.Instance.Player.ChaControl);
            foreach (AgentActor a in agent)
            {
                if (a != null)
                {
#if DEBUG
                    AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Sending event for: {a?.ChaControl?.fileParam?.fullname}");
#endif
                    a.ChaControl.gameObject.GetComponent<AdditionalAccessoryControlsController>().HandleVisibilityRules(hstart: true);
                    hActors.Add(a.ChaControl);
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneManager), "HsceneInit", new Type[] { typeof(MerchantActor), typeof(AgentActor) })]
        static void HSceneInitMerchant(MerchantActor Merchant, AgentActor agent)
        {
#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo("HScene Init Merchant");
#endif
            Singleton<Manager.Map>.Instance.Player.ChaControl.gameObject.GetComponent<AdditionalAccessoryControlsController>().HandleVisibilityRules(hstart: true);
            hActors.Add(Singleton<Manager.Map>.Instance.Player.ChaControl);
            if (agent != null)
            {
#if DEBUG
                AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Sending event for: {agent?.ChaControl?.fileParam?.fullname}");
#endif
                agent.ChaControl.gameObject.GetComponent<AdditionalAccessoryControlsController>().HandleVisibilityRules(hstart: true);
                hActors.Add(agent.ChaControl);
            }
            if (Merchant != null)
            {
#if DEBUG
                AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Sending event for: {Merchant?.ChaControl?.fileParam?.fullname}");
#endif
                Merchant.ChaControl.gameObject.GetComponent<AdditionalAccessoryControlsController>().HandleVisibilityRules(hstart: true);
                hActors.Add(Merchant.ChaControl);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "EndProcADV")]
        static void HSceneEnd(HSceneManager __instance)
        {
#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo("End HScene");
#endif            
            foreach (ChaControl actor in hActors)
            {
                if (actor != null)
                {
#if DEBUG
                    AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Sending event for: {actor?.fileParam?.fullname}");
#endif
                    actor.gameObject.GetComponent<AdditionalAccessoryControlsController>().HandleVisibilityRules(hend: true);
                }
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(HSceneManager), "EndHScene")]
        static void HSceneClose(HSceneManager __instance)
        {
#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo("Close HScene");
#endif            
            foreach (ChaControl actor in hActors)
            {
                if (actor != null)
                {
#if DEBUG
                    AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Sending event for: {actor?.fileParam?.fullname}");
#endif
                    actor.gameObject.GetComponent<AdditionalAccessoryControlsController>().HandleVisibilityRules(startup: true);
                }
            }
            hActors.Clear();
        }

#endif


    }
}
