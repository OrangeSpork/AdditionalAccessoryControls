using AIChara;
using HarmonyLib;
using KKAPI.Chara;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryMaterialEditorHelper
    {

        private class MaterialEditorSaveData
        {
            public List<object> rendererPropertyList { get; set; }
            public List<object> materialFloatPropertyList { get; set; }
            public List<object> materialColorPropertyList { get; set; }
            public List<object> materialTexturePropertyList { get; set; }
            public List<object> materialShaderPropertyList { get; set; }

            public override string ToString()
            {
                string response = "\n";
                response += "Renderer Props";
                response += EnumerateMatEditList(rendererPropertyList);
                response += "\nFloat Props";
                response += EnumerateMatEditList(materialFloatPropertyList);
                response += "\nColor Props";
                response += EnumerateMatEditList(materialColorPropertyList);
                response += "\nTex Props";
                response += EnumerateMatEditList(materialTexturePropertyList);
                response += "\nShader Props";
                response += EnumerateMatEditList(materialShaderPropertyList);
                return response;
            }

            private string EnumerateMatEditList(IList list)
            {
                if (list == null)
                {
                    return "";
                }

                StringBuilder sb = new StringBuilder();
                foreach (object o in list)
                {
                    sb.Append($" ({ExtractObjectType(o)}-{ExtractSlot(o)})");
                }
                return sb.ToString();
            }

        }

        public ChaControl Control { get; set; }

        private MaterialEditorSaveData snapshotData;

        private static MethodInfo OnCoordinateLoad = AccessTools.Method(typeof(CharaCustomFunctionController), "OnCoordinateBeingLoaded", new Type[] { typeof(ChaFileCoordinate), typeof(bool) });
        private static MethodInfo OnCoordinateSave = AccessTools.Method(typeof(CharaCustomFunctionController), "OnCoordinateBeingSaved", new Type[] { typeof(ChaFileCoordinate) });

        public AdditionalAccessoryMaterialEditorHelper(ChaControl control)
        {
            Control = control;
        }

        public void UpdateOnCoordinateSave(ChaFileCoordinate coordinate, List<int> slotsToRemove)
        {
            CharaCustomFunctionController materialController = FindMaterialCharaController();
            if (materialController == null)
                return;

            snapshotData = null;

            // Store a snapshot for later restoration
            snapshotData = FillMaterialEditorLists();
            // And our copy for tampering with
            MaterialEditorSaveData saveData = FillMaterialEditorLists();

#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Update Coord Save: Storing Snapshot {snapshotData}");
#endif

            // Remove character accessories from save data
            ClearRemovedSlots(slotsToRemove, saveData);
            ApplyMaterialEditorLists(saveData);

#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Update Coord Save: After Slot Removal {saveData}");
#endif
            OnCoordinateSave.Invoke(materialController, new object[] { coordinate });
        }

        public void RestoreSnapshot(ChaFileCoordinate coordinate)
        {
            CharaCustomFunctionController materialController = FindMaterialCharaController();
            if (materialController == null)
                return;

            if (snapshotData != null)
            {
#if DEBUG
                AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Restore Snapshot {snapshotData}");
#endif
                ApplyMaterialEditorLists(snapshotData);

                OnCoordinateSave.Invoke(materialController, new object[] { coordinate });
                OnCoordinateLoad.Invoke(materialController, new object[] { coordinate, false });

                snapshotData = null;
            }

        }

        public void UpdateOnCoordinateLoadSnapshot()
        {
            CharaCustomFunctionController materialController = FindMaterialCharaController();
            if (materialController == null)
                return;

            // Squirrel away the current data so we can put character accessories back on later
            snapshotData = FillMaterialEditorLists();
#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Preload Snapshot Enum {snapshotData}");
#endif

        }

        public void UpdateOnCoordinateLoadApply(ChaFileCoordinate coordinate, List<Tuple<int, int>> movedSlots)
        {
            CharaCustomFunctionController materialController = FindMaterialCharaController();
            if (materialController == null || snapshotData == null)
                return;

#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Restore Snapshot {snapshotData}");
#endif

            // Update the material editor data to account for moving slots
            MaterialEditorSaveData currentSaveData = FillMaterialEditorLists();
            MoveSlots(movedSlots, currentSaveData, snapshotData);
            ApplyMaterialEditorLists(currentSaveData);

            // Need to do both of these, first in case material editor hasn't loaded yet, second if they have
            OnCoordinateSave.Invoke(materialController, new object[] { coordinate });
            OnCoordinateLoad.Invoke(materialController, new object[] { coordinate, false });

#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"After Application {FillMaterialEditorLists()}");
#endif

            snapshotData = null;
        }

        private void MoveSlots(List<Tuple<int, int>> slotsToMove, MaterialEditorSaveData saveData, MaterialEditorSaveData snapshotData)
        {
            DoMoveSlots(slotsToMove, saveData.rendererPropertyList, snapshotData.rendererPropertyList);
            DoMoveSlots(slotsToMove, saveData.materialFloatPropertyList, snapshotData.materialFloatPropertyList);
            DoMoveSlots(slotsToMove, saveData.materialColorPropertyList, snapshotData.materialColorPropertyList);
            DoMoveSlots(slotsToMove, saveData.materialTexturePropertyList, snapshotData.materialTexturePropertyList);
            DoMoveSlots(slotsToMove, saveData.materialShaderPropertyList, snapshotData.materialShaderPropertyList);
        }

        private void ClearRemovedSlots(List<int> slotsToRemove, MaterialEditorSaveData saveData)
        {
            ClearRemovedNodes(slotsToRemove, saveData.rendererPropertyList);
            ClearRemovedNodes(slotsToRemove, saveData.materialFloatPropertyList);
            ClearRemovedNodes(slotsToRemove, saveData.materialColorPropertyList);
            ClearRemovedNodes(slotsToRemove, saveData.materialTexturePropertyList);
            ClearRemovedNodes(slotsToRemove, saveData.materialShaderPropertyList);
        }

        private CharaCustomFunctionController FindMaterialCharaController()
        {
            return (CharaCustomFunctionController)Control.gameObject.GetComponent("MaterialEditorCharaController");
        }

        private void DoMoveSlots(List<Tuple<int, int>> slotsToMove, IList current, IList snapshot)
        {
            if (slotsToMove == null || snapshot == null)
                return;

            foreach (Tuple<int, int> slot in slotsToMove)
            {
                DoMoveSlot(slot.Item1, slot.Item2, current, snapshot);
            }
        }

        private void DoMoveSlot(int slotFrom, int slotTo, IList current, IList snapshot)
        {
            foreach (object sourceNode in snapshot)
            {
                if (sourceNode == null)
                    continue;

                if (ExtractObjectType(sourceNode) == 2 && ExtractSlot(sourceNode) == slotFrom)
                {
#if DEBUG
                    AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Updating slot {slotFrom} to slot {slotTo} On {sourceNode}");
#endif
                    if (!Update(slotFrom, slotTo, current))
                    {
                        UpdateSlot(sourceNode, slotTo);
                        current.Add(sourceNode);
#if DEBUG
                        AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"New: {current}");
#endif
                    }
                }
            }
        }

        private bool Update(int slotFrom, int slotTo, IList list)
        {
            if (list == null || list.Count == 0)
                return false;

            foreach (object child in list)
            {
                if (child == null)
                    continue;

                if (ExtractObjectType(child) == 2 && ExtractSlot(child) == slotFrom)
                {
                    UpdateSlot(child, slotTo);
#if DEBUG
                    AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Found existing record, updating {child}");
#endif
                    return true;
                }
            }
            return false;
        }

        private void ClearRemovedNodes(List<int> slotsToRemove, IList node)
        {
            if (node == null || node.Count == 0)
            {
                return;
            }

            for (int i = node.Count - 1; i >= 0; i--)
            {
                object child = node[i];
                if (ExtractObjectType(child) == 2)
                {
                    foreach (int slot in slotsToRemove)
                    {
                        if (slot == ExtractSlot(child))
                        {
                            node.Remove(child);
                            break;
                        }
                    }
                }
            }
        }

        private MaterialEditorSaveData FillMaterialEditorLists()
        {
            MaterialEditorSaveData saveData = new MaterialEditorSaveData();
            CharaCustomFunctionController materialController = FindMaterialCharaController();

            saveData.rendererPropertyList = new List<object>();
            CopyList((IList)AccessTools.Field(materialController.GetType(), "RendererPropertyList").GetValue(materialController), saveData.rendererPropertyList);

            saveData.materialFloatPropertyList = new List<object>();
            CopyList((IList)AccessTools.Field(materialController.GetType(), "MaterialFloatPropertyList").GetValue(materialController), saveData.materialFloatPropertyList);

            saveData.materialColorPropertyList = new List<object>();
            CopyList((IList)AccessTools.Field(materialController.GetType(), "MaterialColorPropertyList").GetValue(materialController), saveData.materialColorPropertyList);

            saveData.materialTexturePropertyList = new List<object>();
            CopyList((IList)AccessTools.Field(materialController.GetType(), "MaterialTexturePropertyList").GetValue(materialController), saveData.materialTexturePropertyList);

            saveData.materialShaderPropertyList = new List<object>();
            CopyList((IList)AccessTools.Field(materialController.GetType(), "MaterialShaderList").GetValue(materialController), saveData.materialShaderPropertyList);

            return saveData;
        }

        private void ApplyMaterialEditorLists(MaterialEditorSaveData saveData)
        {
            CharaCustomFunctionController materialController = FindMaterialCharaController();

            IList rendererList = (IList)AccessTools.Field(materialController.GetType(), "RendererPropertyList").GetValue(materialController);
            rendererList.Clear();
            CopyList(saveData.rendererPropertyList, rendererList);

            IList materialFloatList = (IList)AccessTools.Field(materialController.GetType(), "MaterialFloatPropertyList").GetValue(materialController);
            materialFloatList.Clear();
            CopyList(saveData.materialFloatPropertyList, materialFloatList);

            IList materialColorList = (IList)AccessTools.Field(materialController.GetType(), "MaterialColorPropertyList").GetValue(materialController);
            materialColorList.Clear();
            CopyList(saveData.materialColorPropertyList, materialColorList);

            IList materialTextureList = (IList)AccessTools.Field(materialController.GetType(), "MaterialTexturePropertyList").GetValue(materialController);
            materialTextureList.Clear();
            CopyList(saveData.materialTexturePropertyList, materialTextureList);

            IList materialShaderList = (IList)AccessTools.Field(materialController.GetType(), "MaterialShaderList").GetValue(materialController);
            materialShaderList.Clear();
            CopyList(saveData.materialShaderPropertyList, materialShaderList);

        }

        private void CopyList(IList source, IList destination)
        {
            foreach (object o in source)
            {
                destination.Add(o);
            }
        }

        protected static int ExtractObjectType(object data)
        {
            return (int)data.GetType().GetField("ObjectType", AccessTools.all).GetValue(data);
        }

        protected static int ExtractSlot(object data)
        {
            return (int)data.GetType().GetField("Slot", AccessTools.all).GetValue(data);
        }

        protected static void UpdateSlot(object data, int newSlot)
        {
            data.GetType().GetField("Slot", AccessTools.all).SetValue(data, newSlot);
        }
    }
}
