using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdditionalAccessoryControls
{
    [Serializable]
    [MessagePackObject]
    public class AdditionalAccessoryCoordinateData
    {
        [Key(0)]
        public List<AdditionalAccessoryCoordinateRuleData> SuppressedRules { get; set; }
        [Key(1)]
        public List<AdditionalAccessoryCoordinateRuleData> OverrideRules { get; set; }

        public AdditionalAccessoryCoordinateData()
        {
            SuppressedRules = new List<AdditionalAccessoryCoordinateRuleData>();
            OverrideRules = new List<AdditionalAccessoryCoordinateRuleData>();
        }

        public void SetSuppressionRule(AdditionalAccessoryVisibilityRules rule, AdditionalAccessoryVisibilityRulesModifiers modifier, bool value)
        {
            if (value)
            {
                foreach (AdditionalAccessoryCoordinateRuleData suppressionRule in SuppressedRules)
                {
                    if (suppressionRule.Rule == rule && String.Equals(((int)modifier).ToString(), suppressionRule.RuleModifier))
                    {
                        return;
                    }
                }
                SuppressedRules.Add(new AdditionalAccessoryCoordinateRuleData(rule, ((int)modifier).ToString(), AdditionalAccessoryVisibilityRules.NONE, null));
            }
            else
            {
                ClearSuppressionRule(rule, modifier);
            }
        }

        public void ClearSuppressionRule(AdditionalAccessoryVisibilityRules rule, AdditionalAccessoryVisibilityRulesModifiers modifier)
        {
            for (int i = SuppressedRules.Count - 1; i >= 0; i--)
            {
                if (SuppressedRules[i].Rule == rule && String.Equals(((int)modifier).ToString(), SuppressedRules[i].RuleModifier))
                {
                    SuppressedRules.RemoveAt(i);
                }
            }
        }

        public void SetOverrideRule(AdditionalAccessoryVisibilityRules rule, AdditionalAccessoryVisibilityRulesModifiers modifier, AdditionalAccessoryVisibilityRules overrideRule, AdditionalAccessoryVisibilityRulesModifiers overrideModifier)
        {
            foreach (AdditionalAccessoryCoordinateRuleData oRule in OverrideRules)
            {
                if (oRule.Rule == rule && String.Equals(((int)modifier).ToString(), oRule.RuleModifier) && oRule.OverrideRule == overrideRule && String.Equals(((int)overrideModifier).ToString(), oRule.OverrideRuleModifier))
                {
                    return;
                }
            }
            OverrideRules.Add(new AdditionalAccessoryCoordinateRuleData(rule, ((int)modifier).ToString(), overrideRule, ((int)overrideModifier).ToString()));
        }

        public void ClearOverrideRule(AdditionalAccessoryVisibilityRules rule, AdditionalAccessoryVisibilityRulesModifiers modifier, AdditionalAccessoryVisibilityRules overrideRule, AdditionalAccessoryVisibilityRulesModifiers overrideModifier)
        {
            for (int i = OverrideRules.Count - 1; i >= 0; i--)
            {
                if (OverrideRules[i].Rule == rule && String.Equals(((int)modifier).ToString(), OverrideRules[i].RuleModifier) && OverrideRules[i].OverrideRule == overrideRule && String.Equals(((int)overrideModifier).ToString(), OverrideRules[i].OverrideRuleModifier))
                {
                    OverrideRules.RemoveAt(i);
                }
            }
        }

        public void ClearOverrideRule(AdditionalAccessoryVisibilityRules rule, string modifier, AdditionalAccessoryVisibilityRules overrideRule, string overrideModifier)
        {
            for (int i = OverrideRules.Count - 1; i >= 0; i--)
            {
                if (OverrideRules[i].Rule == rule && String.Equals(modifier, OverrideRules[i].RuleModifier) && OverrideRules[i].OverrideRule == overrideRule && String.Equals(overrideModifier, OverrideRules[i].OverrideRuleModifier))
                {
                    OverrideRules.RemoveAt(i);
                }
            }
        }

        public bool IsSuppressed(AdditionalAccessoryVisibilityRules rule)
        {
            foreach (AdditionalAccessoryCoordinateRuleData suppressionRule in SuppressedRules)
            {
                if (suppressionRule.Rule == rule)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsSuppressed(AdditionalAccessoryVisibilityRules rule, string modifier)
        {
            foreach (AdditionalAccessoryCoordinateRuleData suppressionRule in SuppressedRules)
            {
                if (suppressionRule.Rule == rule && String.Equals(modifier, suppressionRule.RuleModifier))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsSuppressed(AdditionalAccessoryVisibilityRules rule, AdditionalAccessoryVisibilityRulesModifiers modifier)
        {
            return IsSuppressed(rule, ((int)modifier).ToString());
        }

        public AdditionalAccessoryCoordinateRuleData GetOverride(AdditionalAccessoryVisibilityRules rule)
        {
            foreach (AdditionalAccessoryCoordinateRuleData overrideRule in OverrideRules)
            {
                if (overrideRule.Rule == rule)
                {
                    return overrideRule;
                }
            }
            return null;
        }

        public bool IsOverrideSource(AdditionalAccessoryVisibilityRules rule, string modifier)
        {
            foreach (AdditionalAccessoryCoordinateRuleData overrideRule in OverrideRules)
            {
                if (overrideRule.Rule == rule && (String.Equals(modifier, overrideRule.RuleModifier) || String.Equals(overrideRule.RuleModifier, ((int)AdditionalAccessoryVisibilityRulesModifiers.ALL).ToString())))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsOverrideSource(AdditionalAccessoryVisibilityRules rule, AdditionalAccessoryVisibilityRulesModifiers modifier)
        {
            return IsOverrideSource(rule, ((int)modifier).ToString());
        }

        public List<AdditionalAccessoryCoordinateRuleData> GetOverrideTargets(AdditionalAccessoryVisibilityRules rule, string modifier)
        {
            return OverrideRules.Where(or => or.OverrideRule == rule && String.Equals(modifier, or.OverrideRuleModifier)).ToList();
        }

        public List<AdditionalAccessoryCoordinateRuleData> GetOverrideTargets(AdditionalAccessoryVisibilityRules rule, AdditionalAccessoryVisibilityRulesModifiers modifier)
        {
            return GetOverrideTargets(rule, ((int)modifier).ToString());
        }

        public override string ToString()
        {
            return $"Suppressed: {String.Join(",", SuppressedRules)} Overrides: {String.Join(",", OverrideRules)}";
        }

        public static AdditionalAccessoryCoordinateData Copy(AdditionalAccessoryCoordinateData source)
        {
            AdditionalAccessoryCoordinateData copy = new AdditionalAccessoryCoordinateData();
            foreach (AdditionalAccessoryCoordinateRuleData rule in source.SuppressedRules)
            {
                copy.SuppressedRules.Add(rule.Copy());
            }
            foreach (AdditionalAccessoryCoordinateRuleData overrideRule in source.OverrideRules)
            {
                copy.OverrideRules.Add(overrideRule);
            }
            return copy;
        }

        [Serializable]
        [MessagePackObject]
        public class AdditionalAccessoryCoordinateRuleData
        {
            [Key(0)]
            public AdditionalAccessoryVisibilityRules Rule { get; set; }
            [Key(1)]
            public string RuleModifier { get; set; }
            [Key(2)]
            public AdditionalAccessoryVisibilityRules OverrideRule { get; set; }
            [Key(3)]
            public string OverrideRuleModifier { get; set; }

            public AdditionalAccessoryCoordinateRuleData()
            {

            }

            public AdditionalAccessoryCoordinateRuleData(AdditionalAccessoryVisibilityRules rule, string ruleModifier, AdditionalAccessoryVisibilityRules overrideRule, string overrideRuleModifier)
            {
                Rule = rule;
                RuleModifier = ruleModifier;
                OverrideRule = overrideRule;
                OverrideRuleModifier = overrideRuleModifier;
            }
            public override string ToString()
            {
                return $"Rule: {Rule} Mod: {RuleModifier} Over: {OverrideRule} OverMod: {OverrideRuleModifier}";
            }

            public bool IsNoneRule()
            {
                return Rule == AdditionalAccessoryVisibilityRules.NONE && String.Equals(RuleModifier, ((int)AdditionalAccessoryVisibilityRulesModifiers.NONE).ToString())
                    && OverrideRule == AdditionalAccessoryVisibilityRules.NONE && String.Equals(OverrideRuleModifier, ((int)AdditionalAccessoryVisibilityRulesModifiers.NONE).ToString());
            }

            public AdditionalAccessoryCoordinateRuleData Copy()
            {
                AdditionalAccessoryCoordinateRuleData copy = new AdditionalAccessoryCoordinateRuleData();
                copy.Rule = this.Rule;
                copy.RuleModifier = this.RuleModifier;
                copy.OverrideRule = this.OverrideRule;
                copy.OverrideRuleModifier = this.OverrideRuleModifier;
                return copy;
            }

        }
    }
}
