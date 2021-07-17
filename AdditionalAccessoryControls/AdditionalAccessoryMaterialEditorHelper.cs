using AIChara;
using HarmonyLib;
using KKAPI.Chara;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            public List<object> materialCopyList { get; set; }

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
                response += "\nCopy List";
                response += EnumerateMatEditList(materialCopyList);
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
                    if (ExtractObjectType(o) != 2)
                        continue;
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
#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Moving Slots: {string.Join(",", slotsToMove)}");
#endif

            DoMoveSlots(slotsToMove, saveData.rendererPropertyList, snapshotData.rendererPropertyList);
            DoMoveSlots(slotsToMove, saveData.materialFloatPropertyList, snapshotData.materialFloatPropertyList);
            DoMoveSlots(slotsToMove, saveData.materialColorPropertyList, snapshotData.materialColorPropertyList);
            DoMoveSlots(slotsToMove, saveData.materialTexturePropertyList, snapshotData.materialTexturePropertyList);
            DoMoveSlots(slotsToMove, saveData.materialShaderPropertyList, snapshotData.materialShaderPropertyList);
            DoMoveSlots(slotsToMove, saveData.materialCopyList, snapshotData.materialCopyList);
        }

        private void ClearRemovedSlots(List<int> slotsToRemove, MaterialEditorSaveData saveData)
        {
#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Removing Slots: {string.Join(",", slotsToRemove)}");
#endif
            ClearRemovedNodes(slotsToRemove, saveData.rendererPropertyList);
            ClearRemovedNodes(slotsToRemove, saveData.materialFloatPropertyList);
            ClearRemovedNodes(slotsToRemove, saveData.materialColorPropertyList);
            ClearRemovedNodes(slotsToRemove, saveData.materialTexturePropertyList);
            ClearRemovedNodes(slotsToRemove, saveData.materialShaderPropertyList);
            ClearRemovedNodes(slotsToRemove, saveData.materialCopyList);
        }

        private CharaCustomFunctionController FindMaterialCharaController()
        {
            return (CharaCustomFunctionController)Control.gameObject.GetComponent("MaterialEditorCharaController");
        }

        private void DoMoveSlots(List<Tuple<int, int>> slotsToMove, IList current, IList snapshot)
        {
            if (slotsToMove == null || snapshot == null)
                return;

            List<int> movingSlotNumbers = slotsToMove.Select(t => t.Item1).Distinct<int>().ToList();
#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"Slots Moving {string.Join(",", movingSlotNumbers)}");
#endif

            // Identify slots to move
            List<object> movingSlots = new List<object>();
            foreach (object sourceNode in snapshot)
            {
                if (sourceNode != null && ExtractObjectType(sourceNode) == 2 && movingSlotNumbers.Contains(ExtractSlot(sourceNode)))
                {
                    movingSlots.Add(sourceNode);
                }
            }

            // Update slot numbers
            foreach (object node in movingSlots)
            {
                UpdateSlot(node, slotsToMove.Find(t => t.Item1 == ExtractSlot(node)).Item2);
                current.Add(node);
            }          
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

            if (AccessTools.GetFieldNames(materialController.GetType()).Contains("MaterialCopyList"))
            {
                saveData.materialCopyList = new List<object>();
                CopyList((IList)AccessTools.Field(materialController.GetType(), "MaterialCopyList").GetValue(materialController), saveData.materialCopyList);
            }

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

            if (AccessTools.GetFieldNames(materialController.GetType()).Contains("MaterialCopyList"))
            {
                IList materialCopyList = (IList)AccessTools.Field(materialController.GetType(), "MaterialCopyList").GetValue(materialController);
                materialCopyList.Clear();
                CopyList(saveData.materialCopyList, materialCopyList);
            }

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
