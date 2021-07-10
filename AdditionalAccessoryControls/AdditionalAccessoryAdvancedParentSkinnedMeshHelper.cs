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

        private ManualLogSource Log => AdditionalAccessoryControlsPlugin.Instance.Log;

        private Mesh bakedMesh;
        private List<Vector3> vertexList;
        private List<Vector3> normalList;

        private SkinnedMeshRenderer skinnedMeshRenderer;

        private float[] blendShapeWeights;

        // Listeners
        private Dictionary<int, List<Action<SkinnedMeshRenderedVertex>>> SkinnedMeshRendererListeners = new Dictionary<int, List<Action<SkinnedMeshRenderedVertex>>>();

        private void Awake()
        {
            skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            bakedMesh = new Mesh();
            vertexList = new List<Vector3>();
            normalList = new List<Vector3>();

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
#if DEBUG
                    Log.LogInfo($"Index {i} Old {blendShapeWeights[i]:000.00000} New {weight:000.00000}");
#endif
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

        private void LateUpdate()
        {
            if (BlendShapesDirty())
            {
#if DEBUG
                Log.LogInfo($"Mesh {gameObject.name} Dirty");
#endif
                skinnedMeshRenderer.BakeMesh(bakedMesh);

                bakedMesh.GetVertices(vertexList);
                bakedMesh.GetNormals(normalList);
            }

            foreach (int vertexId in SkinnedMeshRendererListeners.Keys)
            {
                Vector3 verticeLocation = vertexList[vertexId];
                Vector3 normalDirection = normalList[vertexId];

                SkinnedMeshRenderedVertex vertex = new SkinnedMeshRenderedVertex(transform.TransformPoint(verticeLocation), transform.TransformDirection(normalDirection), verticeLocation, normalDirection);
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
