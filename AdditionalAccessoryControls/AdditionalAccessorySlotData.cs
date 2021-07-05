using AIChara;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace AdditionalAccessoryControls
{
    [Serializable]
    [MessagePackObject]
    public class AdditionalAccessorySlotData
    {
        [Key(0)]
        public bool IsEmpty { get; set; }  // Slot is empty
        [Key(1)]
        public int SlotNumber { get; set; } // Slot Number
        [Key(2)]
        public int OriginalSlotNumber { get; set; } // Slot Number before character accessory movement shenanigans
        [Key(3)]
        public bool CharacterAccessory { get; set; } // Is a character accessory
        [Key(4)]
        public string AccessoryName { get; set; } // ...name...of the accessory...
        [Key(5)]
        public bool AutoMatchBackHairColor { get; set; } // When a coordinate card is loaded, please set this to current char back hair color
        [Key(6)]
        public List<AdditionalAccessoryVisibilityRuleData> VisibilityRules { get; set; } // Visibility Rules
        [Key(7)]
        public string AdvancedParent { get; set; }

        [IgnoreMember]
        public string AdvancedParentShort { 
            get
            {
                int lastSlash = AdvancedParent == null ? -1 : AdvancedParent.LastIndexOf("/") + 1;
                return AdvancedParent == null ? "None" : AdvancedParent.Substring(lastSlash);
            }
        }

        [Serializable]
        [MessagePackObject]
        public class AdditionalAccessoryVisibilityRuleData
        {
            [Key(0)]
            public AdditionalAccessoryVisibilityRules Rule { get; set; }
            [Key(1)]
            public string Modifier { get; set; }

            public AdditionalAccessoryVisibilityRuleData()
            {

            }

            public AdditionalAccessoryVisibilityRuleData(AdditionalAccessoryVisibilityRules rule, string mod)
            {
                this.Rule = rule;
                this.Modifier = mod;
            }


            public override string ToString()
            {
                return "(" + Rule.ToString() + ":" + Modifier + ")";
            }

            public AdditionalAccessoryVisibilityRuleData Copy()
            {
                AdditionalAccessoryVisibilityRuleData copy = new AdditionalAccessoryVisibilityRuleData();
                copy.Rule = this.Rule;
                copy.Modifier = this.Modifier;
                return copy;
            }
        }

        [IgnoreMember]
        public ChaFileAccessory.PartsInfo PartsInfo { get; set; }

        public AdditionalAccessorySlotData()
        {

        }

        public override string ToString()
        {
            return String.Format("Slot: {0} Acc: {4} Original Slot: {1} Is_Empty: {2} Char_Acc: {3} Auto Match Hair: {7} Id: {5} Type: {6} AdvParent: {9} V.Rules: ({8})", SlotNumber, OriginalSlotNumber, IsEmpty, CharacterAccessory, AccessoryName, PartsInfo?.id, PartsInfo?.type, AutoMatchBackHairColor, (VisibilityRules == null ? "" : string.Join(",", VisibilityRules)), AdvancedParent);
        }

        public void ClearVisibilityRule(AdditionalAccessoryVisibilityRules rule)
        {
            if (VisibilityRules == null)
            {
                return;
            }
            for (int i = VisibilityRules.Count - 1; i >= 0; i--)
            {
                AdditionalAccessoryVisibilityRuleData data = VisibilityRules[i];
                if (data.Rule.Equals(rule))
                {
                    VisibilityRules.RemoveAt(i);
                }
            }

        }        

        public void SetVisibilityRule(AdditionalAccessoryVisibilityRules rule, AdditionalAccessoryVisibilityRulesModifiers mod, bool enabled)
        {
            if (VisibilityRules == null)
            {
                VisibilityRules = new List<AdditionalAccessoryVisibilityRuleData>();
            }

            AdditionalAccessoryVisibilityRuleData foundRule = FindVisibilityRule(rule, mod);
            if (!enabled && foundRule != null)
            {
                VisibilityRules.Remove(foundRule);
            }
            else if (enabled)
            {
                VisibilityRules.Add(new AdditionalAccessoryVisibilityRuleData(rule, ((int)mod).ToString()));
            }
        }

        public void SetVisibilityRule(AdditionalAccessoryVisibilityRules rule, string mod, bool enabled)
        {
            if (VisibilityRules == null)
            {
                VisibilityRules = new List<AdditionalAccessoryVisibilityRuleData>();
            }

            AdditionalAccessoryVisibilityRuleData foundRule = FindVisibilityRule(rule);
            if (!enabled && foundRule != null)
            {
                VisibilityRules.Remove(foundRule);
            }
            else if (enabled)
            {
                VisibilityRules.Add(new AdditionalAccessoryVisibilityRuleData(rule, mod));
            }
        }

        public AdditionalAccessoryVisibilityRuleData FindVisibilityRule(AdditionalAccessoryVisibilityRules checkRule, AdditionalAccessoryVisibilityRulesModifiers checkMod)
        {
            if (VisibilityRules == null)
            {
                return null;
            }

            foreach (AdditionalAccessoryVisibilityRuleData rule in VisibilityRules)
            {
                AdditionalAccessoryVisibilityRulesModifiers modifier;
                bool isEnumModifier = Enum.TryParse(rule.Modifier, out modifier);

                if (rule.Rule.Equals(checkRule) && (isEnumModifier && modifier.Equals(checkMod)))
                {
                    return rule;
                }
            }
            return null;
        }

        public AdditionalAccessoryVisibilityRuleData FindVisibilityRule(AdditionalAccessoryVisibilityRules checkRule)
        {
            if (VisibilityRules == null)
            {
                return null;
            }

            foreach (AdditionalAccessoryVisibilityRuleData rule in VisibilityRules)
            {
                if (rule.Rule.Equals(checkRule))
                {
                    return rule;
                }
            }
            return null;
        }

        public List<AdditionalAccessoryVisibilityRuleData> FindAllVisibilityRules(AdditionalAccessoryVisibilityRules checkRule)
        {
            List<AdditionalAccessoryVisibilityRuleData> matchRules = new List<AdditionalAccessoryVisibilityRuleData>();
            if (VisibilityRules == null)
            {
                return matchRules;
            }
            
            foreach (AdditionalAccessoryVisibilityRuleData rule in VisibilityRules)
            {
                if (rule.Rule.Equals(checkRule))
                {
                    matchRules.Add(rule);
                }
            }
            return matchRules;
        }


        public bool ContainsVisibilityRule(AdditionalAccessoryVisibilityRules checkRule, AdditionalAccessoryVisibilityRulesModifiers mod)
        {
            if (VisibilityRules == null)
            {
                return false;
            }

            foreach (AdditionalAccessoryVisibilityRuleData rule in VisibilityRules)
            {
                AdditionalAccessoryVisibilityRulesModifiers modifier;
                bool isEnumModifier = Enum.TryParse(rule.Modifier, out modifier);

                if (rule.Rule.Equals(checkRule) && (isEnumModifier && modifier.Equals(mod)))
                {                    
                    return true;
                }
            }
            return false;
        }

        public bool ContainsVisibilityRule(AdditionalAccessoryVisibilityRules checkRule)
        {
            if (VisibilityRules == null)
            {
                return false;
            }

            foreach (AdditionalAccessoryVisibilityRuleData rule in VisibilityRules)
            {
                if (rule.Rule.Equals(checkRule))
                {
                    return true;
                }
            }
            return false;
        }

        public static AdditionalAccessorySlotData Copy(AdditionalAccessorySlotData source, int newSlotNumber, bool noteOriginalSource = false)
        {
            AdditionalAccessorySlotData copy = new AdditionalAccessorySlotData();

            copy.IsEmpty = source.IsEmpty;
            copy.SlotNumber = newSlotNumber;
            copy.OriginalSlotNumber = noteOriginalSource ? source.SlotNumber : source.OriginalSlotNumber;
            copy.CharacterAccessory = source.CharacterAccessory;
            copy.AccessoryName = source.AccessoryName;
            copy.PartsInfo = source.PartsInfo;
            copy.AdvancedParent = source.AdvancedParent;
            copy.AutoMatchBackHairColor = source.AutoMatchBackHairColor;
            copy.VisibilityRules = new List<AdditionalAccessoryVisibilityRuleData>();
            if (source.VisibilityRules != null)
            {
                foreach (AdditionalAccessoryVisibilityRuleData data in source.VisibilityRules)
                {
                    copy.VisibilityRules.Add(data.Copy());
                }
            }
            return copy;
        }

        public void MakeEmpty()
        {
            OriginalSlotNumber = -1;
            AccessoryName = "Empty";
            IsEmpty = true;
            CharacterAccessory = false;
            PartsInfo = null;
            AdvancedParent = null;
            AutoMatchBackHairColor = false;
            VisibilityRules = new List<AdditionalAccessoryVisibilityRuleData>();
        }

        public static AdditionalAccessorySlotData EmptySlot(int slotNumber)
        {
            AdditionalAccessorySlotData slot = new AdditionalAccessorySlotData();
            slot.SlotNumber = slotNumber;
            slot.OriginalSlotNumber = -1;
            slot.AccessoryName = "Empty";
            slot.IsEmpty = true;
            slot.CharacterAccessory = false;
            slot.PartsInfo = null;
            slot.AdvancedParent = null;
            slot.AutoMatchBackHairColor = false;
            slot.VisibilityRules = new List<AdditionalAccessoryVisibilityRuleData>();
            return slot;
        }

        public static AdditionalAccessorySlotData NonCharacterAccessorySlot(int slotNumber, string name, ChaFileAccessory.PartsInfo partsInfo)
        {
            AdditionalAccessorySlotData slot = new AdditionalAccessorySlotData();
            slot.SlotNumber = slotNumber;
            slot.OriginalSlotNumber = -1;
            slot.AccessoryName = name;
            slot.IsEmpty = false;
            slot.CharacterAccessory = false;
            slot.PartsInfo = partsInfo;
            slot.AdvancedParent = null;
            slot.AutoMatchBackHairColor = false;
            slot.VisibilityRules = new List<AdditionalAccessoryVisibilityRuleData>();
            return slot;
        }

        public static AdditionalAccessorySlotData CharacterAccessorySlot(int slotNumber, string name, ChaFileAccessory.PartsInfo partsInfo)
        {
            AdditionalAccessorySlotData slot = new AdditionalAccessorySlotData();
            slot.SlotNumber = slotNumber;
            slot.OriginalSlotNumber = -1;
            slot.AccessoryName = name;
            slot.IsEmpty = false;
            slot.CharacterAccessory = true;
            slot.PartsInfo = partsInfo;
            slot.AdvancedParent = null;
            slot.AutoMatchBackHairColor = false;
            slot.VisibilityRules = new List<AdditionalAccessoryVisibilityRuleData>();
            return slot;
        }

    }
}
