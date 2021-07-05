using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AdditionalAccessoryControls
{
    public static class AdditionalAccessoryControlDynamicBoneUpdateManager
    {

        public static Dictionary<WeakReference<DynamicBone>, List<Action<DynamicBone>>> DynamicBoneUpdateListeners = new Dictionary<WeakReference<DynamicBone>, List<Action<DynamicBone>>>();
        public static Dictionary<WeakReference<DynamicBone_Ver02>, List<Action<DynamicBone_Ver02>>> DynamicBoneV2UpdateListeners = new Dictionary<WeakReference<DynamicBone_Ver02>, List<Action<DynamicBone_Ver02>>>();

        private static ManualLogSource Log => AdditionalAccessoryControlsPlugin.Instance.Log;

        public static void InvokeUpdateListeners(DynamicBone bone)
        {
            foreach (WeakReference<DynamicBone> wr in DynamicBoneUpdateListeners.Keys)
            {
                bool found = wr.TryGetTarget(out DynamicBone checkbone);
                if (found && checkbone == bone)
                {
                    foreach (Action<DynamicBone> listener in DynamicBoneUpdateListeners[wr])
                    {
                        listener?.Invoke(bone);
                    }
                }
            }
        }

        public static void InvokeUpdateListeners(DynamicBone_Ver02 bone)
        {
            foreach (WeakReference<DynamicBone_Ver02> wr in DynamicBoneV2UpdateListeners.Keys)
            {
                bool found = wr.TryGetTarget(out DynamicBone_Ver02 checkbone);
                if (found && checkbone == bone)
                {
                    foreach (Action<DynamicBone_Ver02> listener in DynamicBoneV2UpdateListeners[wr])
                    {
                        listener?.Invoke(bone);
                    }
                }
            }
        }

        public static void RegisterDynamicBone(DynamicBone bone, Action<DynamicBone> updateListener)
        {
            bool foundBone = false;
            foreach ( WeakReference<DynamicBone> wr in DynamicBoneUpdateListeners.Keys )
            {
                bool found = wr.TryGetTarget(out DynamicBone checkBone);
                if (found && checkBone == bone)
                {
#if DEBUG
                    Log.LogInfo($"Adding Listener to {bone.name}");
#endif
                    DynamicBoneUpdateListeners[wr].Add(updateListener);
                    foundBone = true;
                    break;
                }
            }

            if (!foundBone)
            {
#if DEBUG
                Log.LogInfo($"Adding new Bone Listener {bone.name}");
#endif
                List<Action<DynamicBone>> listeners = new List<Action<DynamicBone>>();
                listeners.Add(updateListener);
                DynamicBoneUpdateListeners[new WeakReference<DynamicBone>(bone)] = listeners;
            }
        }

        public static void RegisterDynamicBone(DynamicBone_Ver02 bone, Action<DynamicBone_Ver02> updateListener)
        {
            bool foundBone = false;
            foreach (WeakReference<DynamicBone_Ver02> wr in DynamicBoneV2UpdateListeners.Keys)
            {
                bool found = wr.TryGetTarget(out DynamicBone_Ver02 checkBone);
                if (found && checkBone == bone)
                {
#if DEBUG
                    Log.LogInfo($"Adding Listener to {bone.name}");
#endif
                    DynamicBoneV2UpdateListeners[wr].Add(updateListener);
                    foundBone = true;
                    break;
                }
            }

            if (!foundBone)
            {
#if DEBUG
                Log.LogInfo($"Adding new Bone Listener {bone.name}");
#endif
                List<Action<DynamicBone_Ver02>> listeners = new List<Action<DynamicBone_Ver02>>();
                listeners.Add(updateListener);
                DynamicBoneV2UpdateListeners[new WeakReference<DynamicBone_Ver02>(bone)] = listeners;
            }
        }

        public static void UnRegisterDynamicBone(DynamicBone bone, Action<DynamicBone> updateListener)
        {
            foreach (WeakReference<DynamicBone> wr in DynamicBoneUpdateListeners.Keys)
            {
                bool found = wr.TryGetTarget(out DynamicBone checkBone);
                if (found && checkBone == bone)
                {
#if DEBUG
                    Log.LogInfo($"Removing listener from {bone.name}");
#endif
                    DynamicBoneUpdateListeners[wr].Remove(updateListener);

                    if (DynamicBoneUpdateListeners[wr].Count == 0)
                        DynamicBoneUpdateListeners.Remove(wr);

                    break;
                }
            }
        }

        public static void UnRegisterDynamicBone(DynamicBone_Ver02 bone, Action<DynamicBone_Ver02> updateListener)
        {
            foreach (WeakReference<DynamicBone_Ver02> wr in DynamicBoneV2UpdateListeners.Keys)
            {
                bool found = wr.TryGetTarget(out DynamicBone_Ver02 checkBone);
                if (found && checkBone == bone)
                {
#if DEBUG
                    Log.LogInfo($"Removing listener from {bone.name}");
#endif
                    DynamicBoneV2UpdateListeners[wr].Remove(updateListener);

                    if (DynamicBoneV2UpdateListeners[wr].Count == 0)
                        DynamicBoneV2UpdateListeners.Remove(wr);

                    break;
                }
            }
        }

        private static float lastRunTime;
        private static List<WeakReference<DynamicBone_Ver02>> keysV2ToRemove = new List<WeakReference<DynamicBone_Ver02>>();
        private static List<WeakReference<DynamicBone>> keysToRemove = new List<WeakReference<DynamicBone>>();
        public static void ReapInactiveDynamicBones()
        {
            if (Time.time <= lastRunTime + 5)
                return;

            keysToRemove.Clear();
            foreach (WeakReference<DynamicBone> wr in DynamicBoneUpdateListeners.Keys)
            {
                if (!wr.TryGetTarget(out DynamicBone bone))
                {
                    keysToRemove.Add(wr);
                }
            }

            foreach (WeakReference<DynamicBone> wr in keysToRemove)
                DynamicBoneUpdateListeners.Remove(wr);


            keysV2ToRemove.Clear();
            foreach (WeakReference<DynamicBone_Ver02> wr in DynamicBoneV2UpdateListeners.Keys)
            {
                if (!wr.TryGetTarget(out DynamicBone_Ver02 bone))
                {
                    keysV2ToRemove.Add(wr);
                }
            }
            foreach (WeakReference<DynamicBone_Ver02> wr in keysV2ToRemove)
                DynamicBoneV2UpdateListeners.Remove(wr);

            lastRunTime = Time.time;
        }
    }
}
