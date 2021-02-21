using KKABMX.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryBoneEffect : BoneEffect
    {
        public const string NOSE_BONE = "cf_J_Nose_r";
        public const string LEFT_EAR = "cf_J_EarBase_s_L";
        public const string RIGHT_EAR = "cf_J_EarBase_s_R";
        public const string LEFT_HAND = "cf_J_Hand_L";
        public const string RIGHT_HAND = "cf_J_Hand_R";
        public const string LEFT_FOOT = "cf_J_Foot01_L";
        public const string RIGHT_FOOT = "cf_J_Foot01_R";

        private string[] UsedBones = new string[] { NOSE_BONE, LEFT_EAR, RIGHT_EAR, LEFT_HAND, RIGHT_HAND, LEFT_FOOT, RIGHT_FOOT };

        public List<string> HiddenBones { get; set; }

        public bool ResetLeftEar { get; set; }
        public bool ResetRightEar { get; set; }

        private BoneModifierData hiddenBoneModifier = new BoneModifierData(new UnityEngine.Vector3(0.01f, 0.01f, 0.01f), 1f);
        private BoneModifierData earHiddenBoneModifier = new BoneModifierData(new UnityEngine.Vector3(1f, 1f, 1f), 0.01f);

        public AdditionalAccessoryBoneEffect()
        {
            HiddenBones = new List<string>();
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin)
        {
            return UsedBones;
        }

        public override BoneModifierData GetEffect(string bone, BoneController origin, CoordinateType coordinate)
        {
            // Ears...need extra help for some reason...
            bool hiddenBone = HiddenBones.Contains(bone);
            if (hiddenBone && bone.Equals(LEFT_EAR))
            {
               return earHiddenBoneModifier;
            }
            else if (!hiddenBone && bone.Equals(LEFT_EAR) && ResetLeftEar)
            {
                origin.NeedsFullRefresh = true;
                ResetLeftEar = false;
                return null;
            }
            else if (hiddenBone && bone.Equals(RIGHT_EAR))
            {
                return earHiddenBoneModifier;
            }
            else if (!hiddenBone && bone.Equals(RIGHT_EAR) && ResetRightEar)
            {
                origin.NeedsFullRefresh = true;
                ResetRightEar = false;
                return null;
            }           
            else if (hiddenBone)
            {
                return hiddenBoneModifier;
            } 
            else
            {
                return null;
            }
        }
    }
}
