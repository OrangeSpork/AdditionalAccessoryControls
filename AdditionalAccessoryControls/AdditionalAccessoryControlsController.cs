using AIChara;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using MessagePack;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using static AdditionalAccessoryControls.AdditionalAccessoryCoordinateData;
using static AdditionalAccessoryControls.AdditionalAccessorySlotData;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryControlsController : CharaCustomFunctionController
    {

        // Convenience
        BepInEx.Logging.ManualLogSource Log = AdditionalAccessoryControlsPlugin.Instance.Log;


        // Slot Data Sets
        private AdditionalAccessorySlotData[] slotData;  // Mirrors character accessory data
        private AdditionalAccessorySlotData[] coordinateSlotData; // Additional data from coordinate set, merged to slot data during load process

        public AdditionalAccessoryCoordinateData CoordinateOverrideData { get; set; }

        public AdditionalAccessorySlotData[] SlotData // Always enable exhibitionism
        {
            get => slotData;
        }

        public AdditionalAccessoryMaterialEditorHelper MaterialEditorHelper { get; set; }
        public AdditionalAccessoryDynamicBonesHelper DBHelper { get; set; }

        private AdditionalAccessoryBoneEffect boneEffect;

        private bool loading = false;


        // Register Handlers
        protected override void OnEnable()
        {
            MaterialEditorHelper = new AdditionalAccessoryMaterialEditorHelper(ChaControl);
            DBHelper = new AdditionalAccessoryDynamicBonesHelper(ChaControl);

            AccessoriesApi.AccessoryKindChanged += UpdateCharacterAccessories;
            AccessoriesApi.AccessoryTransferred += UpdateCharacterAccessories;
#if DEBUG
            Log.LogInfo("Accessory Handlers Registered");
#endif
            if (MakerAPI.InsideMaker)
            {
#if DEBUG
                Log.LogInfo("Start Maker Handlers Registration");
#endif
                StartCoroutine(RegisterMakerControls());                                
            }
            boneEffect = new AdditionalAccessoryBoneEffect();
            ChaControl.GetComponent<BoneController>().AddBoneEffect(boneEffect);

            CoordinateOverrideData = new AdditionalAccessoryCoordinateData();

            base.OnEnable();
        }

        IEnumerator RegisterMakerControls()
        {
            yield return new WaitUntil(() => AdditionalAccessoryControlsPlugin.Instance.MakerControlsRegistered);

            AdditionalAccessoryControlsPlugin.Instance.CharacterAccessoryControlWrapper.ValueChanged += UpdateCharacterAccessorialToggle;
            AdditionalAccessoryControlsPlugin.Instance.AutoMatchHairColorWrapper.ValueChanged += UpdateMatchHairAccessorialToggle;
#if DEBUG
            Log.LogInfo("Maker Handlers Registered");
#endif
        }

        // De-Register Handlers
        protected void OnDisable()
        {
            AccessoriesApi.AccessoryKindChanged -= UpdateCharacterAccessories;
            AccessoriesApi.AccessoryTransferred -= UpdateCharacterAccessories;

#if DEBUG
            Log.LogInfo("Accessory Handlers De-Registered");
#endif
            if (AdditionalAccessoryControlsPlugin.Instance.CharacterAccessoryControlWrapper != null)
                AdditionalAccessoryControlsPlugin.Instance.CharacterAccessoryControlWrapper.ValueChanged -= UpdateCharacterAccessorialToggle;
            if (AdditionalAccessoryControlsPlugin.Instance.AutoMatchHairColorWrapper != null) 
                AdditionalAccessoryControlsPlugin.Instance.AutoMatchHairColorWrapper.ValueChanged -= UpdateMatchHairAccessorialToggle;
#if DEBUG
            Log.LogInfo("Maker Handlers De-Registration");
#endif
        }


        // UI Event Handlers
        private void UpdateMatchHairAccessorialToggle(object sender, AccessoryWindowControlValueChangedEventArgs<bool> args)
        {
            if (loading)
            {
                return;
            }
#if DEBUG
            Log.LogInfo($"Setting MatchHair Slot: {args.SlotIndex} To: {args.NewValue}");
#endif
            if (slotData != null && slotData.Length > args.SlotIndex)
            {
                slotData[args.SlotIndex].AutoMatchBackHairColor = args.NewValue;
            }
        }

        private void UpdateCharacterAccessorialToggle(object sender, AccessoryWindowControlValueChangedEventArgs<bool> args)
        {
            if (loading)
            {
                return;
            }
#if DEBUG
            Log.LogInfo($"Setting CharAcc Slot: {args.SlotIndex} To: {args.NewValue}");
#endif
            if (slotData != null && slotData.Length > args.SlotIndex)
            {
                slotData[args.SlotIndex].CharacterAccessory = args.NewValue;
            }
        }

        private void UpdateCharacterAccessories(object sender, EventArgs args)
        {
            if (loading)
            {
                return;
            }
            if (args.GetType().Equals(typeof(AccessorySlotEventArgs)))
            {
                AccessorySlotEventArgs slotArgs = (AccessorySlotEventArgs)args;
                AdditionalAccessoryControlsPlugin.Instance.CharacterAccessoryControlWrapper.SetSelectedValue(false);
                AdditionalAccessoryControlsPlugin.Instance.AutoMatchHairColorWrapper.SetSelectedValue(false);

                ChaFileAccessory.PartsInfo accessoryParts = null;
                ListInfoBase accessoryInfo = null;
                if (slotArgs.SlotIndex < 20)
                {
                    accessoryParts = ChaControl.nowCoordinate.accessory.parts[slotArgs.SlotIndex];
                    accessoryInfo = ChaControl.infoAccessory[slotArgs.SlotIndex];
                }
                else
                {
                    accessoryParts = GetMoreAccessorialPartInfo(slotArgs.SlotIndex - 20);
                    accessoryInfo = GetMoreAccessorialAccInfo(slotArgs.SlotIndex - 20);
                }


                if (accessoryParts == null || accessoryInfo == null)
                    AddOrUpdateSlot(slotArgs.SlotIndex, AdditionalAccessorySlotData.EmptySlot(slotArgs.SlotIndex));
                else
                    AddOrUpdateSlot(slotArgs.SlotIndex, AdditionalAccessorySlotData.NonCharacterAccessorySlot(slotArgs.SlotIndex, accessoryInfo.Name, accessoryParts));
#if DEBUG
                Log.LogInfo($"Changed Type, New Slot: {slotData[slotArgs.SlotIndex]}");
#endif
                ChaControl.SetAccessoryState(slotArgs.SlotIndex, true);
                HandleVisibilityRules(accessory: true);
            }
            else if (args.GetType().Equals(typeof(AccessoryTransferEventArgs)))
            {
                AccessoryTransferEventArgs transferArgs = (AccessoryTransferEventArgs)args;

                AddOrUpdateSlot(transferArgs.DestinationSlotIndex, AdditionalAccessorySlotData.Copy(slotData[transferArgs.SourceSlotIndex], transferArgs.DestinationSlotIndex));

                ChaFileAccessory.PartsInfo accessoryParts = null;
                if (transferArgs.DestinationSlotIndex < 20)
                {
                    accessoryParts = ChaControl.nowCoordinate.accessory.parts[transferArgs.DestinationSlotIndex];
                }
                else
                {
                    accessoryParts = GetMoreAccessorialPartInfo(transferArgs.DestinationSlotIndex - 20);
                }

                slotData[transferArgs.DestinationSlotIndex].PartsInfo = accessoryParts;
                AdditionalAccessoryControlsPlugin.Instance.CharacterAccessoryControlWrapper.SetValue(transferArgs.DestinationSlotIndex, slotData[transferArgs.SourceSlotIndex].CharacterAccessory);
                AdditionalAccessoryControlsPlugin.Instance.AutoMatchHairColorWrapper.SetValue(transferArgs.DestinationSlotIndex, slotData[transferArgs.SourceSlotIndex].AutoMatchBackHairColor);
#if DEBUG
                Log.LogInfo($"Transferred Slot Source: {slotData[transferArgs.SourceSlotIndex]} Dest: {slotData[transferArgs.DestinationSlotIndex]}");
#endif
                ChaControl.SetAccessoryState(transferArgs.DestinationSlotIndex, true);
                HandleVisibilityRules(accessory: true);
            }
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
#if DEBUG
            Log.LogInfo("Card Being Saved");
            dumpSlotData();
#endif

            if (AdditionalAccessoryControlsPlugin.TrimExcessAccessorySlotsOnSave.Value)
            {                
                slotData = TrimMoreAccessorySlots(SlotData);

                if (AccessoriesApi.SelectedMakerAccSlot > slotData.Length)
                {
                    GameObject slot1obj = GameObject.Find("CharaCustom/CustomControl/CanvasMain/SubMenu/SubMenuAccessory/Scroll View/Viewport/Content/Category/CategoryTop/Slot01");
                    if (slot1obj != null)
                        slot1obj.GetComponent<UI_ButtonEx>().onClick.Invoke();
                }

    
#if DEBUG
                Log.LogInfo("After Trim Operation");
                dumpSlotData();
#endif
            }

            var data = new PluginData();
            data.data["accessoryData"] = MessagePackSerializer.Serialize(slotData);
            data.data["overrideData"] = MessagePackSerializer.Serialize(CoordinateOverrideData);
            SetExtendedData(data);            
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState)
        {
            loading = true;
            try
            {

#if DEBUG
                Log.LogInfo($"Coordinate Being Loaded: {coordinate.coordinateName} MS: {maintainState}");
                dumpSlotData();
                dumpCurrentAccessories();
#endif

                // Load Coordinate Extension Data

                coordinateSlotData = null;

                if (!maintainState)
                {
                    PluginData coordinateData = GetCoordinateExtendedData(coordinate);
                    if (coordinateData != null)
                    {
                        // Grab and deserialize
                        byte[] coordinateAccessorySlotBinary = (byte[])coordinateData.data["coordinateAccessoryData"];
                        if (coordinateAccessorySlotBinary != null && coordinateAccessorySlotBinary.Length > 0)
                        {
#if DEBUG
                            Log.LogInfo($"Found Coordinate Extended Data {coordinateAccessorySlotBinary.Length}");
#endif
                            coordinateSlotData = MessagePackSerializer.Deserialize<AdditionalAccessorySlotData[]>(coordinateAccessorySlotBinary);

                            // Inflate PartInfo Data
                            foreach (AdditionalAccessorySlotData coordinateSlot in coordinateSlotData)
                            {
                                if (coordinateSlot.SlotNumber < 20)
                                {
                                    coordinateSlot.PartsInfo = ChaControl.nowCoordinate.accessory.parts[coordinateSlot.SlotNumber];
                                }
                                else
                                {
                                    coordinateSlot.PartsInfo = GetMoreAccessorialPartInfo(coordinateSlot.SlotNumber - 20);
                                }
                            }
                        }
                        else
                        {
#if DEBUG
                            Log.LogInfo("No Coordinate Extended Data Binary");
#endif
                            coordinateSlotData = null;
                        }
                        byte[] coordinateOverrideBinary = coordinateData.data.ContainsKey("coordinateOverrideData") ? (byte[])coordinateData.data["coordinateOverrideData"] : null;
                        if (coordinateOverrideBinary != null && coordinateOverrideBinary.Length > 0)
                        {
#if DEBUG
                            Log.LogInfo($"Found Coordinate Override Data {coordinateOverrideBinary.Length}");
#endif
                            CoordinateOverrideData = MessagePackSerializer.Deserialize<AdditionalAccessoryCoordinateData>(coordinateOverrideBinary);
#if DEBUG
                            Log.LogInfo($"Loaded Coordinate Override Data {CoordinateOverrideData}");
#endif
                        }
                        else
                        {
                            CoordinateOverrideData = new AdditionalAccessoryCoordinateData();
                        }
                    }
                    else
                    {
                        CoordinateOverrideData = new AdditionalAccessoryCoordinateData();
#if DEBUG
                        Log.LogInfo("Coordinate Data Missing");
#endif
                    }
                }

#if DEBUG
                dumpCoordinateSlotData();
#endif

                // Merge Coordinate Data to Slot Data

                // First copy the coordinate data
                List<AdditionalAccessorySlotData> newSlotData = new List<AdditionalAccessorySlotData>();
                newSlotData.AddRange(coordinateSlotData == null ? buildFromAccessories(AccessoriesApi.GetAccessoryObjects(ChaControl)) : coordinateSlotData);
#if DEBUG
                Log.LogInfo("Merge Coordinate Data");
                dumpSlotData(newSlotData.ToArray());

#endif
                List<AdditionalAccessorySlotData> charaSlots = new List<AdditionalAccessorySlotData>();
                List<Tuple<int, int>> movedSlots = new List<Tuple<int, int>>();

                // Copy character accessories from character slots
                foreach (AdditionalAccessorySlotData slot in slotData)
                {
                    if (slot.CharacterAccessory)
                    {
                        int newSlotNumber = FindNextEmptySlot(newSlotData);
#if DEBUG
                        Log.LogInfo($"Assigning old slot {slot.SlotNumber} to new slot {newSlotNumber}");
#endif
                        if (newSlotNumber == -1)
                        {
                            newSlotNumber = AddMoreAccessoriesPart(slot.PartsInfo, -1) + 20;
                            AdditionalAccessorySlotData newSlot = AdditionalAccessorySlotData.Copy(slot, newSlotNumber, true);
                            newSlotData.Add(newSlot);
                            charaSlots.Add(newSlot);
                            movedSlots.Add(new Tuple<int, int>(slot.SlotNumber, newSlotNumber));
                        }
                        else
                        {
                            if (newSlotNumber < 20)
                            {
                                ChaControl.nowCoordinate.accessory.parts[newSlotNumber] = slot.PartsInfo;
                                AdditionalAccessorySlotData newSlot = AdditionalAccessorySlotData.Copy(slot, newSlotNumber, true);
                                newSlotData[newSlotNumber] = newSlot;
                                charaSlots.Add(newSlot);
                                movedSlots.Add(new Tuple<int, int>(slot.SlotNumber, newSlotNumber));
                            }
                            else
                            {
                                AddMoreAccessoriesPart(slot.PartsInfo, newSlotNumber - 20);
                                AdditionalAccessorySlotData newSlot = AdditionalAccessorySlotData.Copy(slot, newSlotNumber, true);
                                newSlotData[newSlotNumber] = newSlot;
                                charaSlots.Add(newSlot);
                                movedSlots.Add(new Tuple<int, int>(slot.SlotNumber, newSlotNumber));
                            }
                        }
                    }
                }

                UpdateMovedAccessoryLinks(charaSlots);

                // Make merged list the new character accessory set
                slotData = newSlotData.ToArray();

                if (KKAPI.Maker.MakerAPI.InsideAndLoaded)
                {
                    // Resync UI Extension Checkboxes
                    foreach (AdditionalAccessorySlotData slot in slotData)
                    {
                        try
                        {
                            AdditionalAccessoryControlsPlugin.Instance.CharacterAccessoryControlWrapper.SetValue(slot.SlotNumber, slot.CharacterAccessory);
                            AdditionalAccessoryControlsPlugin.Instance.AutoMatchHairColorWrapper.SetValue(slot.SlotNumber, slot.AutoMatchBackHairColor);
                        }
                        catch
                        {
                            Log.LogWarning($"Tried to init UI value for slot: {slot.SlotNumber} but there was no control present.");
                        }
                    }
                }

                // Trigger game to update
                if (KKAPI.Maker.MakerAPI.InsideAndLoaded)
                {
                    ChaControl.AssignCoordinate();
                    ChaControl.ChangeAccessory(true);
                }
                else
                {
                    ChaControl.ChangeAccessory(true);
                }

                // Update related plugins that I just broke
                MaterialEditorHelper.UpdateOnCoordinateLoadApply(coordinate, movedSlots);
                DBHelper.UpdateOnCoordinateLoadApply(coordinate, movedSlots);

#if DEBUG
                Log.LogInfo("New Slot Data");
                dumpSlotData();
                dumpCurrentAccessories();
#endif

                // Handle Match Hair on Coordinate Load Setting
                int i = 0;
                foreach (AdditionalAccessorySlotData slot in slotData)
                {
                    CmpAccessory cmpAccessory = null;
                    if (i < 20)
                    {
                        cmpAccessory = ChaControl.cmpAccessory[i];
                    }
                    else
                    {
                        cmpAccessory = GetMoreAccessorialCmpAccessory(i - 20);
                    }
                    if (cmpAccessory != null && cmpAccessory.typeHair && slotData[i].AutoMatchBackHairColor)
                    {
                        HandleAutoMatchHairColor(slot.PartsInfo, 0, i);
                    }
                    i++;
                }

#if DEBUG
                Log.LogInfo("Coordinate Load - Setting all Accessories ON");
#endif
                ChaControl.SetAccessoryStateAll(true);

                // Wait a few frames for other players to finish up
                StartCoroutine(StartHandleVisibilityRules());
            }
            catch (Exception e)
            {
                Log.LogWarning($"Error during Coordinate Load: {e.Message} {e.StackTrace}");
            }
            loading = false;
        }

        // Nothing to do on this one
        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
        {
        }

        private IEnumerator StartHandleVisibilityRules()
        {
            yield return null;
            yield return null;
            yield return null;
            HandleVisibilityRules(startup: true);
        }


        // Save coordinate - weed out character accessories
        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
        {
            loading = true;
            try
            {
#if DEBUG
                Log.LogInfo($"Coordinate Being Saved: {coordinate.coordinateName}");
#endif

                // Populate coordinate slot data from character slots
                coordinateSlotData = new AdditionalAccessorySlotData[slotData.Length];
                int i = 0;
                foreach (AdditionalAccessorySlotData slot in slotData)
                {
                    coordinateSlotData[i] = AdditionalAccessorySlotData.Copy(slot, slot.SlotNumber);
                    i++;
                }

#if DEBUG
                Log.LogInfo("BEFORE");
                dumpCoordinateSlotData();
#endif

                List<int> slotsRemoved = new List<int>();
                // Remove character accessories
                foreach (AdditionalAccessorySlotData coordinateSlot in coordinateSlotData)
                {
                    if (coordinateSlot.CharacterAccessory)
                    {
                        slotsRemoved.Add(coordinateSlot.SlotNumber);
                        coordinateSlot.MakeEmpty();                        
                    }
                }

                // Clear any accessory link visibility rules going from non-character accessories to now removed character accessories
                PruneDeadAccessoryLinks(coordinateSlotData);

                if (AdditionalAccessoryControlsPlugin.TrimExcessAccessorySlotsOnSave.Value)
                {
                    coordinateSlotData = TrimMoreAccessorySlots(coordinateSlotData);
#if DEBUG
                    Log.LogInfo("After Trim Operation");
#endif
                }

#if DEBUG
                Log.LogInfo("AFTER");
                dumpCoordinateSlotData();
#endif
                
                MaterialEditorHelper.UpdateOnCoordinateSave(coordinate, slotsRemoved);
                DBHelper.UpdateOnCoordinateSave(coordinate, slotsRemoved);

                // And Save
                var data = new PluginData();
                data.data["coordinateAccessoryData"] = MessagePackSerializer.Serialize(coordinateSlotData);
                data.data["coordinateOverrideData"] = MessagePackSerializer.Serialize(CoordinateOverrideData);
                SetCoordinateExtendedData(coordinate, data);
            }
            catch (Exception e)
            {
                Log.LogWarning($"Error during Coordinate Save: {e.Message} {e.StackTrace}");
            }
            loading = false;
        }

        // Load Handler
        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            loading = true;
            try
            {
                CharacterLoadFlags flags = MakerAPI.GetCharacterLoadFlags();
#if DEBUG
                Log.LogInfo($"Reload: {maintainState}");
                Log.LogInfo(string.Format("Load Flags: Body {0} Clothes {1} Face {2} Hair {3} Parameters {4}", flags?.Body, flags?.Clothes, flags?.Face, flags?.Hair, flags?.Parameters));
                dumpCurrentAccessories();
#endif

                // Only do stuff if the clothes changed
                if (flags != null && !flags.Clothes)
                {
#if DEBUG
                    Log.LogInfo("Clothes weren't reloaded...nothing to do.");
#endif
                    loading = false;
                    return;
                }

                // Clear coordinate data - we're loading a scratch character     
                coordinateSlotData = null;

                // Still not sure when this isn't false...
                if (!maintainState)
                {
                    // load and deserialize
                    PluginData data = GetExtendedData();
                    if (data != null)
                    {
                        byte[] accessorySlotBinary = (byte[])data.data["accessoryData"];
                        if (accessorySlotBinary != null && accessorySlotBinary.Length > 0)
                        {
                            slotData = MessagePackSerializer.Deserialize<AdditionalAccessorySlotData[]>(accessorySlotBinary);

                            // Inflate part data
                            foreach (AdditionalAccessorySlotData slot in slotData)
                            {
                                if (slot.SlotNumber < 20)
                                {
                                    slot.PartsInfo = ChaControl.nowCoordinate.accessory.parts[slot.SlotNumber];
                                }
                                else
                                {
                                    slot.PartsInfo = GetMoreAccessorialPartInfo(slot.SlotNumber - 20);
                                }
                            }
                        }
                        else
                        {
                            // No Data, instantiate from current accessories
                            slotData = buildFromAccessories(AccessoriesApi.GetAccessoryObjects(ChaControl));
                        }
                        byte[] overrideDataBinary = data.data.ContainsKey("overrideData") ? (byte[])data.data["overrideData"] : null;
                        if (overrideDataBinary != null && overrideDataBinary.Length > 0)
                        {
                           
#if DEBUG
                            Log.LogInfo($"Found Coordinate Override Data {overrideDataBinary.Length}");
#endif
                            CoordinateOverrideData = MessagePackSerializer.Deserialize<AdditionalAccessoryCoordinateData>(overrideDataBinary);
#if DEBUG
                            Log.LogInfo($"Loaded Coordinate Override Data {CoordinateOverrideData}");
#endif                           
                        }
                        else
                        {
                            CoordinateOverrideData = new AdditionalAccessoryCoordinateData();
                        }
                    }
                    else
                    {
                        CoordinateOverrideData = new AdditionalAccessoryCoordinateData();
                        slotData = buildFromAccessories(AccessoriesApi.GetAccessoryObjects(ChaControl));
#if DEBUG
                        Log.LogInfo("Extended Data Missing");                        
                        Log.LogInfo("No Plugin Data Present, Building From Equipped Accessories");
                        dumpSlotData();
#endif
                    }

                    
                }

                // If we have data, sync the UI checkboxes
                if (slotData != null)
                {
#if DEBUG
                    dumpSlotData();
#endif
                    if (KKAPI.Maker.MakerAPI.InsideAndLoaded)
                    {
                        foreach (AdditionalAccessorySlotData slot in slotData)
                        {
                            try
                            {
                                AdditionalAccessoryControlsPlugin.Instance.CharacterAccessoryControlWrapper.SetValue(slot.SlotNumber, slot.CharacterAccessory);
                                AdditionalAccessoryControlsPlugin.Instance.AutoMatchHairColorWrapper.SetValue(slot.SlotNumber, slot.AutoMatchBackHairColor);
                            }
                            catch
                            {
                                Log.LogWarning($"Tried to init UI value for slot: {slot.SlotNumber} but there was no control present.");
                            }
                        }
                    }
                }
                else
                {
                    // No data - Build from current equipped accessories
                    buildFromAccessories(AccessoriesApi.GetAccessoryObjects(ChaControl));
                }

                if (!KKAPI.Studio.StudioAPI.InsideStudio)
                {
#if DEBUG
                    Log.LogInfo("Initial Load - All Accessories ON");
#endif
                    ChaControl.SetAccessoryStateAll(true);
                }
                HandleVisibilityRules(startup: true);
            }
            catch (Exception e)
            {
                Log.LogWarning($"Error during Reload: {e.Message} {e.StackTrace}");
            }
            loading = false;
        }

        // Alternative Entrance?
        protected override void OnReload(GameMode currentGameMode)
        {
           // OnReload(currentGameMode, false);
        }

        // Logic Handlers
        private void HandleAutoMatchHairColor(ChaFileAccessory.PartsInfo partsInfo, int hairSlot, int slotNumber)
        {
#if DEBUG
            Log.LogInfo($"Setting Hair Color for Slot {slotNumber}");
#endif

            partsInfo.colorInfo[0].color = ChaControl.fileHair.parts[hairSlot].baseColor;
            partsInfo.colorInfo[1].color = ChaControl.fileHair.parts[hairSlot].topColor;
            partsInfo.colorInfo[2].color = ChaControl.fileHair.parts[hairSlot].underColor;
            partsInfo.colorInfo[3].color = ChaControl.fileHair.parts[hairSlot].specular;

            partsInfo.colorInfo[0].smoothnessPower = ChaControl.fileHair.parts[hairSlot].smoothness;
            partsInfo.colorInfo[0].metallicPower = ChaControl.fileHair.parts[hairSlot].metallic;

            ChaControl.ChangeHairTypeAccessoryColor(slotNumber);

        }               

        // Builds out the plugin slot data from current accessories
        private AdditionalAccessorySlotData[] buildFromAccessories(GameObject[] accessories)
        {
            AdditionalAccessorySlotData[] slotData = new AdditionalAccessorySlotData[accessories.Length];
            int i = 0;
            foreach (GameObject accessory in accessories)
            {
                if (accessory == null)
                {
                    slotData[i] = AdditionalAccessorySlotData.EmptySlot(i);
                }
                else
                {
                    ListInfoComponent infoAccessory = accessory.GetComponent<ListInfoComponent>();
                    if (i < 20)
                    {
                        slotData[i] = AdditionalAccessorySlotData.NonCharacterAccessorySlot(i, infoAccessory.data.Name, ChaControl.nowCoordinate.accessory.parts[i]);
                    }
                    else
                    {
                        slotData[i] = AdditionalAccessorySlotData.NonCharacterAccessorySlot(i, infoAccessory.data.Name, GetMoreAccessorialPartInfo(i - 20));
                    }
                }


                i++;
            }
            return slotData;
        }

        private bool hidingAccessoriesForPicture = false;
        // Following methods used when saving a coordinate card so character accessories don't end up in the coordinate card picture
        public void HideCharacterAccessories()
        {
            hidingAccessoriesForPicture = true;
            foreach (AdditionalAccessorySlotData slot in slotData)
            {
                if (slot.CharacterAccessory)
                {
                    ChaControl.SetAccessoryState(slot.SlotNumber, false);
                }
            }
        }

        public void UnHideCharacterAccessories()
        {
            hidingAccessoriesForPicture = false;
            foreach (AdditionalAccessorySlotData slot in slotData)
            {
                if (slot.CharacterAccessory)
                {
                    ChaControl.SetAccessoryState(slot.SlotNumber, true);
                }
            }            
        }

        // state vars
        private List<ChaFileAccessory.PartsInfo> originalMACCPartsList;
        private ChaFileAccessory.PartsInfo[] originalPartsArray;

        // When saving a coordinate card, first remove the character accessories before saving
        public void ClearCharacterAccessoriesForSave(ChaFileCoordinate chaFileCoordinate)
        {
            int i = 0;
            originalPartsArray = CopyPartsArray(chaFileCoordinate.accessory.parts);
            foreach (AdditionalAccessorySlotData slot in slotData)
            {
                if (slot.CharacterAccessory)
                {
                    if (i < 20)
                    {
#if DEBUG
                        Log.LogInfo($"Clearing normal character accessory, slot: {slot}");
#endif
                        chaFileCoordinate.accessory.parts[i].MemberInit();
                    }
                    else
                    {
#if DEBUG
                        Log.LogInfo($"Clearing more acc. character accessory, slot: {slot}");
#endif
                        SetMoreAccessorialPartToEmpty(i - 20);
                    }
                }
                i++;
            }
        }

        // Then put 'em back
        public void RestoreCharacterAccessoriesForSave(ChaFileCoordinate chaFileCoordinate)
        {
            chaFileCoordinate.accessory.parts = originalPartsArray;
            originalPartsArray = null;

            if (originalMACCPartsList != null)
            {
#if DEBUG
                Log.LogInfo("Restoring Original MACC Parts");
#endif
                IDictionary charAdditionalData = (IDictionary)additionalDataField.GetValue(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance);

                foreach (DictionaryEntry entry in charAdditionalData)
                {
                    if (entry.Key.Equals(ChaControl.chaFile))
                    {
                        List<ChaFileAccessory.PartsInfo> partsList = (List<ChaFileAccessory.PartsInfo>)partsField.GetValue(entry.Value);
                        partsList.Clear();
                        partsList.AddRange(originalMACCPartsList);
                        break;
                    }
                }

                originalMACCPartsList = null;
            }

            MaterialEditorHelper.RestoreSnapshot(chaFileCoordinate);
            DBHelper.RestoreSnapshot(chaFileCoordinate);

            // Trigger game to update
            ChaControl.AssignCoordinate();
            ChaControl.ChangeAccessory(true);            

#if DEBUG
            dumpCurrentAccessories();
            dumpSlotData();
#endif


            UnHideCharacterAccessories();
            HandleVisibilityRules(clothes: true, startup: true);
        }

        // More Accessory Hooks
        private static FieldInfo additionalDataField = AccessTools.Field(AdditionalAccessoryControlsPlugin.MoreAccessoriesType, "_charAdditionalData");
        private static FieldInfo partsField = AccessTools.Field(AdditionalAccessoryControlsPlugin.MoreAccessoriesType.GetNestedType("AdditionalData", AccessTools.all), "parts");
        private static FieldInfo objectsField = AccessTools.Field(AdditionalAccessoryControlsPlugin.MoreAccessoriesType.GetNestedType("AdditionalData", AccessTools.all), "objects");        
        private static Type accessoryObjectType = AdditionalAccessoryControlsPlugin.MoreAccessoriesType.GetNestedType("AdditionalData", AccessTools.all).GetNestedType("AccessoryObject", AccessTools.all);
        private static FieldInfo cmpAccessoryField = AccessTools.Field(AdditionalAccessoryControlsPlugin.MoreAccessoriesType.GetNestedType("AdditionalData", AccessTools.all).GetNestedType("AccessoryObject", AccessTools.all), "cmp");
        private static FieldInfo listInfoBaseField = AccessTools.Field(AdditionalAccessoryControlsPlugin.MoreAccessoriesType.GetNestedType("AdditionalData", AccessTools.all).GetNestedType("AccessoryObject", AccessTools.all), "info");
        private static FieldInfo showField = AccessTools.Field(AdditionalAccessoryControlsPlugin.MoreAccessoriesType.GetNestedType("AdditionalData", AccessTools.all).GetNestedType("AccessoryObject", AccessTools.all), "show");
        private static FieldInfo objField = AccessTools.Field(AdditionalAccessoryControlsPlugin.MoreAccessoriesType.GetNestedType("AdditionalData", AccessTools.all).GetNestedType("AccessoryObject", AccessTools.all), "obj");
        private static MethodInfo updateMakerUIMethod = AccessTools.Method(AdditionalAccessoryControlsPlugin.MoreAccessoriesType, "UpdateMakerUI");
        private static MethodInfo updateStudioUIMethod = AccessTools.Method(AdditionalAccessoryControlsPlugin.MoreAccessoriesType, "UpdateStudioUI");
        private static MethodInfo updateHUIMethod = AccessTools.Method(AdditionalAccessoryControlsPlugin.MoreAccessoriesType, "UpdateHUI");

        private static FieldInfo charaInfoPanelField = AccessTools.Field(typeof(ManipulatePanelCtrl), "charaPanelInfo");
        private static FieldInfo mpCharCtrlField = AccessTools.Field(typeof(ManipulatePanelCtrl).GetNestedType("CharaPanelInfo", AccessTools.all), "m_MPCharCtrl");
        private static FieldInfo stateInfoField = AccessTools.Field(typeof(MPCharCtrl), "stateInfo");
        private static FieldInfo accessoriesInfoField = AccessTools.Field(typeof(MPCharCtrl).GetNestedType("StateInfo", AccessTools.all), "accessoriesInfo");

        // Really Illusion, you don't bind your own char controls?
        private void UpdateStudioUI()
        {
            var charaPanelInfo = charaInfoPanelField.GetValue(Studio.Studio.Instance.manipulatePanelCtrl);
            MPCharCtrl mpCharCtrl = (MPCharCtrl)mpCharCtrlField.GetValue(charaPanelInfo);
            var stateInfo = stateInfoField.GetValue(mpCharCtrl);
            MPCharCtrl.AccessoriesInfo accessoriesInfo = (MPCharCtrl.AccessoriesInfo)accessoriesInfoField.GetValue(stateInfo);
            foreach (OCIChar chara in KKAPI.Studio.StudioAPI.GetSelectedCharacters())
            {
                if (chara != null)
                {
                    accessoriesInfo.UpdateInfo(chara);
                }
            }            
        }

        private void UpdateMoreAccessorialUI()
        {
            try
            {
                if (KKAPI.Maker.MakerAPI.InsideAndLoaded)
                {
                    updateMakerUIMethod.Invoke(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance, null);
                }
                else if (KKAPI.Studio.StudioAPI.InsideStudio)
                {
                    updateStudioUIMethod.Invoke(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance, null);
                }
                else if (Manager.HSceneManager.isHScene)
                {
                    updateHUIMethod.Invoke(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance, null);
                }
            }
            catch (Exception e)
            {
                Log.LogWarning($"Unable to update More Accessorial UI, may be out of sync. {e.Message} {e.StackTrace}");
            }
        }

        private bool GetMoreAccessorySlotStatus(int slot)
        {
            IDictionary charAdditionalData = (IDictionary)additionalDataField.GetValue(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance);
            foreach (DictionaryEntry entry in charAdditionalData)
            {
                if (entry.Key.Equals(ChaControl.chaFile))
                {
                    IList objectList = (IList)objectsField.GetValue(entry.Value);
                    if (slot >= objectList.Count)
                    {
                        return false;
                    }
                    else
                    {
                        return (bool)showField.GetValue(objectList[slot]);
                    }
                }
            }
            return false;
        }

        private AdditionalAccessorySlotData[] TrimMoreAccessorySlots(AdditionalAccessorySlotData[] slotData)
        {
            int lastAccessoryIndex = Array.FindLastIndex(slotData, slot => !slot.IsEmpty);
            int lastActualSlotIndex = slotData.Length - 1;
            
            if (lastActualSlotIndex >= 20 && lastAccessoryIndex < lastActualSlotIndex)
            {
                int trimFromSlot = Math.Max(20, lastAccessoryIndex + 1);
#if DEBUG
                Log.LogInfo($"Trimming Last Accessory Index {lastAccessoryIndex} Last Actual Slot {lastActualSlotIndex} Trim From Slot {trimFromSlot}");
#endif
                RemoveLastMoreAccessorySlots(trimFromSlot - 20);
                Array.Resize(ref slotData, trimFromSlot + 1);
            }
            return slotData;
        }

        private void RemoveLastMoreAccessorySlots(int trimFromSlot)
        {
            IDictionary charAdditionalData = (IDictionary)additionalDataField.GetValue(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance);
            foreach (DictionaryEntry entry in charAdditionalData)
            {
                if (entry.Key.Equals(ChaControl.chaFile))
                {
                    List<ChaFileAccessory.PartsInfo> partsList = (List<ChaFileAccessory.PartsInfo>)partsField.GetValue(entry.Value);
                    int beforeTrimSize = partsList.Count;
                    partsList.RemoveRange(trimFromSlot, partsList.Count - trimFromSlot);

                    IList objectList = (IList)objectsField.GetValue(entry.Value);
                    for (int i = objectList.Count - 1; i >= trimFromSlot; i--)
                    {
                        GameObject obj = (GameObject)objField.GetValue(objectList[i]);
                        if (obj != null)
                        {
                            Destroy(obj);
                            obj = null;
                        }
                        objectList.RemoveAt(i);
                    }

#if DEBUG
                    Log.LogInfo($"Trimmed Parts List Before: {beforeTrimSize} After: {partsList.Count} Obj List: {objectList.Count}");
#endif               
                }
            }

            if (KKAPI.Maker.MakerAPI.InsideAndLoaded)
                updateMakerUIMethod.Invoke(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance, new object[] { });
        }

        private void SetMoreAccessorialPartToEmpty(int slot)
        {

#if DEBUG
            Log.LogInfo($"Setting more acc slot {slot} parts info to nothing");
#endif

            IDictionary charAdditionalData = (IDictionary)additionalDataField.GetValue(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance);
            foreach (DictionaryEntry entry in charAdditionalData)
            {
                if (entry.Key.Equals(ChaControl.chaFile))
                {
                    List<ChaFileAccessory.PartsInfo> partsList = (List<ChaFileAccessory.PartsInfo>)partsField.GetValue(entry.Value);
                    if (originalMACCPartsList == null)
                    {
#if DEBUG
                        Log.LogInfo($"Original Parts List: {partsList.Count}");
#endif
                        originalMACCPartsList = CopyPartsList(partsList);
                    }
                    partsList[slot].type = 350;
                    break;
                }
            }

        }

        private CmpAccessory GetMoreAccessorialCmpAccessory(int slot)
        {
            IDictionary charAdditionalData = (IDictionary)additionalDataField.GetValue(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance);
            foreach (DictionaryEntry entry in charAdditionalData)
            {
                if (entry.Key.Equals(ChaControl.chaFile))
                {
                    IList objectList = (IList)objectsField.GetValue(entry.Value);
                    if (slot >= objectList.Count)
                    {
                        return null;
                    }
                    else
                    {
                        return (CmpAccessory)cmpAccessoryField.GetValue(objectList[slot]);
                    }
                }
            }
            return null;
        }

        private ChaFileAccessory.PartsInfo GetMoreAccessorialPartInfo(int slot)
        {
            IDictionary charAdditionalData = (IDictionary)additionalDataField.GetValue(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance);
            foreach (DictionaryEntry entry in charAdditionalData)
            {
                if (entry.Key.Equals(ChaControl.chaFile))
                {
                    List<ChaFileAccessory.PartsInfo> partsList = (List<ChaFileAccessory.PartsInfo>)partsField.GetValue(entry.Value);
                    if (slot >= partsList.Count)
                    {
                        return null;
                    }
                    else
                    {
                        return partsList[slot];
                    }
                }
            }
            return null;
        }

        private ListInfoBase GetMoreAccessorialAccInfo(int slot)
        {
            IDictionary charAdditionalData = (IDictionary)additionalDataField.GetValue(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance);
            foreach (DictionaryEntry entry in charAdditionalData)
            {
                if (entry.Key.Equals(ChaControl.chaFile))
                {
                    IList objectList = (IList)objectsField.GetValue(entry.Value);
                    if (slot >= objectList.Count)
                    {
                        return null;
                    }
                    else
                    {
                        return (ListInfoBase)listInfoBaseField.GetValue(objectList[slot]);
                    }
                }
            }
            return null;
        }

        private int AddMoreAccessoriesPart(ChaFileAccessory.PartsInfo partsInfo, int slotNumber)
        {
#if DEBUG
            Log.LogInfo($"Setting slot {slotNumber} to {partsInfo.id}|{partsInfo.type}");
#endif

            IDictionary charAdditionalData = (IDictionary)additionalDataField.GetValue(AdditionalAccessoryControlsPlugin.MoreAccessoriesInstance);
            foreach (DictionaryEntry entry in charAdditionalData)
            {
                if (entry.Key.Equals(ChaControl.chaFile))
                {
                    List<ChaFileAccessory.PartsInfo> partsList = (List<ChaFileAccessory.PartsInfo>)partsField.GetValue(entry.Value);
                    IList objectList = (IList)objectsField.GetValue(entry.Value);
#if DEBUG
                    Log.LogInfo($"Before Parts List Size: {partsList.Count} Objects List Size: {objectList.Count}");
#endif
                    if (slotNumber >= partsList.Count || slotNumber >= objectList.Count)
                    {
                        while (slotNumber >= partsList.Count)
                        {
                            partsList.Add(partsInfo);

                        }
                        while (slotNumber >= objectList.Count)
                        {
                            objectList.Add(accessoryObjectType.GetConstructor(Type.EmptyTypes).Invoke(null));
                        }
#if DEBUG
                    Log.LogInfo($"Augmented Parts List Size: {partsList.Count} Objects List Size: {objectList.Count}");
#endif
                    }

                    if (slotNumber == -1)
                    {
                        partsList.Add(partsInfo);
                        objectList.Add(accessoryObjectType.GetConstructor(Type.EmptyTypes).Invoke(null));
                    }
                    else
                    {
                        partsList[slotNumber] = partsInfo;
                        objectList[slotNumber] = accessoryObjectType.GetConstructor(Type.EmptyTypes).Invoke(null);
                    }
#if DEBUG
                    Log.LogInfo($"After Parts List Size: {partsList.Count} Objects List Size: {objectList.Count}");
#endif
                    return partsList.Count - 1;
                }
            }
            return -1;
        }

        // Helpers
        private List<ChaFileAccessory.PartsInfo> CopyPartsList(List<ChaFileAccessory.PartsInfo> sourcePartsList)
        {
            List<ChaFileAccessory.PartsInfo> destPartsList = new List<ChaFileAccessory.PartsInfo>();
            foreach (ChaFileAccessory.PartsInfo sourcePart in sourcePartsList)
            {
                destPartsList.Add(MakePartFrom(sourcePart));
            }

            return destPartsList;
        }

        private ChaFileAccessory.PartsInfo[] CopyPartsArray(ChaFileAccessory.PartsInfo[] sourcePartsArray)
        {
            ChaFileAccessory.PartsInfo[] destPartsArray = new ChaFileAccessory.PartsInfo[20];
            int i = 0;
            foreach (ChaFileAccessory.PartsInfo sourcePart in sourcePartsArray)
            {
                destPartsArray[i] = MakePartFrom(sourcePart);
                i++;
            }

            return destPartsArray;
        }

        private ChaFileAccessory.PartsInfo MakePartFrom(ChaFileAccessory.PartsInfo source)
        {
            ChaFileAccessory.PartsInfo destination = new ChaFileAccessory.PartsInfo();
            destination.type = source.type;
            destination.id = source.id;
            destination.parentKey = source.parentKey;
            destination.addMove = new Vector3[2, 3];
            for (int i = 0; i < 2; i++)
            {
                destination.addMove[i, 0] = source.addMove[i, 0];
                destination.addMove[i, 1] = source.addMove[i, 1];
                destination.addMove[i, 2] = source.addMove[i, 2];
            }
            destination.colorInfo = new ChaFileAccessory.PartsInfo.ColorInfo[4];
            for (int j = 0; j < destination.colorInfo.Length; j++)
            {
                ChaFileAccessory.PartsInfo.ColorInfo sourceColor = source.colorInfo[j];
                destination.colorInfo[j] = new ChaFileAccessory.PartsInfo.ColorInfo
                {
                    color = sourceColor.color,
                    glossPower = sourceColor.glossPower,
                    metallicPower = sourceColor.metallicPower,
                    smoothnessPower = sourceColor.smoothnessPower
                };
            }
            destination.hideCategory = source.hideCategory;
            destination.hideTiming = source.hideTiming;
            destination.partsOfHead = source.partsOfHead;
            destination.noShake = source.noShake;
            return destination;
        }

        private bool runningVisibilityRules = false;
        public void HandleVisibilityRules(bool startup = false, bool hstart = false, bool hend = false, bool clothes = false, bool accessory = false)
        {            
            try
            {
                if (slotData == null || hidingAccessoriesForPicture || runningVisibilityRules)
                    return;

                runningVisibilityRules = true;

                foreach (AdditionalAccessorySlotData slot in slotData)
                {
                    HandleVisibilityRulesForSlot(slot, startup, hstart, hend, clothes, accessory);
                }

                HandleAccessorialSlotLinks();
                HandleHairVisibilityRules();
                HandleBodyVisibilityRules();

                UpdateMoreAccessorialUI();
                if (KKAPI.Studio.StudioAPI.InsideStudio)
                {
                    UpdateStudioUI();
                }
            } catch (Exception e)
            {
                Log.LogWarning($"Unable to process visibility rules, accessories may not be in desired state {e.Message} {e.StackTrace}");
            }
            runningVisibilityRules = false;
        }

        public void HandleVisibilityRulesForSlot(AdditionalAccessorySlotData slot, bool startup = false, bool hstart = false, bool hend = false, bool clothes = false, bool accessory = false, bool ruleUpdate = false)
        {
            if (slot.VisibilityRules == null || hidingAccessoriesForPicture || (startup && AdditionalAccessoryControlsPlugin.Instance.StudioSceneLoading) || (startup && KKAPI.Studio.StudioAPI.InsideStudio && !HasStudioApplyVisibilityRule(slot)))
            {
                return;
            }    
            
            if (ruleUpdate)
            {
#if DEBUG
                Log.LogInfo($"Rules Update for Slot: {slot}");
#endif
            }
           
            if (startup && HasStartupVisibilityRule(slot) && !accessory)
            {
#if DEBUG
                Log.LogInfo($"Hiding Accessorial: {slot.SlotNumber} {slot.AccessoryName} due to startup rules");
#endif
                try { ChaControl.SetAccessoryState(slot.SlotNumber, false); } catch (Exception e) { Log.LogWarning($"Error in Set Accessory State for slot {slot.SlotNumber} Message: {e.Message}\n\n{e.StackTrace}");  }
            } 
            else if (hstart && slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_START, AdditionalAccessoryVisibilityRulesModifiers.SHOW))
            {
#if DEBUG
                Log.LogInfo($"Showing Accessorial: {slot.SlotNumber} {slot.AccessoryName} due to hstart rules");
#endif
                try { ChaControl.SetAccessoryState(slot.SlotNumber, true); } catch (Exception e) { Log.LogWarning($"Error in Set Accessory State for slot {slot.SlotNumber} Message: {e.Message}\n\n{e.StackTrace}"); }
            }
            else if (hstart && slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_START, AdditionalAccessoryVisibilityRulesModifiers.HIDE))
            {
#if DEBUG
                Log.LogInfo($"Hiding Accessorial: {slot.SlotNumber} {slot.AccessoryName} due to hstart rules");
#endif
                try { ChaControl.SetAccessoryState(slot.SlotNumber, false); } catch (Exception e) { Log.LogWarning($"Error in Set Accessory State for slot {slot.SlotNumber} Message: {e.Message}\n\n{e.StackTrace}"); }
            }
            else if (hend && slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_END, AdditionalAccessoryVisibilityRulesModifiers.SHOW))
            {
#if DEBUG
                Log.LogInfo($"Showing Accessorial: {slot.SlotNumber} {slot.AccessoryName} due to hend rules");
#endif
                try { ChaControl.SetAccessoryState(slot.SlotNumber, true); } catch (Exception e) { Log.LogWarning($"Error in Set Accessory State for slot {slot.SlotNumber} Message: {e.Message}\n\n{e.StackTrace}"); }
            }
            else if (hend && slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_END, AdditionalAccessoryVisibilityRulesModifiers.HIDE))
            {
#if DEBUG
                Log.LogInfo($"Hiding Accessorial: {slot.SlotNumber} {slot.AccessoryName} due to hend rules");
#endif
                try { ChaControl.SetAccessoryState(slot.SlotNumber, false); } catch (Exception e) { Log.LogWarning($"Error in Set Accessory State for slot {slot.SlotNumber} Message: {e.Message}\n\n{e.StackTrace}"); }
            }


            if (!accessory && (startup || clothes || ruleUpdate))
            {
                if (HasClothesVisibilityRule(slot) || (ruleUpdate && !HasStartupVisibilityRule(slot)))
                {
                    if (VisibilityClothesCheckResult(slot))
                    {
                        if (IsAccessoryShowing(slot.SlotNumber))
                        {
#if DEBUG
                            Log.LogInfo($"Hiding Accessorial: {slot.SlotNumber} {slot.AccessoryName} due to clothes rules");
#endif
                            try { ChaControl.SetAccessoryState(slot.SlotNumber, false); } catch (Exception e) { Log.LogWarning($"Error in Set Accessory State for slot {slot.SlotNumber} Message: {e.Message}\n\n{e.StackTrace}"); }
                        }
                    }
                    else 
                    {
                        if (!IsAccessoryShowing(slot.SlotNumber))
                        {
#if DEBUG
                            Log.LogInfo($"Showing Accessorial: {slot.SlotNumber} {slot.AccessoryName} due to clothes rules");
#endif
                            try { ChaControl.SetAccessoryState(slot.SlotNumber, true); } catch (Exception e) { Log.LogWarning($"Error in Set Accessory State for slot {slot.SlotNumber} Message: {e.Message}\n\n{e.StackTrace}"); }
                        }
                    }
                }
            }

            if (ruleUpdate)
                HandleAccessorialSlotLinks();

            if (ruleUpdate)
                HandleHairVisibilityRules();

            if (ruleUpdate)
                HandleBodyVisibilityRules();
        }

        private bool updatingSlotLinks = false;
        public void HandleAccessorialSlotLinks()
        {
            if (updatingSlotLinks)
                return;

            try
            {
                updatingSlotLinks = true;

                int recursion = 25;
                while (recursion-- > 0 && DoHandleAccessorialSlotLinks())
                {
#if DEBUG
                    Log.LogInfo($"Dirty Changes detected, looping: {recursion}");
#endif
                }
                if (recursion == 0)
                {
                    Log.LogMessage("Accessorial Links Too Deep or Looped. Check for loops (Slot 1 -> Slot 2 -> Slot 1) or flatten the link structure.");
                }
            }
            catch (Exception)
            {
                Log.LogWarning("Invalid Accessory Link, Something is linked to a slot that does not exist or was moved.");
            }

            updatingSlotLinks = false;
        }

        private bool DoHandleAccessorialSlotLinks()
        {
            bool dirty = false;            

            IEnumerable<AdditionalAccessorySlotData> linkedSlots = slotData.Where<AdditionalAccessorySlotData>(slot => slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK) || slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK));
            foreach (AdditionalAccessorySlotData slot in linkedSlots)
            {
                AdditionalAccessoryVisibilityRuleData rule = slot.FindVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK);
                if (rule != null)
                {
                    int linkSlotNumber = int.Parse(rule.Modifier) - 1;
#if DEBUG
                    Log.LogInfo($"Handling Link for Slot: {IsAccessoryShowing(slot.SlotNumber)} linked to Slot: {IsAccessoryShowing(linkSlotNumber)}");
#endif
                    if (!CheckAccessorialStateMatch(slot, slotData[linkSlotNumber]))
                    {
                        ChaControl.SetAccessoryState(slot.SlotNumber, IsAccessoryShowing(linkSlotNumber));
                        dirty = true;
#if DEBUG
                        Log.LogInfo($"New Status for Link for Slot: {IsAccessoryShowing(slot.SlotNumber)} linked to Slot: {IsAccessoryShowing(linkSlotNumber)}");
#endif
                    }
                }
                else 
                {
                    rule = slot.FindVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK);
                    if (rule == null)
                        continue;

                    int linkSlotNumber = int.Parse(rule.Modifier) - 1;
#if DEBUG
                    Log.LogInfo($"Handling Inverse Link for Slot: {IsAccessoryShowing(slot.SlotNumber)} linked to Slot: {IsAccessoryShowing(linkSlotNumber)}");
#endif                 
                    if (CheckAccessorialStateMatch(slot, slotData[linkSlotNumber]))
                    {
                        ChaControl.SetAccessoryState(slot.SlotNumber, !IsAccessoryShowing(linkSlotNumber));
                        dirty = true;
#if DEBUG
                        Log.LogInfo($"New Status for Inverse Link for Slot: {IsAccessoryShowing(slot.SlotNumber)} linked to Slot: {IsAccessoryShowing(linkSlotNumber)}");
#endif
                    }
                }
            }
            return dirty;
        }

        private bool CheckAccessorialStateMatch(AdditionalAccessorySlotData firstSlot, AdditionalAccessorySlotData secondSlot)
        {
            return IsAccessoryShowing(firstSlot.SlotNumber) == IsAccessoryShowing(secondSlot.SlotNumber);
        }

        public void HandleBodyVisibilityRules()
        {
            lock (boneEffect)
            {
                List<string> bonesToHide = CheckBodyVisibilityRules();

                boneEffect.HiddenBones.Clear();
                boneEffect.HiddenBones.AddRange(bonesToHide);
                
            }
        }

        public List<string> CheckBodyVisibilityRules()
        {
            List<string> bonesToHide = new List<string>();

            if (CheckBodyVisibilityRule(AdditionalAccessoryVisibilityRules.NOSE, AdditionalAccessoryVisibilityRulesModifiers.NONE))
                bonesToHide.Add(AdditionalAccessoryBoneEffect.NOSE_BONE);

            if (CheckBodyVisibilityRule(AdditionalAccessoryVisibilityRules.EAR, AdditionalAccessoryVisibilityRulesModifiers.LEFT))
            {
                bonesToHide.Add(AdditionalAccessoryBoneEffect.LEFT_EAR);
            }
            else if (boneEffect.HiddenBones.Contains(AdditionalAccessoryBoneEffect.LEFT_EAR))
            {
                boneEffect.ResetLeftEar = true;
            }                
            
            if (CheckBodyVisibilityRule(AdditionalAccessoryVisibilityRules.EAR, AdditionalAccessoryVisibilityRulesModifiers.RIGHT))
            {
                bonesToHide.Add(AdditionalAccessoryBoneEffect.RIGHT_EAR);
            }
            else if (boneEffect.HiddenBones.Contains(AdditionalAccessoryBoneEffect.RIGHT_EAR))
            {
                boneEffect.ResetRightEar = true;
            }

            if (CheckBodyVisibilityRule(AdditionalAccessoryVisibilityRules.HAND, AdditionalAccessoryVisibilityRulesModifiers.LEFT))
                bonesToHide.Add(AdditionalAccessoryBoneEffect.LEFT_HAND);

            if (CheckBodyVisibilityRule(AdditionalAccessoryVisibilityRules.HAND, AdditionalAccessoryVisibilityRulesModifiers.RIGHT))
                bonesToHide.Add(AdditionalAccessoryBoneEffect.RIGHT_HAND);

            if (CheckBodyVisibilityRule(AdditionalAccessoryVisibilityRules.FOOT, AdditionalAccessoryVisibilityRulesModifiers.LEFT))
                bonesToHide.Add(AdditionalAccessoryBoneEffect.LEFT_FOOT);

            if (CheckBodyVisibilityRule(AdditionalAccessoryVisibilityRules.FOOT, AdditionalAccessoryVisibilityRulesModifiers.RIGHT))
                bonesToHide.Add(AdditionalAccessoryBoneEffect.RIGHT_FOOT);

            return bonesToHide;
        }

        public bool CheckBodyVisibilityRule(AdditionalAccessoryVisibilityRules rule, AdditionalAccessoryVisibilityRulesModifiers mod)
        {
            if (slotData == null)
            {
                return false;
            }

            foreach (AdditionalAccessorySlotData slot in slotData)
            {
                if (slot.ContainsVisibilityRule(rule, mod) && IsAccessoryShowing(slot.SlotNumber))
                {
                    return true;
                }
            }
            return false;
        }

        public void HandleHairVisibilityRules()
        {
            bool showFront = true;
            bool showBack = true;
            bool showSide= true;
            bool showExt = true;

            foreach (AdditionalAccessorySlotData slot in slotData)
            {
                if (!IsAccessoryShowing(slot.SlotNumber))
                    continue;

                if (slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_FRONT))
                {
                    showFront = false;
                }
                if (slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_BACK))
                {
                    showBack = false;
                }
                if (slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_SIDE))
                {
                    showSide = false;
                }
                if (slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_EXT))
                {
                    showExt = false;
                }
            }

            if (showFront)
            {
                ShowHair(ChaControl.objHair[(int)ChaFileDefine.HairKind.front]);
            }
            else
            {
                HideHair(ChaControl.objHair[(int)ChaFileDefine.HairKind.front]);
            }

            if (showBack)
            {
                ShowHair(ChaControl.objHair[(int)ChaFileDefine.HairKind.back]);
            }
            else
            {
                HideHair(ChaControl.objHair[(int)ChaFileDefine.HairKind.back]);
            }

            if (showSide)
            {
                ShowHair(ChaControl.objHair[(int)ChaFileDefine.HairKind.side]);
            }
            else
            {
                HideHair(ChaControl.objHair[(int)ChaFileDefine.HairKind.side]);
            }

            if (showExt)
            {
                ShowHair(ChaControl.objHair[(int)ChaFileDefine.HairKind.option]);
            }
            else
            {
                HideHair(ChaControl.objHair[(int)ChaFileDefine.HairKind.option]);
            }
        }

        private void HideHair(GameObject hair)
        {
            if (hair != null)
            {
                foreach (Transform child in hair.transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void ShowHair(GameObject hair)
        {
            if (hair != null)
            {
                foreach (Transform child in hair.transform)
                {
                    child.gameObject.SetActive(true);
                }
            }
        }

        private bool IsAccessoryShowing(int slotNumber)
        {
            if (slotData[slotNumber].IsEmpty)
                return false;

            if (slotNumber < 20)
            {
                return ChaControl.fileStatus.showAccessory[slotNumber];
            }
            else
            {
                return GetMoreAccessorySlotStatus(slotNumber - 20);
            }
        }

        public bool HasStudioApplyVisibilityRule(AdditionalAccessorySlotData slot)
        {
            foreach (AdditionalAccessoryVisibilityRuleData rule in slot.VisibilityRules)
            {
                if (rule.Rule.Equals(AdditionalAccessoryVisibilityRules.STUDIO_LOAD))
                    return true;
            }
            return false;
        }

        public bool HasStartupVisibilityRule(AdditionalAccessorySlotData slot)
        {
            foreach (AdditionalAccessoryVisibilityRuleData rule in slot.VisibilityRules)
            {
                if (rule.Rule.Equals(AdditionalAccessoryVisibilityRules.STARTUP))
                    return true;
            }
            return false;
        }

        public bool HasClothesVisibilityRule(AdditionalAccessorySlotData slot)
        {
            foreach (AdditionalAccessoryVisibilityRuleData rule in slot.VisibilityRules)
            {
                if ( ((int)rule.Rule) >= 10 && ((int)rule.Rule) < 20)
                {
                    return true;
                }
            }
            return false;
        }

        public List<AdditionalAccessoryVisibilityRuleData> FindAllClothesRules(AdditionalAccessorySlotData slot)
        {
            return slot.VisibilityRules == null ? new List<AdditionalAccessoryVisibilityRuleData>() : slot.VisibilityRules.Where(rule => ((int)rule.Rule) >= 10 && ((int)rule.Rule) < 20).ToList();
        }

        // Visibility Rule Handlers
        public bool VisibilityClothesCheckResult(AdditionalAccessorySlotData slot)
        {
            bool result = false;
            foreach (AdditionalAccessoryVisibilityRuleData rule in FindAllClothesRules(slot))
            {
                result = result || CheckClothingSlotRule(rule);
            }
            return result;           
        }

        // Check Clothing Slot Rule Result - Including Suppression and Override Rules
        public bool CheckClothingSlotRule(AdditionalAccessoryVisibilityRuleData ruleData)
        {
            bool result = false;

            // Handle Base Result - Unless we are overridden to some other slot
            if (!CoordinateOverrideData.IsOverrideSource(ruleData.Rule, ruleData.Modifier))
                result = result || (!CoordinateOverrideData.IsSuppressed(ruleData.Rule, ruleData.Modifier) && CheckClothesState(ruleData.Modifier, ClothesSlotMapping[ruleData.Rule], ruleData.Rule));

            List<AdditionalAccessoryCoordinateRuleData> overrideTargetRules = CoordinateOverrideData.GetOverrideTargets(ruleData.Rule, ruleData.Modifier);
            overrideTargetRules.AddRange(CoordinateOverrideData.GetOverrideTargets(ruleData.Rule, AdditionalAccessoryVisibilityRulesModifiers.ALL));

            // Handle Overrides
            foreach (AdditionalAccessoryCoordinateRuleData overrideRule in overrideTargetRules) {
                AdditionalAccessoryVisibilityRules checkRule = overrideRule == null ? ruleData.Rule : overrideRule.Rule;
                string checkModifier = overrideRule == null ? ruleData.Modifier : overrideRule.RuleModifier;

                // If this is an ALL->ALL, we want that to act like ON->ON, HALF->HALF, OFF->OFF rather than *->*
                if (String.Equals(overrideRule.RuleModifier, ((int)AdditionalAccessoryVisibilityRulesModifiers.ALL).ToString()) && String.Equals(overrideRule.OverrideRuleModifier, ((int)AdditionalAccessoryVisibilityRulesModifiers.ALL).ToString()))
                {
                    checkModifier = ruleData.Modifier;
                }
#if DEBUG
                if (overrideRule != null)
                {
                    Log.LogInfo($"Evaluating Using Override, Base: {ruleData} CheckRule: {checkRule} CheckMod: {checkModifier} Override: {overrideRule}");
                }
#endif
                result = result || (!CoordinateOverrideData.IsSuppressed(checkRule, checkModifier) && CheckClothesState(checkModifier, ClothesSlotMapping[checkRule], ruleData.Rule));
            }
            return result;
        }

        private bool CheckClothesState(string mod, ChaFileDefine.ClothesKind kind, AdditionalAccessoryVisibilityRules baseRule)
        {            
            switch (Enum.Parse(typeof(AdditionalAccessoryVisibilityRulesModifiers), mod))
            {
                case AdditionalAccessoryVisibilityRulesModifiers.ON:
                    if (ChaControl.IsClothesStateKind((int)kind) && ChaControl.fileStatus.clothesState[(int)kind] == 0)
                        return true;
                    break;
                case AdditionalAccessoryVisibilityRulesModifiers.HALF:
                    if ( (baseRule == AdditionalAccessoryVisibilityRules.GLOVE || baseRule == AdditionalAccessoryVisibilityRules.SOCK || baseRule == AdditionalAccessoryVisibilityRules.SHOE) && ChaControl.IsClothesStateKind((int)kind) && ChaControl.fileStatus.clothesState[(int)kind] == 2)
                        return true;
                    else if (ChaControl.IsClothesStateKind((int)kind) && ChaControl.fileStatus.clothesState[(int)kind] == 1)
                        return true;
                    break;
                case AdditionalAccessoryVisibilityRulesModifiers.OFF:
                    if (ChaControl.IsClothesStateKind((int)kind) && ChaControl.fileStatus.clothesState[(int)kind] == 2)
                        return true;
                    break;
                case AdditionalAccessoryVisibilityRulesModifiers.ALL:
                    if (
                        (ChaControl.IsClothesStateKind((int)kind) && ChaControl.fileStatus.clothesState[(int)kind] == 2)
                        || (ChaControl.IsClothesStateKind((int)kind) && ChaControl.fileStatus.clothesState[(int)kind] == 1)
                        || (ChaControl.IsClothesStateKind((int)kind) && ChaControl.fileStatus.clothesState[(int)kind] == 0)
                        )
                        return true;
                    break;

            }
            return false;
        }
        
        public void AddOrUpdateSlot(int slotIndex, AdditionalAccessorySlotData slot)
        {
            if (slotIndex >= slotData.Length)
            {
                int previousMaxSlot = slotData.Length - 1;
                Array.Resize(ref slotData, slotIndex + 1);
                for (int i = slotData.Length - 1; i > previousMaxSlot; i--)
                {
                    slotData[i] = AdditionalAccessorySlotData.Copy(slot, i);
                }
            } 
            else
            {
                slotData[slotIndex] = slot;
            }            
        }

        private int FindNextEmptySlot(List<AdditionalAccessorySlotData> slotData)
        {
            foreach (AdditionalAccessorySlotData slot in slotData)
            {
                if (slot.IsEmpty)
                    return slot.SlotNumber;
            }

            return -1;
        }

        // Used in coordinate loads to update accessory links from character accessories to other character accessories follow the new slot location
        // If a character accessory is linked to a non-character accessory, this link is cleared (as it would be gone after coordinate load).
        private void UpdateMovedAccessoryLinks(List<AdditionalAccessorySlotData> slotData)
        {
            foreach (AdditionalAccessorySlotData slot in slotData)
            {
                if (slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK))
                {
                    AdditionalAccessoryVisibilityRuleData data = slot.FindVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK);
                    int oldLinkSlotNumber = int.Parse(data.Modifier) - 1;
                    bool found = false;
                    foreach (AdditionalAccessorySlotData originalSlot in slotData)
                    {
                        if (originalSlot.OriginalSlotNumber == oldLinkSlotNumber)
                        {
#if DEBUG
                            Log.LogInfo($"Moving Inverse Link on new Slot {slot.SlotNumber} Rule From {oldLinkSlotNumber} To {originalSlot.SlotNumber}");
#endif
                            slot.ClearVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK);
                            slot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK, (originalSlot.SlotNumber + 1).ToString(), true);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
#if DEBUG
                        Log.LogInfo($"Clearing Dead Inverse Link on new Slot {slot.SlotNumber}");
#endif

                        slot.ClearVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK);
                    }
                    
                }
                else if (slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK))
                {
                    AdditionalAccessoryVisibilityRuleData data = slot.FindVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK);
                    int oldLinkSlotNumber = int.Parse(data.Modifier) - 1;
                    bool found = false;
                    foreach (AdditionalAccessorySlotData originalSlot in slotData)
                    {
                        if (originalSlot.OriginalSlotNumber == oldLinkSlotNumber)
                        {
#if DEBUG
                            Log.LogInfo($"Moving Link on new Slot {slot.SlotNumber} Rule From {oldLinkSlotNumber} To {originalSlot.SlotNumber}");
#endif
                            slot.ClearVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK);
                            slot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK, (originalSlot.SlotNumber + 1).ToString(), true);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
#if DEBUG
                        Log.LogInfo($"Clearing Dead Link on new Slot {slot.SlotNumber}");
#endif
                        slot.ClearVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK);
                    }

                }
            }
        }

        // Used in coordinate saves to remove link references to accessorials that no longer would exist in the coordinate (non-character accesory -> character accessory)
        private void PruneDeadAccessoryLinks(AdditionalAccessorySlotData[] slotData)
        {
            foreach (AdditionalAccessorySlotData slot in slotData)
            {
                if (slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK))
                {
                    AdditionalAccessoryVisibilityRuleData data = slot.FindVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK);
                    int linkSlotNumber = int.Parse(data.Modifier) - 1;
                    if (slotData[linkSlotNumber].IsEmpty)
                    {
#if DEBUG
                        Log.LogInfo($"Pruning dead inverse link from {slot.SlotNumber} to {linkSlotNumber}");
#endif
                        slot.ClearVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK);
                    }
                }
                else if (slot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK))
                {
                    AdditionalAccessoryVisibilityRuleData data = slot.FindVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK);
                    int linkSlotNumber = int.Parse(data.Modifier) - 1;
                    if (slotData[linkSlotNumber].IsEmpty)
                    {
#if DEBUG
                        Log.LogInfo($"Pruning dead link from {slot.SlotNumber} to {linkSlotNumber}");
#endif
                        slot.ClearVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK);
                    }
                }
            }
        }

        // Debug Helpers
        private void dumpSlotData()
        {
            dumpSlotData(slotData);
        }

        private void dumpSlotData(AdditionalAccessorySlotData[] slotData)
        {
            Log.LogInfo("SLOT DATA...");
            if (slotData == null || slotData.Length == 0)
            {
                Log.LogInfo("No Slot Data");
                return;
            }

            int i = 0;
            foreach (AdditionalAccessorySlotData slot in slotData)
            {
                if (slot != null)
                {
                    Log.LogInfo(slot.ToString());
                }
                else
                {
                    Log.LogInfo($"Slot: {i} empty.");
                }
                i++;
            }
        }

        private void dumpCoordinateSlotData()
        {
            Log.LogInfo("COORDINATE SLOT DATA...");
            if (coordinateSlotData == null || coordinateSlotData.Length == 0)
            {
                Log.LogInfo("No Slot Data");
                return;
            }

            int i = 0;
            foreach (AdditionalAccessorySlotData slot in coordinateSlotData)
            {
                if (slot != null)
                {
                    Log.LogInfo(slot);
                }
                else
                {
                    Log.LogInfo($"Slot: {i} empty.");
                }
                i++;
            }
        }

        private void dumpCurrentAccessories()
        {
            Log.LogInfo("CURRENT ACCESSORIES...");

            int i = 0;
            foreach (GameObject accessory in AccessoriesApi.GetAccessoryObjects(ChaControl))
            {
                if (accessory == null)
                {
                    Log.LogInfo($"Accessory Slot: {i} empty.");
                }
                else
                {
                    CmpAccessory cmpAccessory = accessory.GetComponent<CmpAccessory>();
                    ListInfoComponent infoAccessory = accessory.GetComponent<ListInfoComponent>();
                    Log.LogInfo($"Accessory Slot: {i} Name: {infoAccessory.data.Name} ID: {infoAccessory.data.Id} Type: {infoAccessory.data.Category}");
                }
                i++;
            }
        }

        private static readonly Dictionary<AdditionalAccessoryVisibilityRules, ChaFileDefine.ClothesKind> ClothesSlotMapping = new Dictionary<AdditionalAccessoryVisibilityRules, ChaFileDefine.ClothesKind>
        {
            { AdditionalAccessoryVisibilityRules.TOP, ChaFileDefine.ClothesKind.top },
            { AdditionalAccessoryVisibilityRules.BOT, ChaFileDefine.ClothesKind.bot },
            { AdditionalAccessoryVisibilityRules.INNER_TOP, ChaFileDefine.ClothesKind.inner_t },
            { AdditionalAccessoryVisibilityRules.INNER_BOT, ChaFileDefine.ClothesKind.inner_b },
            { AdditionalAccessoryVisibilityRules.PANTYHOSE, ChaFileDefine.ClothesKind.panst },
            { AdditionalAccessoryVisibilityRules.GLOVE, ChaFileDefine.ClothesKind.gloves },
            { AdditionalAccessoryVisibilityRules.SHOE, ChaFileDefine.ClothesKind.shoes },
            { AdditionalAccessoryVisibilityRules.SOCK, ChaFileDefine.ClothesKind.socks }            
        };
    }
}
