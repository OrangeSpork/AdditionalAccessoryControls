using AIChara;
using BepInEx;
using BepInEx.Configuration;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using System;
using System.Collections.Generic;
using System.Text;


namespace AdditionalAccessoryControls
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInDependency(KKABMX.Core.KKABMX_Core.GUID, KKABMX.Core.KKABMX_Core.Version)]
    public partial class AdditionalAccessoryControlsPlugin : BaseUnityPlugin
    {

        public const string GUID = "orange.spork.additionalaccessorycontrolsplugin";
        public const string PluginName = "Additional Accessory Controls";
        public const string Version = "1.0.0";

        public static AdditionalAccessoryControlsPlugin Instance { get; set; }  // Me

        internal BepInEx.Logging.ManualLogSource Log => Logger; // Me talk to universe

        // Config
        public static ConfigEntry<bool> TrimExcessAccessorySlotsOnSave { get; set; }

        // UX References
        public AccessoryControlWrapper<MakerToggle, bool> CharacterAccessoryControlWrapper { get; set; }
        public AccessoryControlWrapper<MakerToggle, bool> AutoMatchHairColorWrapper { get; set; }
        public AdditionalAccessoryUI UI { get; set; }

        public bool MakerControlsRegistered { get; set;}


        // More Accessories
        public static object MoreAccessoriesInstance { get; set; }
        public static Type MoreAccessoriesType { get; set; }    
        
        public AdditionalAccessoryControlsPlugin()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Singleton only.");
            }

            Instance = this;

            TrimExcessAccessorySlotsOnSave = Config.Bind("Options", "Trim More Accessory Slots", false, "Extra Accessory Slots Past Actual Accessories Removed on Save");

#if DEBUG
            Log.LogInfo("Additional Accessories Plugin Loaded");
#endif
        }

        public void Start()
        {
            DetectMoreAccessories();     
            if (MoreAccessoriesInstance == null || MoreAccessoriesType == null)
            {
                this.enabled = false;
                throw new Exception("More Accessories Required and Not Found");
            }

            // UI
            gameObject.AddComponent<AdditionalAccessoryUI>();
            CharacterApi.RegisterExtraBehaviour<AdditionalAccessoryControlsController>(GUID);
            MakerAPI.MakerStartedLoading += SetupMakerControls;
            MakerAPI.MakerExiting += CleanupMakerControls;

            // Hooks
            AdditionalAccessoryHooks.PatchMe();
            
        }        

        // Do Work on Entering/Leaving Maker
        public void SetupMakerControls(object sender, RegisterCustomControlsEvent eventData)
        {            
            CharacterAccessoryControlWrapper = MakerAPI.AddEditableAccessoryWindowControl<MakerToggle, bool>(new MakerToggle(new MakerCategory("Accessory", ""), "Character Accessory", this));
            AutoMatchHairColorWrapper = MakerAPI.AddEditableAccessoryWindowControl<MakerToggle, bool>(new MakerToggle(new MakerCategory("Accessory", ""), "Match Hair Color on Coord Load", this));

            MakerAPI.AddAccessoryWindowControl(new MakerButton("Visibility Rules", null, this)).OnClick.AddListener(VisibilityRulesListener);
            AccessoriesApi.SelectedMakerAccSlotChanged += UpdateVisibilityRulesUI;

            MakerAPI.AddAccessoryWindowControl(new MakerButton("Show", null, this)).OnClick.AddListener(ShowAccessory);
            MakerAPI.AddAccessoryWindowControl(new MakerButton("Hide", null, this)).OnClick.AddListener(HideAccessory);
            MakerControlsRegistered = true;
        }

        private void CleanupMakerControls(object sender, EventArgs args)
        {
            MakerControlsRegistered = false;
            AccessoriesApi.SelectedMakerAccSlotChanged -= UpdateVisibilityRulesUI;
            AdditionalAccessoryUI.Hide();
        }

        private void ShowAccessory()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;
            
            int currentSlot = AccessoriesApi.SelectedMakerAccSlot;
            MakerAPI.GetCharacterControl().SetAccessoryState(currentSlot, true);
        }

        private void HideAccessory()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            int currentSlot = AccessoriesApi.SelectedMakerAccSlot;
            MakerAPI.GetCharacterControl().SetAccessoryState(currentSlot, false);

        }

        // Handle Maker Acc Slot Change
        private void UpdateVisibilityRulesUI(object sender, AccessorySlotEventArgs args)
        {
            if (args.SlotIndex >= 0)
            {
#if DEBUG
                Log.LogInfo($"Changing Displayed Slot to {args.SlotIndex}");
#endif
                AdditionalAccessoryControlsController aacController = MakerAPI.GetCharacterControl().gameObject.GetComponent<AdditionalAccessoryControlsController>();

                if (args.SlotIndex >= aacController.SlotData.Length)
                {
#if DEBUG
                    Log.LogInfo($"New Slot Added: {args.SlotIndex}");
#endif
                    aacController.AddOrUpdateSlot(args.SlotIndex, AdditionalAccessorySlotData.EmptySlot(args.SlotIndex));
                }

                if (aacController.SlotData[args.SlotIndex] != null && !aacController.SlotData[args.SlotIndex].IsEmpty)
                {
#if DEBUG
                    Log.LogInfo($"New Slot Set: {aacController.SlotData[args.SlotIndex]}");
#endif
                    AdditionalAccessoryUI.Change(aacController.SlotData[args.SlotIndex], aacController.ChaControl);
                }
                else
                {
#if DEBUG
                    Log.LogInfo("No Slot Data or Slot Empty.");
#endif
                    AdditionalAccessoryUI.Hide();
                }
            }
        }

        // Open/Close the Visibility Rules UI
        private void VisibilityRulesListener()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            if (AdditionalAccessoryUI.Instance.enabled)
            {
                AdditionalAccessoryUI.Hide();
                return;
            }

            int currentSlot = AccessoriesApi.SelectedMakerAccSlot;
            if (currentSlot != -1)
            {
                AdditionalAccessoryControlsController aacController = MakerAPI.GetCharacterControl().gameObject.GetComponent<AdditionalAccessoryControlsController>();
                if (currentSlot >= aacController.SlotData.Length)
                {
#if DEBUG
                    Log.LogInfo("Invalid Slot (Beyond current length)");
#endif
                    AdditionalAccessoryUI.Hide();
                }
                else if (aacController.SlotData[currentSlot] != null && !aacController.SlotData[currentSlot].IsEmpty)
                {
#if DEBUG
                    Log.LogInfo($"Showing Rules for {MakerAPI.GetCharacterControl()?.chaFile?.parameter?.fullname}");
#endif
                    AdditionalAccessoryUI.Show(aacController.SlotData[currentSlot], MakerAPI.GetCharacterControl());
                }
                else
                {
#if DEBUG
                    Log.LogInfo("No Acc Slot Data and/or Empty");
#endif
                    AdditionalAccessoryUI.Hide();
                }
            }
            else
            {
#if DEBUG
                Log.LogInfo("No Slot Selected");
#endif
                AdditionalAccessoryUI.Hide();
            }
        }

        // Soft Link Reference to More Accessories
        private static void DetectMoreAccessories()
        {
            try
            {
                MoreAccessoriesType = Type.GetType("MoreAccessoriesAI.MoreAccessories, MoreAccessories", false);
                if (MoreAccessoriesType != null)
                    MoreAccessoriesInstance = BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(MoreAccessoriesType);
            }
            catch (Exception e)
            {
                MoreAccessoriesType = null;
                Instance.Logger.LogWarning($"More Accessories appears to be missing...{e}");
            }
        }

    }
}
