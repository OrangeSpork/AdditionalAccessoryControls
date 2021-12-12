using AIChara;
using BepInEx.Logging;
using HarmonyLib;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryAdvancedParentController : MonoBehaviour
    {
        private string linkParent;
        public string LinkParent
        {
            get { return linkParent; }
            set
            {
                if (linkParent != value)
                {
                    linkParent = value;
                    UpdateParent();
                }
            }
        }

        public ChaControl ChaControl { get; set; }

        public MonoBehaviour DynamicBone { get; set; }



        private bool dynamicBonesInstantiated = true;
        private Transform parentTransform;
        private int[] vertices;
        private AdditionalAccessoryAdvancedParentSkinnedMeshHelper meshHelper;
        private AdditionalAccessoryAdvancedParentSkinnedMeshHelper altMeshHelper;

        private ManualLogSource Log => AdditionalAccessoryControlsPlugin.Instance.Log;

        private static List<AdditionalAccessoryAdvancedParentController> Helpers = new List<AdditionalAccessoryAdvancedParentController>();

        private void Awake()
        {
            Helpers.Add(this);
        }

        public static void ExternalUpdate(ChaControl chaControl)
        {
            foreach (AdditionalAccessoryAdvancedParentController helper in Helpers)
            {
                if (helper.ChaControl == chaControl)
                    helper.OnLateFKUpdate();
            }
        }

        private void UpdateParent()
        {
            parentTransform = null;
            gameObject.transform.position = Vector3.zero;
            gameObject.transform.eulerAngles = Vector3.zero;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localEulerAngles = Vector3.zero;


            if (meshHelper != null)
            {
                meshHelper.UnRegisterVertexListener(vertices[0], OnSkinnedMeshUpdate);
                vertices = null;
                meshHelper = null;
            }

            if (altMeshHelper != null)
            {
                altMeshHelper.UnRegisterVertexListener(vertices[0], OnSkinnedMeshUpdate);
                altMeshHelper = null;
            }

            if (LinkParent != null && LinkParent.Length > 0)
            {
                if (LinkParent.IndexOf("|") > 0)
                {
                    try
                    {
#if DEBUG
                        Log.LogInfo($"Linking to Mesh Vertice at Transform location {LinkParent} | index {LinkParent.IndexOf("|")} Search: {LinkParent.Substring(0, LinkParent.IndexOf("|"))}");
#endif
                        parentTransform = ChaControl.gameObject.transform.Find(LinkParent.Substring(0, LinkParent.IndexOf("|")))?.transform;
                        if (parentTransform != null)
                        {
                            string verticesString = LinkParent.Substring(LinkParent.IndexOf("|") + 1, LinkParent.Length - LinkParent.IndexOf("|") - 1);
                            string[] verticeStrings = verticesString.Split('|');
                            vertices = new int[verticeStrings.Length];
                            for (int i = 0; i < verticeStrings.Length; i++)
                            {
                                vertices[i] = int.Parse(verticeStrings[i]);
                            }

                            meshHelper = parentTransform.gameObject.GetOrAddComponent<AdditionalAccessoryAdvancedParentSkinnedMeshHelper>();
                            meshHelper.ChaControl = ChaControl;
                            if (AnimatedBoneNames.Contains(parentTransform.gameObject.name))
                                meshHelper.RenderAlways = true;

                            meshHelper.RegisterVertexListener(vertices[0], OnSkinnedMeshUpdate);

                            if (parentTransform.name == "o_tang")
                            {
                                altMeshHelper = ChaControl.transform.Find("BodyTop/p_cf_body_00/n_o_root/n_body_base/N_mnpbset/N_tang/o_tang").gameObject.GetOrAddComponent<AdditionalAccessoryAdvancedParentSkinnedMeshHelper>();
                                altMeshHelper.ChaControl = ChaControl;
                                altMeshHelper.RenderAlways = true;
                                altMeshHelper.RegisterVertexListener(vertices[0], OnSkinnedMeshUpdate);
                            }

#if DEBUG
                            Log.LogInfo($"Attaching to SM at {parentTransform} Vertices {vertices}");
#endif
                        }
                    }
                    catch
                    {
                        vertices = null;
                        LinkParent = null;
                        parentTransform = null;
                        meshHelper = null;
                        altMeshHelper = null;
                    }
                }
                else
                    parentTransform = ChaControl.gameObject.transform.Find(LinkParent)?.transform;
            }
#if DEBUG
            if (parentTransform)
                Log.LogInfo($"Linked to {parentTransform}  {LinkParent}");
            else
                Log.LogInfo($"Parent Link Not Found {LinkParent}");
#endif

            // Re-Check for dynamic bones in the parent hierarchy
            if (DynamicBone != null && DynamicBone.GetType() == typeof(DynamicBone))
                AdditionalAccessoryControlDynamicBoneUpdateManager.UnRegisterDynamicBone((DynamicBone)DynamicBone, OnDynamicBoneUpdate);
            else if (DynamicBone != null && DynamicBone.GetType() == typeof(DynamicBone_Ver02))
                AdditionalAccessoryControlDynamicBoneUpdateManager.UnRegisterDynamicBone((DynamicBone_Ver02)DynamicBone, OnDynamicBoneV2Update);

            DynamicBone = null;
            dynamicBonesInstantiated = true;
            if (parentTransform != null)
                ScanForDynamicBone();
        }

        private static FieldInfo mParticlesField = AccessTools.Field(typeof(DynamicBone), "m_Particles");
        private static Type particleType = AccessTools.Inner(typeof(DynamicBone), "Particle");
        private static FieldInfo particleTransformField = AccessTools.Field(particleType, "m_Transform");
        private static FieldInfo mParticles2Field = AccessTools.Field(typeof(DynamicBone_Ver02), "Particles");

        private void ScanForDynamicBone()
        {
            ScanForDynamicBoneInHierarchy();

            if (DynamicBone == null)
            {
                ScanForDynamicBoneInClothing();
            }
        }

        private List<Transform> BuildParentTransforms()
        {
            List<Transform> parentTransforms = new List<Transform>();

            Transform checkTransform = parentTransform;
            while (checkTransform != null)
            {
                parentTransforms.Add(checkTransform);
                if (checkTransform == ChaControl.gameObject.transform)
                    return parentTransforms;

                checkTransform = checkTransform.transform.parent;
            }

            return parentTransforms;
        }

        private void ScanForDynamicBoneInClothing()
        {
            if (parentTransform != null)
            {
                List<Transform> parentTransforms = BuildParentTransforms();
                foreach (GameObject clotheObject in ChaControl.objClothes)
                {
                    if (CheckAndLinkDynamicBone(clotheObject, parentTransforms))
                        return;
                }
            }
        }

        private void ScanForDynamicBoneInHierarchy()
        {
            if (parentTransform != null)
            {
                List<Transform> parentTransforms = new List<Transform>();

                Transform checkParentTransform = parentTransform;
                while (checkParentTransform != null)
                {
                    parentTransforms.Add(checkParentTransform);

                    if (CheckAndLinkDynamicBone(checkParentTransform.gameObject, parentTransforms))
                        return;
                    else if (checkParentTransform == ChaControl.gameObject.transform)
                        return;

                    checkParentTransform = checkParentTransform.parent;
                }
            }
        }

        private bool CheckAndLinkDynamicBone(GameObject go, List<Transform> parentTransforms)
        {
            if (go == null)
                return false;

            DynamicBone[] bones = go.GetComponents<DynamicBone>();
            if (bones != null && bones.Length > 0)
            {
                foreach (DynamicBone bone in bones)
                {
                    if (DynamicBoneInyMyHierarchy(bone, parentTransforms))
                    {
                        DynamicBone = bone;
                        dynamicBonesInstantiated = true;
                        AdditionalAccessoryControlDynamicBoneUpdateManager.RegisterDynamicBone(bone, OnDynamicBoneUpdate);
#if DEBUG
                        Log.LogInfo($"Dynamic Bone Link Established: {bone.gameObject.name} {bone.gameObject.GetInstanceID()}");
#endif
                        return true;
                    }
                    else if (!DynamicBoneSetup(bone))
                    {
                        dynamicBonesInstantiated = false;
                    }
                }
            }

            DynamicBone_Ver02[] bones2 = go.GetComponents<DynamicBone_Ver02>();
            if (bones2 != null && bones2.Length > 0)
            {
                foreach (DynamicBone_Ver02 bone in bones2)
                {
                    if (DynamicBoneInyMyHierarchy(bone, parentTransforms))
                    {
                        DynamicBone = bone;
                        dynamicBonesInstantiated = true;
                        AdditionalAccessoryControlDynamicBoneUpdateManager.RegisterDynamicBone(bone, OnDynamicBoneV2Update);
#if DEBUG
                        Log.LogInfo($"Dynamic Bone Link Established: {bone.gameObject.name} {bone.gameObject.GetInstanceID()}");
#endif
                        return true;
                    }
                    else if (!DynamicBoneSetup(bone))
                    {
                        dynamicBonesInstantiated = false;
                    }
                }
            }

            return false;
        }

        private bool DynamicBoneInyMyHierarchy(DynamicBone bone, List<Transform> parentTransforms)
        {
            IList particleList = (IList)mParticlesField.GetValue(bone);
            foreach (object particle in particleList)
            {
                if (ParticleIsParentTransform(particle, parentTransforms))
                    return true;
            }
            return false;
        }

        private bool DynamicBoneInyMyHierarchy(DynamicBone_Ver02 bone, List<Transform> parentTransforms)
        {
            List<DynamicBone_Ver02.Particle> particleList = (List<DynamicBone_Ver02.Particle>)mParticles2Field.GetValue(bone);
            foreach (DynamicBone_Ver02.Particle particle in particleList)
            {
                if (ParticleIsParentTransform(particle, parentTransforms))
                    return true;
            }
            return false;
        }

        private bool DynamicBoneSetup(DynamicBone bone)
        {
            IList particleList = (IList)mParticlesField.GetValue(bone);
            foreach (object particle in particleList)
            {
                if (particleTransformField.GetValue(particle) != null)
                    return true;
            }
            return false;
        }

        private bool DynamicBoneSetup(DynamicBone_Ver02 bone)
        {
            List<DynamicBone_Ver02.Particle> particleList = (List<DynamicBone_Ver02.Particle>)mParticles2Field.GetValue(bone);
            foreach (DynamicBone_Ver02.Particle particle in particleList)
            {
                if (particle.Transform != null)
                    return true;
            }
            return false;
        }

        private bool ParticleIsParentTransform(object particle, List<Transform> parentTransforms)
        {
            if (particle.GetType() == particleType)
                return parentTransforms.Contains((Transform)particleTransformField.GetValue(particle));
            else
                return parentTransforms.Contains(((DynamicBone_Ver02.Particle)particle).Transform);
        }

        public void OnDynamicBoneUpdate(DynamicBone bone)
        {
#if DEBUG
            if (parentTransform == null)
            {
                Log.LogInfo($"Receiving Update on Dead Controller: {bone.name}");
            }
#endif
            LateUpdate();
        }

        public void OnLateFKUpdate()
        {
            LateUpdate();
        }

        public void OnDynamicBoneV2Update(DynamicBone_Ver02 bone)
        {
#if DEBUG
            if (parentTransform == null)
            {
                Log.LogInfo($"Receiving Update on Dead Controller: {bone.name}");
            }
#endif
            LateUpdate();
        }

        public void OnSkinnedMeshUpdate(SkinnedMeshRenderedVertex vertex)
        {
            try
            {
                if (!this.enabled || this.gameObject == null)
                    return;

                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localEulerAngles = Vector3.zero;

                gameObject.transform.position = vertex.position;
                //      gameObject.transform.eulerAngles = gameObject.transform.TransformDirection(gameObject.transform.parent.InverseTransformDirection(gameObject.transform.parent.eulerAngles));
            }
            catch (Exception skinnedMeshUpdateErr)
            {
#if DEBUG
                Log.LogError($"Error in Adv Parent Late Update: {skinnedMeshUpdateErr.Message}\n{skinnedMeshUpdateErr.StackTrace});
#endif
            }
        }

        private bool eofCoroutineRunning = false;
        private void LateUpdate()
        {
            try
            {
                if (!this.enabled || this.gameObject == null)
                    return;

                if (LinkParent != null && parentTransform == null)
                {
                    UpdateParent();
                }
                if (DynamicBone == null && !dynamicBonesInstantiated)
                {
                    ScanForDynamicBone();
                }

                if (parentTransform != null)
                {
                    if (meshHelper == null)
                    {
                        gameObject.transform.localPosition = Vector3.zero;
                        gameObject.transform.localEulerAngles = Vector3.zero;

                        gameObject.transform.position = parentTransform.position;
                        gameObject.transform.eulerAngles = parentTransform.eulerAngles;

#if DEBUG
                    if (Time.frameCount % 60 == 0)
                    {
                        Log.LogInfo($"Updating {gameObject.name} {gameObject.GetInstanceID()} My Pos: {gameObject.transform.position} Par Pos: {parentTransform.position}");
                        if (!eofCoroutineRunning)
                        {
                            StartCoroutine(EndOfFrame());
                            eofCoroutineRunning = true;
                        }
                    }
                    
#endif
                    }
                }
            }
            catch (Exception lateUpdateErr)
            {
#if DEBUG
                Log.LogError($"Error in Adv Parent Late Update: {lateUpdateErr.Message}\n{lateUpdateErr.StackTrace});
#endif
            }
        }

        private IEnumerator EndOfFrame()
        {
            yield return new WaitForEndOfFrame();

#if DEBUG
            if (Time.frameCount % 60 == 0)
                Log.LogInfo($"EOF {gameObject.name} {gameObject.GetInstanceID()} My Pos: {gameObject.transform.position} Par Pos: {parentTransform.position}");

            eofCoroutineRunning = false;
#endif
        }

        private void OnDestroy()
        {
            Helpers.Remove(this);

            if (DynamicBone != null && DynamicBone.GetType() == typeof(DynamicBone))
                AdditionalAccessoryControlDynamicBoneUpdateManager.UnRegisterDynamicBone((DynamicBone)DynamicBone, OnDynamicBoneUpdate);
            else if (DynamicBone != null && DynamicBone.GetType() == typeof(DynamicBone_Ver02))
                AdditionalAccessoryControlDynamicBoneUpdateManager.UnRegisterDynamicBone((DynamicBone_Ver02)DynamicBone, OnDynamicBoneV2Update);

            if (meshHelper != null)
                meshHelper.UnRegisterVertexListener(vertices[0], OnSkinnedMeshUpdate);
            if (altMeshHelper != null)
                altMeshHelper.UnRegisterVertexListener(vertices[0], OnSkinnedMeshUpdate);

        }

        public static string[] AnimatedBoneNames = { "o_body_cm", "o_body_cf", "cm_o_dan00", "cm_o_dan_f" };
    }
}
