using AIChara;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryAdvancedParentController : MonoBehaviour
    {
        private string linkParent;
        public string LinkParent {
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

        private ManualLogSource Log => AdditionalAccessoryControlsPlugin.Instance.Log;

        private void UpdateParent()
        {
            parentTransform = ChaControl.gameObject.transform.Find(LinkParent)?.transform;
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

            DynamicBone[] bones = go.GetComponentsInChildren<DynamicBone>(true);
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
                        Log.LogInfo($"Dynamic Bone Link Established: {bone.gameObject.name}");
#endif
                        return true;
                    }
                    else if (!DynamicBoneSetup(bone))
                    {
                        dynamicBonesInstantiated = false;
                    }
                }
            }

            DynamicBone_Ver02[] bones2 = go.GetComponentsInChildren<DynamicBone_Ver02>(true);
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
                        Log.LogInfo($"Dynamic Bone Link Established: {bone.gameObject.name}");
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
                return parentTransforms.Contains( ((DynamicBone_Ver02.Particle)particle).Transform );
        }

        public void OnDynamicBoneUpdate(DynamicBone bone)
        {            
            LateUpdate();
        }

        public void OnDynamicBoneV2Update(DynamicBone_Ver02 bone)
        {
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (LinkParent != null && parentTransform == null)
            {
                UpdateParent();
            }
            if (DynamicBone == null && !dynamicBonesInstantiated)
            {
                ScanForDynamicBone();
            }

            if (parentTransform)
            {
                gameObject.transform.position = parentTransform.position;
                gameObject.transform.eulerAngles = parentTransform.eulerAngles;
            }
        }

        private void OnDestroy()
        {
            if (DynamicBone != null && DynamicBone.GetType() == typeof(DynamicBone))
                AdditionalAccessoryControlDynamicBoneUpdateManager.UnRegisterDynamicBone((DynamicBone)DynamicBone, OnDynamicBoneUpdate);
            else if (DynamicBone != null && DynamicBone.GetType() == typeof(DynamicBone_Ver02))
                AdditionalAccessoryControlDynamicBoneUpdateManager.UnRegisterDynamicBone((DynamicBone_Ver02)DynamicBone, OnDynamicBoneV2Update);
        }
    }
}
