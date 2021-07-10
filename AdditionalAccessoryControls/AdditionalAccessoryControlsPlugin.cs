using AIChara;
using BepInEx;
using BepInEx.Configuration;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Maker.UI.Sidebar;
using KKAPI.Studio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UniRx;
using UnityEngine.SceneManagement;

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
        public const string Version = "1.1.1";

        public static AdditionalAccessoryControlsPlugin Instance { get; set; }  // Me

        internal BepInEx.Logging.ManualLogSource Log => Logger; // Me talk to universe

        // Config
        public static ConfigEntry<bool> TrimExcessAccessorySlotsOnSave { get; set; }
        public static ConfigEntry<bool> MoreAccessoriesDynamicBonesFix { get; set; }

        // UX References
        public AccessoryControlWrapper<MakerToggle, bool> CharacterAccessoryControlWrapper { get; set; }
        public AccessoryControlWrapper<MakerToggle, bool> AutoMatchHairColorWrapper { get; set; }
        public SidebarToggle CoordinateRulesToggle { get; set; }

        public bool MakerControlsRegistered { get; set; }

        public bool StudioSceneLoading { get; set; }


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
            MoreAccessoriesDynamicBonesFix = Config.Bind("Options", "Fix More Accessories Dynamic Bones", true, "Fix a Bug in More Accessories That Disables Dynamic Bones in More Accessory Slots");

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
            gameObject.AddComponent<AdditionalCoordinateUI>();
            gameObject.AddComponent<AdditionalAccessoryAdvancedParentUI>();
            CharacterApi.RegisterExtraBehaviour<AdditionalAccessoryControlsController>(GUID);
            MakerAPI.MakerStartedLoading += SetupMakerControls;
            MakerAPI.MakerExiting += CleanupMakerControls;

            // Hooks
            AdditionalAccessoryHooks.PatchMe();

            if (StudioAPI.InsideStudio)
            {
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                SceneManager.sceneLoaded += OnSceneLoaded;                
            }

        }

        public void RefreshAdvancedParentLabel()
        {
            ReloadCustomInterface(null, null);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
#if DEBUG
            Log.LogInfo($"Scene {scene.name} Loaded, Mode: {mode}");
#endif
            if (scene.name.Equals("StudioSceneLoad"))
            {
                StudioSceneLoading = true;
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
#if DEBUG
            Log.LogInfo($"Scene {scene.name} Unloaded");
#endif
            if (scene.name.Equals("StudioSceneLoad"))
            {
                StudioSceneLoading = false;
            }
        }


        private MakerText advancedParentLabel;
        // Do Work on Entering/Leaving Maker
        public void SetupMakerControls(object sender, RegisterCustomControlsEvent eventData)
        {            
            CharacterAccessoryControlWrapper = MakerAPI.AddEditableAccessoryWindowControl<MakerToggle, bool>(new MakerToggle(new MakerCategory("Accessory", ""), "Character Accessory", this));
            AutoMatchHairColorWrapper = MakerAPI.AddEditableAccessoryWindowControl<MakerToggle, bool>(new MakerToggle(new MakerCategory("Accessory", ""), "Match Hair Color on Coord Load", this));

            MakerAPI.AddAccessoryWindowControl(new MakerButton("Visibility Rules", null, this)).OnClick.AddListener(VisibilityRulesListener);
            AccessoriesApi.SelectedMakerAccSlotChanged += UpdateUI;
            AccessoriesApi.SelectedMakerAccSlotChanged += RefreshAdvancedParents;
            MakerAPI.ReloadCustomInterface += ReloadCustomInterface;

            MakerAPI.AddAccessoryWindowControl(new MakerButton("Show", null, this)).OnClick.AddListener(ShowAccessory);
            MakerAPI.AddAccessoryWindowControl(new MakerButton("Hide", null, this)).OnClick.AddListener(HideAccessory);

            advancedParentLabel = new MakerText("Adv Parent: None", null, this);
            MakerAPI.AddAccessoryWindowControl(advancedParentLabel);
            MakerAPI.AddAccessoryWindowControl(new MakerButton("Advanced Parent", null, this)).OnClick.AddListener(AdvancedParent);

            CoordinateRulesToggle = MakerAPI.AddSidebarControl(new SidebarToggle("Coordinate Visibility Rules", false, this));
            CoordinateRulesToggle.ValueChanged.Subscribe(b =>
            {
                ShowCoordinateRulesGUI(b);
            });

            MakerControlsRegistered = true;
        }

        private void CleanupMakerControls(object sender, EventArgs args)
        {
            MakerControlsRegistered = false;
            AccessoriesApi.SelectedMakerAccSlotChanged -= UpdateUI;
            AccessoriesApi.SelectedMakerAccSlotChanged -= RefreshAdvancedParents;
            AdditionalAccessoryUI.Hide();
        }

        private void RefreshAdvancedParents(object sender, EventArgs args)
        {
            StartCoroutine(MakerAPI.GetCharacterControl().gameObject.GetComponent<AdditionalAccessoryControlsController>().StartRefreshAdvancedParents());
        }

        private void AdvancedParent()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            if (AdditionalAccessoryAdvancedParentUI.Instance.enabled)
                AdditionalAccessoryAdvancedParentUI.Hide();
            else
            {

                int currentSlot = AccessoriesApi.SelectedMakerAccSlot;
                AdditionalAccessoryControlsController aacController = MakerAPI.GetCharacterControl().gameObject.GetComponent<AdditionalAccessoryControlsController>();
                AdditionalAccessoryAdvancedParentUI.Show(aacController.SlotData[currentSlot], MakerAPI.GetCharacterControl());
            }
        }

        private void ShowCoordinateRulesGUI(bool toggleState)
        {
#if DEBUG
            Log.LogInfo($"Show Coordinate Rules GUI {toggleState}");
#endif
            if (!toggleState)
                AdditionalCoordinateUI.Hide();
            else
            { 
                if (MakerAPI.InsideAndLoaded)
                    AdditionalCoordinateUI.Show();
            }
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

        private void ReloadCustomInterface(object sender, EventArgs eventArgs)
        {
            AdditionalAccessoryControlsController aacController = MakerAPI.GetCharacterControl().gameObject.GetComponent<AdditionalAccessoryControlsController>();
            int currentSlot = AccessoriesApi.SelectedMakerAccSlot;
            if (currentSlot != -1 && aacController != null)
            {
                advancedParentLabel.Text = $"Adv Parent: {aacController.SlotData[currentSlot].AdvancedParentShort}";
            }
            else
            {
                advancedParentLabel.Text = "Adv Parent: None";
            }
        }

        // Handle Maker Acc Slot Change
        private void UpdateUI(object sender, AccessorySlotEventArgs args)
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
                    advancedParentLabel.Text = $"Adv Parent: {aacController.SlotData[args.SlotIndex].AdvancedParentShort}";
                    AdditionalAccessoryAdvancedParentUI.Change(aacController.SlotData[args.SlotIndex], aacController.ChaControl);
                }
                else
                {
#if DEBUG
                    Log.LogInfo("No Slot Data or Slot Empty.");
#endif
                    advancedParentLabel.Text = "Adv Parent: None";
                    AdditionalAccessoryUI.Hide();
                    AdditionalAccessoryAdvancedParentUI.Hide();
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

        private void LateUpdate()
        {
            AdditionalAccessoryControlDynamicBoneUpdateManager.ReapInactiveDynamicBones();
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
