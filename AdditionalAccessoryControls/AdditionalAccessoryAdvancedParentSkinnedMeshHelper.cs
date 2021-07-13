using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryAdvancedParentSkinnedMeshHelper : MonoBehaviour
    {

        public bool RenderAlways { get; set; } = false;

        public int UpdateNFrames { get; set; }
        public int FrameHistoryCount { get; set; }
        public float FastActionThreshold { get; set; }

        private ManualLogSource Log => AdditionalAccessoryControlsPlugin.Instance.Log;

        private Mesh bakedMesh;
        private List<Vector3> vertexList;
   //     private List<Vector3> normalList;

        private SkinnedMeshRenderer skinnedMeshRenderer;

        private float[] blendShapeWeights;

        private Dictionary<int, List<SkinnedMeshUpdateFrame>> frameSet = new Dictionary<int, List<SkinnedMeshUpdateFrame>>();
        private Vector3 lastTransformPosition = Vector3.zero;

        // Listeners
        private Dictionary<int, List<Action<SkinnedMeshRenderedVertex>>> SkinnedMeshRendererListeners = new Dictionary<int, List<Action<SkinnedMeshRenderedVertex>>>();

        private void Awake()
        {
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

        private bool BlendShapesDirty()
        {
            bool dirty = false;
            if (blendShapeWeights == null)
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

        private bool NeedsTimeUpdate()
        {
            if (RenderAlways)
            {

                lastTransformPosition = transform.position;
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

        private void LateUpdate()
        {
            bool updated = false;
            if (BlendShapesDirty() || NeedsTimeUpdate())
            {
                skinnedMeshRenderer.BakeMesh(bakedMesh);

                bakedMesh.GetVertices(vertexList);
                //             bakedMesh.GetNormals(normalList);

                updated = true;                
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
                        frameList[frameList.Count - 2] = new SkinnedMeshUpdateFrame(frameList[frameList.Count - 2].position, frameList[frameList.Count-2].time ,(frameList[frameList.Count - 1].position - frameList[frameList.Count - 2].position) / (frameList[frameList.Count - 1].time - frameList[frameList.Count - 2].time));
                    while (frameList.Count > FrameHistoryCount)
                        frameList.RemoveAt(0);

                    vertex = new SkinnedMeshRenderedVertex(transform.TransformPoint(verticeLocation), Vector3.zero, verticeLocation, Vector3.zero);
                     //                SkinnedMeshRenderedVertex vertex = new SkinnedMeshRenderedVertex(transform.TransformPoint(verticeLocation), transform.TransformDirection(normalDirection), verticeLocation, normalDirection);
                }
                else
                {
                    // Extrapolation time
                    Vector3 extrapolatedMovement = AverageDelta(frameList);
                    Vector3 predictedPosition = frameList[frameList.Count - 1].position + (extrapolatedMovement * (Time.time - frameList[frameList.Count - 1].time)) ;
                    vertex = new SkinnedMeshRenderedVertex(transform.TransformPoint(predictedPosition), Vector3.zero, predictedPosition, Vector3.zero);
                }

                foreach (Action<SkinnedMeshRenderedVertex> listener in SkinnedMeshRendererListeners[vertexId])
                {
                    listener.Invoke(vertex);
                }
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
