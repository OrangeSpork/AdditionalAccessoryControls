using AIChara;
using BepInEx.Logging;
using HarmonyLib;
using RootMotion.FinalIK;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryAdvancedParentSkinnedMeshHelper : MonoBehaviour
    {

        public OCIChar OCIChar { get; set; }

        public ChaControl ChaControl { get; set; }

        public bool RenderAlways { get; set; } = false;

        public int UpdateNFrames { get; set; }
        public int FrameHistoryCount { get; set; }
        public float FastActionThreshold { get; set; }

        private bool FKUpdated = false;
        private bool IKUpdated = false;
        private bool CHAControlUpdated = false;
        private bool BoneControllerUpdated = false;
        private bool FKControlled = false;
        private bool IKControlled = false;
        private int IKUpdateCount = 0;

        private ManualLogSource Log => AdditionalAccessoryControlsPlugin.Instance.Log;

        private Mesh bakedMesh;
        private List<Vector3> vertexList;
        
   //     private List<Vector3> normalList;

        private SkinnedMeshRenderer skinnedMeshRenderer;

        private float[] blendShapeWeights;

        private Dictionary<int, List<SkinnedMeshUpdateFrame>> frameSet = new Dictionary<int, List<SkinnedMeshUpdateFrame>>();

        // Listeners
        private Dictionary<int, List<Action<SkinnedMeshRenderedVertex>>> SkinnedMeshRendererListeners = new Dictionary<int, List<Action<SkinnedMeshRenderedVertex>>>();

        private static List<AdditionalAccessoryAdvancedParentSkinnedMeshHelper> Helpers = new List<AdditionalAccessoryAdvancedParentSkinnedMeshHelper>();

        private Delegate PostUpdateIKDelegate;

        public static void ExternalUpdate(ChaControl chaControl, bool IKUpdate, bool FKUpdate, bool ChaControlUpdate, bool BoneControllerUpdate)
        {
            foreach (AdditionalAccessoryAdvancedParentSkinnedMeshHelper helper in Helpers)
            {
                if (helper.ChaControl == chaControl)
                    helper.ExternalUpdate(IKUpdate, FKUpdate, ChaControlUpdate, BoneControllerUpdate);
            }
        }

        private void Awake()
        {            
            Helpers.Add(this);

            UpdateNFrames = AdditionalAccessoryControlsPlugin.UpdateBodyPositionEveryNFrames.Value;
            FrameHistoryCount = AdditionalAccessoryControlsPlugin.BodyPositionHistoryFrames.Value;
            FastActionThreshold = AdditionalAccessoryControlsPlugin.BodyPositionFastActionThreshold.Value;

            skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            bakedMesh = new Mesh();
            vertexList = new List<Vector3>();
   //         normalList = new List<Vector3>();

#if DEBUG
            Log.LogInfo($"Bringing online new mesh helper for {gameObject.name}");
#endif
        }

        private void OnDestroy()
        {
            Helpers.Remove(this);
        }

        private void Update()
        {
            if (KKAPI.Studio.StudioAPI.InsideStudio && ChaControl != null && OCIChar == null)
            {
                OCIChar = KKAPI.Studio.StudioObjectExtensions.GetOCIChar(ChaControl);
            }

            FKUpdated = false;
            IKUpdated = false;
            IKUpdateCount = 0;
            CHAControlUpdated = false;
            BoneControllerUpdated = false;

            if (RenderAlways && OCIChar != null)
            {
                if (OCIChar.fkCtrl.enabled)
                {
                    FKControlled = true;
                }
                else
                {
                    FKControlled = false;
                    FKUpdated = true;
                }
                if (OCIChar.finalIK.enabled)
                {
                    IKControlled = true;
                    if (PostUpdateIKDelegate == null)
                    {
                        PostUpdateIKDelegate = new IKSolver.UpdateDelegate(() =>
                        {
                            ExternalUpdate(true, false, false, false);
                        });
                        OCIChar.finalIK.solver.OnPostUpdate = (IKSolver.UpdateDelegate)Delegate.Combine(OCIChar.finalIK.solver.OnPostUpdate, PostUpdateIKDelegate);
                    }
                }
                else
                {
                    IKControlled = false;
                    IKUpdated = true;                    
                    OCIChar.finalIK.solver.OnPostUpdate = (IKSolver.UpdateDelegate)Delegate.Remove(OCIChar.finalIK.solver.OnPostUpdate, PostUpdateIKDelegate);
                    PostUpdateIKDelegate = null;
                }
            }            
        }

        private bool BlendShapesDirty()
        {
            bool dirty = false;
            if (blendShapeWeights == null)
            {
                dirty = true;
                blendShapeWeights = new float[skinnedMeshRenderer.sharedMesh.blendShapeCount];
            }
            else if (blendShapeWeights.Length != skinnedMeshRenderer.sharedMesh.blendShapeCount)
            {
                dirty = true;
                blendShapeWeights = new float[skinnedMeshRenderer.sharedMesh.blendShapeCount];
            }

            for (int i = 0; i < blendShapeWeights.Length; i++)
            {
                float weight = skinnedMeshRenderer.GetBlendShapeWeight(i);
                if (!ApproximatelyEquals(weight, blendShapeWeights[i]))
                {
                    dirty = true;
                }

                blendShapeWeights[i] = weight;
            }

            return dirty;
        }

        private bool ApproximatelyEquals(float one, float two)
        {
            return Math.Abs(one - two) < 0.0001;
        }

        private Vector3 AverageDelta(List<SkinnedMeshUpdateFrame> frames)
        {
            Vector3 averageDelta = Vector3.zero;
            int count = 0;
            foreach (SkinnedMeshUpdateFrame frame in frames)
            {
                if (frame.delta != Vector3.zero)
                {
                    averageDelta += frame.delta;
                    count++;
                }
            }
            if (count > 0)
                return averageDelta / count;
            else
                return averageDelta;
        }


        float lastUpdateTime = 0;
        private bool NeedsTimeUpdate()
        {
            if (Time.time == lastUpdateTime)
            {
                return false;
            }            
            if (RenderAlways)
            {
                foreach (List<SkinnedMeshUpdateFrame> list in frameSet.Values)
                {
                    if (list.Count < FrameHistoryCount)
                        return true;

                    if (AverageDelta(list).sqrMagnitude > FastActionThreshold)
                    {
                        return true;
                    }
                }

                if (Time.frameCount % UpdateNFrames == 0)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private bool AllUpdatesIn()
        {
            if (RenderAlways && OCIChar != null)
                return IKUpdated && FKUpdated && CHAControlUpdated && BoneControllerUpdated;
            else
                return CHAControlUpdated && BoneControllerUpdated;
        }

        private static Type AdvIKShoulderRotatorType = AccessTools.TypeByName("AdvIKPlugin.AdvIKShoulderRotator");
        public void ExternalUpdate(bool IKUpdate, bool FKUpdate, bool ChaControlLateUpdate, bool BoneControllerLateUpdate)
        {
            if (IKUpdate)
            {                
                IKUpdateCount++;
                if (AdvIKShoulderRotatorType != null && ChaControl.objAnim.GetComponent(AdvIKShoulderRotatorType) != null && IKUpdateCount > 1)
                {
                    IKUpdated = true;
                }
                else if (AdvIKShoulderRotatorType != null && ChaControl.objAnim.GetComponent(AdvIKShoulderRotatorType) == null && IKUpdateCount > 0)
                {
                    IKUpdated = true;
                }
                else if (AdvIKShoulderRotatorType == null && IKUpdateCount > 0)
                {
                    IKUpdated = true;
                }

                if (IKControlled && AllUpdatesIn())
                {
                    DoUpdate();
                }
            }
            else if (FKUpdate)
            {
                FKUpdated = true;
                if (FKControlled && AllUpdatesIn())
                {
                    DoUpdate();
                }
            }
            else if (ChaControlLateUpdate)
            {
                CHAControlUpdated = true;
                if (AllUpdatesIn())
                {
                    DoUpdate();
                }
            }
            else if (BoneControllerLateUpdate)
            {
                BoneControllerUpdated = true;
                if (AllUpdatesIn())
                {
                    DoUpdate();
                }
            }
        }

        private void LateUpdate()
        {
        }

        private void DoUpdate()
        {
            try
            {
                if (gameObject == null || !gameObject.activeInHierarchy)
                {
                    return;
                }

                bool updated = false;
                if (BlendShapesDirty() || NeedsTimeUpdate())
                {
                    skinnedMeshRenderer.BakeMesh(bakedMesh);

                    bakedMesh.GetVertices(vertexList);
                    //             bakedMesh.GetNormals(normalList);

                    updated = true;
                    lastUpdateTime = Time.time;
                }

                foreach (int vertexId in SkinnedMeshRendererListeners.Keys)
                {
                    List<SkinnedMeshUpdateFrame> frameList = frameSet[vertexId];
                    Vector3 verticeLocation = vertexList[vertexId];
                    //             Vector3 normalDirection = normalList[vertexId];

                    SkinnedMeshRenderedVertex vertex;
                    if (!RenderAlways)
                    {
                        vertex = new SkinnedMeshRenderedVertex(transform.TransformPoint(verticeLocation), Vector3.zero, verticeLocation, Vector3.zero);
                    }
                    else if (updated)
                    {
                        frameList.Add(new SkinnedMeshUpdateFrame(verticeLocation, Time.time, Vector3.zero));
                        if (frameList.Count > 1)
                            frameList[frameList.Count - 2] = new SkinnedMeshUpdateFrame(frameList[frameList.Count - 2].position, frameList[frameList.Count - 2].time, (frameList[frameList.Count - 1].position - frameList[frameList.Count - 2].position) / (frameList[frameList.Count - 1].time - frameList[frameList.Count - 2].time));
                        while (frameList.Count > FrameHistoryCount)
                            frameList.RemoveAt(0);

                        vertex = new SkinnedMeshRenderedVertex(transform.TransformPoint(verticeLocation), Vector3.zero, verticeLocation, Vector3.zero);
                        //                SkinnedMeshRenderedVertex vertex = new SkinnedMeshRenderedVertex(transform.TransformPoint(verticeLocation), transform.TransformDirection(normalDirection), verticeLocation, normalDirection);
                    }
                    else
                    {
                        // Extrapolation time
                        Vector3 extrapolatedMovement = AverageDelta(frameList);
                        Vector3 predictedPosition = frameList[frameList.Count - 1].position + (extrapolatedMovement * (Time.time - frameList[frameList.Count - 1].time));
                        vertex = new SkinnedMeshRenderedVertex(transform.TransformPoint(predictedPosition), Vector3.zero, predictedPosition, Vector3.zero);
                    }

                    foreach (Action<SkinnedMeshRenderedVertex> listener in SkinnedMeshRendererListeners[vertexId])
                    {
                        listener.Invoke(vertex);
                    }
                }
            } catch (Exception e)
            {
                Log.LogWarning($"Error in accessory computation: {e.Message}\n{e.StackTrace}");
            }
        }

        public void RegisterVertexListener(int vertexId, Action<SkinnedMeshRenderedVertex> updateListener)
        {
            if (!SkinnedMeshRendererListeners.ContainsKey(vertexId))
            {
                SkinnedMeshRendererListeners[vertexId] = new List<Action<SkinnedMeshRenderedVertex>>();
                frameSet[vertexId] = new List<SkinnedMeshUpdateFrame>();
            }

            SkinnedMeshRendererListeners[vertexId].Add(updateListener);

#if DEBUG
            Log.LogInfo($"Adding Listener to {gameObject.name}:{vertexId}");
#endif
        }

        public void UnRegisterVertexListener(int vertexId, Action<SkinnedMeshRenderedVertex> updateListener)
        {
            if (SkinnedMeshRendererListeners.ContainsKey(vertexId))
            {
                SkinnedMeshRendererListeners[vertexId].Remove(updateListener);
                if (SkinnedMeshRendererListeners[vertexId].Count == 0)
                {
                    SkinnedMeshRendererListeners.Remove(vertexId);
                    frameSet.Remove(vertexId);
                }

#if DEBUG
                Log.LogInfo($"Removing Listener from {gameObject.name}:{vertexId}");
#endif

                if (SkinnedMeshRendererListeners.Keys.Count == 0)
                {
#if DEBUG
                    Log.LogInfo($"No more listeners, shutting down mesh helper component {gameObject.name}");
#endif
                    DestroyImmediate(this);
                }
            }
        }

    }

    internal struct SkinnedMeshUpdateFrame
    {
        public Vector3 position { get; set; }
        public float time { get; set; }
        public Vector3 delta { get; set; }

        public SkinnedMeshUpdateFrame(Vector3 pos, float time, Vector3 delta)
        {
            this.position = pos;
            this.time = time;
            this.delta = delta;
        }
    }

    public struct SkinnedMeshRenderedVertex
    {
        public Vector3 position { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localEulerAngles { get; set; }

        public SkinnedMeshRenderedVertex(Vector3 position, Vector3 eulerAngles, Vector3 localPosition, Vector3 localEulerAngles)
        {
            this.position = position;
            this.eulerAngles = eulerAngles;
            this.localPosition = localPosition;
            this.localEulerAngles = localEulerAngles;
        }

    }
}
