using AIChara;
using HarmonyLib;
using KKAPI.Chara;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryDynamicBonesHelper
    {

        public ChaControl Control { get; set; }

        private IList snapshotData;

        private static MethodInfo OnCoordinateLoad = AccessTools.Method(typeof(CharaCustomFunctionController), "OnCoordinateBeingLoaded", new Type[] { typeof(ChaFileCoordinate), typeof(bool) });
        private static MethodInfo OnCoordinateSave = AccessTools.Method(typeof(CharaCustomFunctionController), "OnCoordinateBeingSaved", new Type[] { typeof(ChaFileCoordinate) });

        public AdditionalAccessoryDynamicBonesHelper(ChaControl control)
        {
            Control = control;
        }

        public void UpdateOnCoordinateSave(ChaFileCoordinate coordinate, List<int> slotsToRemove)
        {
            CharaCustomFunctionController dbController = FindDynamicBoneController();
            if (dbController == null)
                return;

            snapshotData = null;

            // Store a snapshot for later restoration
            snapshotData = FillDBEditorLists();
            // And our copy for tampering with
            IList saveData = FillDBEditorLists();

#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"DB Update Coord Save: Storing Snapshot {snapshotData}");
#endif

            // Remove character accessories from save data
            ClearRemovedSlots(slotsToRemove, saveData);
            ApplyDynamicBoneList(saveData);

#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"DB Update Coord Save: After Slot Removal {saveData}");
#endif
            OnCoordinateSave.Invoke(dbController, new object[] { coordinate });
        }

        public void RestoreSnapshot(ChaFileCoordinate coordinate)
        {
            CharaCustomFunctionController dbController = FindDynamicBoneController();
            if (dbController == null)
                return;

            if (snapshotData != null)
            {
#if DEBUG
                AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"DB Restore Snapshot {snapshotData}");
#endif
                ApplyDynamicBoneList(snapshotData);

                OnCoordinateSave.Invoke(dbController, new object[] { coordinate });
                OnCoordinateLoad.Invoke(dbController, new object[] { coordinate, false });

                snapshotData = null;
            }

        }

        public void UpdateOnCoordinateLoadSnapshot()
        {
            CharaCustomFunctionController dbController = FindDynamicBoneController();
            if (dbController == null)
                return;

            // Squirrel away the current data so we can put character accessories back on later
            snapshotData = FillDBEditorLists();
#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"DB Preload Snapshot Enum {snapshotData}");
#endif

        }

        public void UpdateOnCoordinateLoadApply(ChaFileCoordinate coordinate, List<Tuple<int, int>> movedSlots)
        {
            CharaCustomFunctionController dbController = FindDynamicBoneController();
            if (dbController == null || snapshotData == null)
                return;

#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"DB Restore Snapshot {snapshotData}");
#endif

            // Update the material editor data to account for moving slots
            IList currentSaveData = FillDBEditorLists();
            MoveSlots(movedSlots, currentSaveData, snapshotData);
            ApplyDynamicBoneList(currentSaveData);

            // Need to do both of these, first in case material editor hasn't loaded yet, second if they have
            OnCoordinateSave.Invoke(dbController, new object[] { coordinate });
            OnCoordinateLoad.Invoke(dbController, new object[] { coordinate, false });

#if DEBUG
            AdditionalAccessoryControlsPlugin.Instance.Log.LogInfo($"DB After Application {FillDBEditorLists()}");
#endif

            snapshotData = null;
        }

        private void MoveSlots(List<Tuple<int, int>> slotsToMove, IList saveData, IList snapshotData)
        {
            DoMoveSlots(slotsToMove, saveData, snapshotData);
        }

        private void ClearRemovedSlots(List<int> slotsToRemove, IList saveData)
        {
            ClearRemovedNodes(slotsToRemove, saveData);
        }

        private CharaCustomFunctionController FindDynamicBoneController()
        {
            CharaCustomFunctionController[] controllers = Control.gameObject.GetComponents<CharaCustomFunctionController>();
            foreach (CharaCustomFunctionController controller in controllers)
            {
                if (controller.GetType().FullName.Equals("KK_Plugins.DynamicBoneEditor.CharaController"))
                    return controller;
            }
            return null;
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
                if (sourceNode != null && movingSlotNumbers.Contains(ExtractSlot(sourceNode)))
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

        private IList FillDBEditorLists()
        {
            IList saveData = new List<object>();
            CharaCustomFunctionController dbController = FindDynamicBoneController();

            CopyList((IList)AccessTools.Field(dbController.GetType(), "AccessoryDynamicBoneData").GetValue(dbController), saveData);
            return saveData;
        }

        private void ApplyDynamicBoneList(IList dynamicBoneList)
        {
            CharaCustomFunctionController dbController = FindDynamicBoneController();

            IList currentList = (IList)AccessTools.Field(dbController.GetType(), "AccessoryDynamicBoneData").GetValue(dbController);
            currentList.Clear();
            CopyList(dynamicBoneList, currentList);

        }

        private void CopyList(IList source, IList destination)
        {
            foreach (object o in source)
            {
                destination.Add(o);
            }
        }

        private static bool CheckKey(object dataOne, object dataTwo)
        {
            if (ExtractCoordinateIndex(dataOne) == ExtractCoordinateIndex(dataTwo)
                && ExtractSlot(dataOne) == ExtractSlot(dataTwo)
                && ExtractBoneName(dataOne).Equals(ExtractBoneName(dataTwo)))
                return true;
            else
                return false;
        }

        private static int ExtractCoordinateIndex(object data)
        {
            return (int)data.GetType().GetField("CoordinateIndex", AccessTools.all).GetValue(data);
        }

        private static int ExtractSlot(object data)
        {
            return (int)data.GetType().GetField("Slot", AccessTools.all).GetValue(data);
        }

        private static string ExtractBoneName(object data)
        {
            return (string)data.GetType().GetField("BoneName", AccessTools.all).GetValue(data);
        }

        private static void UpdateSlot(object data, int newSlot)
        {
            data.GetType().GetField("Slot", AccessTools.all).SetValue(data, newSlot);
        }
    }
}
