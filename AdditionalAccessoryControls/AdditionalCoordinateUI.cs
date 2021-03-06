using AIChara;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static AdditionalAccessoryControls.AdditionalAccessoryCoordinateData;

namespace AdditionalAccessoryControls
{
    public class AdditionalCoordinateUI : MonoBehaviour
    {

        private static ManualLogSource Log => AdditionalAccessoryControlsPlugin.Instance.Log;

        private static Rect windowRect = new Rect(120, 220, 705, 740);
        private static readonly GUILayoutOption expandLayoutOption = GUILayout.ExpandWidth(true);

        private static GUIStyle labelStyle;
        private static GUIStyle selectedButtonStyle;

        private static bool guiLoaded = false;

        private Vector2 scrollPosition = Vector2.zero;

        private AdditionalAccessoryControlsController Controller { get => KKAPI.Maker.MakerAPI.InsideAndLoaded ? KKAPI.Maker.MakerAPI.GetCharacterControl().gameObject.GetComponent<AdditionalAccessoryControlsController>() : null; }

        public static AdditionalCoordinateUI Instance;

        public static void Show()
        {
#if DEBUG
            Log.LogInfo($"Showing Coordinate UI For: {KKAPI.Maker.MakerAPI.GetCharacterControl()?.fileParam?.fullname}");
#endif

            Instance.enabled = true;
        }

        public static void Hide()
        {
#if DEBUG
            Log.LogInfo("Hide Coord UI");
#endif
            Instance.enabled = false;
        }

        private void Awake()
        {
            Instance = this;
            enabled = false;
#if DEBUG
            Log.LogInfo("Coord UI AWAKE");
#endif
        }

        private void Update()
        {

        }

        private void OnEnable()
        {

        }

        private void OnDestroy()
        {
        }

        private void OnGUI()
        {
            if (!guiLoaded)
            {
                labelStyle = new GUIStyle(UnityEngine.GUI.skin.label);
                selectedButtonStyle = new GUIStyle(UnityEngine.GUI.skin.button);

                selectedButtonStyle.fontStyle = FontStyle.Bold;
                selectedButtonStyle.normal.textColor = Color.red;

                labelStyle.alignment = TextAnchor.MiddleRight;
                labelStyle.normal.textColor = Color.white;

                windowRect.x = Mathf.Min(Screen.width - windowRect.width, Mathf.Max(0, windowRect.x));
                windowRect.y = Mathf.Min(Screen.height - windowRect.height, Mathf.Max(0, windowRect.y));

                guiLoaded = true;

#if DEBUG
                Log.LogInfo("GUI Loaded");
#endif
            }

            KKAPI.Utilities.IMGUIUtils.DrawSolidBox(windowRect);


            var rect = GUILayout.Window(8725, windowRect, DoDraw, $"Visibility Rules for Slot: {Controller.ChaControl?.fileParam?.fullname}");
            windowRect.x = rect.x;
            windowRect.y = rect.y;

            if (windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();

        }

        private void SendRulesUpdateNotification()
        {
#if DEBUG
            Log.LogInfo($"Coordinate Overrides: {Controller.CoordinateOverrideData}");
#endif
            Controller.HandleVisibilityRules(clothes: true);
        }

        private void DoDraw(int id)
        {
            GUILayout.BeginVertical();
            {
                UnityEngine.GUI.changed = true;

                // Render Options

                // Slot Suppression Options
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                {
                    GUILayout.BeginHorizontal(expandLayoutOption);
                    GUILayout.Label("Suppress Slot States - Any slot states selected will not show/hide accessories", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    bool topAll = GUILayout.Button("TOP ALL", UnityEngine.GUI.skin.button);
                    if (topAll && !(TOP_ON && TOP_HALF && TOP_OFF))
                    {
                        TOP_ON = true;
                        TOP_HALF = true;
                        TOP_OFF = true;
                    }
                    else if (topAll)
                    {
                        TOP_ON = false;
                        TOP_HALF = false;
                        TOP_OFF = false;
                    }
                    TOP_ON = GUILayout.Toggle(TOP_ON, "TOP ON");
                    TOP_HALF = GUILayout.Toggle(TOP_HALF, "TOP HALF");
                    TOP_OFF = GUILayout.Toggle(TOP_OFF, "TOP OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    bool botALL = GUILayout.Button("BOT ALL", UnityEngine.GUI.skin.button);
                    if (botALL && !(BOT_ON && BOT_HALF && BOT_OFF))
                    {
                        BOT_ON = true;
                        BOT_HALF = true;
                        BOT_OFF = true;
                    }
                    else if (botALL)
                    {
                        BOT_ON = false;
                        BOT_HALF = false;
                        BOT_OFF = false;
                    }
                    BOT_ON = GUILayout.Toggle(BOT_ON, "BOT ON");
                    BOT_HALF = GUILayout.Toggle(BOT_HALF, "BOT HALF");
                    BOT_OFF = GUILayout.Toggle(BOT_OFF, "BOT OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    bool innerTopAll = GUILayout.Button("INNER TOP ALL", UnityEngine.GUI.skin.button);
                    if (innerTopAll && !(INNER_TOP_ON && INNER_TOP_HALF && INNER_TOP_OFF))
                    {
                        INNER_TOP_ON = true;
                        INNER_TOP_HALF = true;
                        INNER_TOP_OFF = true;
                    }
                    else if (innerTopAll)
                    {
                        INNER_TOP_ON = false;
                        INNER_TOP_HALF = false;
                        INNER_TOP_OFF = false;
                    }
                    INNER_TOP_ON = GUILayout.Toggle(INNER_TOP_ON, "INNER TOP ON");
                    INNER_TOP_HALF = GUILayout.Toggle(INNER_TOP_HALF, "INNER TOP HALF");
                    INNER_TOP_OFF = GUILayout.Toggle(INNER_TOP_OFF, "INNER TOP OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    bool innerBotAll = GUILayout.Button("INNER BOT ALL", UnityEngine.GUI.skin.button);
                    if (innerBotAll && !(INNER_BOT_ON && INNER_BOT_HALF && INNER_BOT_OFF))
                    {
                        INNER_BOT_ON = true;
                        INNER_BOT_HALF = true;
                        INNER_BOT_OFF = true;
                    }
                    else if (innerBotAll)
                    {
                        INNER_BOT_ON = false;
                        INNER_BOT_HALF = false;
                        INNER_BOT_OFF = false;
                    }
                    INNER_BOT_ON = GUILayout.Toggle(INNER_BOT_ON, "INNER BOT ON");
                    INNER_BOT_HALF = GUILayout.Toggle(INNER_BOT_HALF, "INNER BOT HALF");
                    INNER_BOT_OFF = GUILayout.Toggle(INNER_BOT_OFF, "INNER BOT OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    bool pantyhoseAll = GUILayout.Button("PANTYHOSE ALL", UnityEngine.GUI.skin.button);
                    if (pantyhoseAll && !(PANTYHOSE_ON && PANTYHOSE_HALF && PANTYHOSE_OFF))
                    {
                        PANTYHOSE_ON = true;
                        PANTYHOSE_HALF = true;
                        PANTYHOSE_OFF = true;
                    }
                    else if (pantyhoseAll)
                    {
                        PANTYHOSE_ON = false;
                        PANTYHOSE_HALF = false;
                        PANTYHOSE_OFF = false;
                    }
                    PANTYHOSE_ON = GUILayout.Toggle(PANTYHOSE_ON, "PANTYHOSE ON");
                    PANTYHOSE_HALF = GUILayout.Toggle(PANTYHOSE_HALF, "PANTYHOSE HALF");
                    PANTYHOSE_OFF = GUILayout.Toggle(PANTYHOSE_OFF, "PANTYHOSE OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);


                    bool gloveAll = GUILayout.Button("GLOVES ALL", UnityEngine.GUI.skin.button);
                    if (gloveAll && !(GLOVE_ON && GLOVE_OFF))
                    {
                        GLOVE_ON = true;
                        GLOVE_OFF = true;
                    }
                    else if (gloveAll)
                    {
                        GLOVE_ON = false;
                        GLOVE_OFF = false;
                    }
                    GLOVE_ON = GUILayout.Toggle(GLOVE_ON, "GLOVES ON");
                    GLOVE_OFF = GUILayout.Toggle(GLOVE_OFF, "GLOVES OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    bool sockAll = GUILayout.Button("SOCKS ALL", UnityEngine.GUI.skin.button);
                    if (sockAll && !(SOCK_ON && SOCK_OFF))
                    {
                        SOCK_ON = true;
                        SOCK_OFF = true;
                    }
                    else if (sockAll)
                    {
                        SOCK_ON = false;
                        SOCK_OFF = false;
                    }
                    SOCK_ON = GUILayout.Toggle(SOCK_ON, "SOCKS ON");
                    SOCK_OFF = GUILayout.Toggle(SOCK_OFF, "SOCKS OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    bool shoeAll = GUILayout.Button("SHOES ALL", UnityEngine.GUI.skin.button);
                    if (shoeAll && !(SHOE_ON && SHOE_OFF))
                    {
                        SHOE_ON = true;
                        SHOE_OFF = true;
                    }
                    else if (shoeAll)
                    {
                        SHOE_ON = false;
                        SHOE_OFF = false;
                    }
                    SHOE_ON = GUILayout.Toggle(SHOE_ON, "SHOES ON");
                    SHOE_OFF = GUILayout.Toggle(SHOE_OFF, "SHOES OFF");


                    GUILayout.EndHorizontal();

                }
                GUILayout.EndVertical();

            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
            {
                GUILayout.BeginHorizontal(expandLayoutOption);
                GUILayout.Label("Slot Overrides - Allows slots on this coordinate to count as other slots for purposes of accessory visibility rules.", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(expandLayoutOption);
                GUILayout.Label("All rules count as wildcards, they match any state. Overriding a slot to a different slot means it no longer counts as the original slot. If you want to it to still count as it's original slot, override each of the states of the slot to themselves.", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                GUILayout.Label("Active Override Rules", GUILayout.ExpandWidth(false));
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(600), GUILayout.Height(115));
                GUILayout.BeginVertical();
                for (int i = Controller.CoordinateOverrideData.OverrideRules.Count - 1; i >= 0; i--)
                {
                    AdditionalAccessoryCoordinateRuleData overrideRule = Controller.CoordinateOverrideData.OverrideRules[i];
                    GUILayout.BeginHorizontal(expandLayoutOption);
                    if (GUILayout.Button(" X ", UnityEngine.GUI.skin.button))
                    {
                        Controller.CoordinateOverrideData.ClearOverrideRule(overrideRule.Rule, overrideRule.RuleModifier, overrideRule.OverrideRule, overrideRule.OverrideRuleModifier);
                        SendRulesUpdateNotification();
                    }
                    GUILayout.Label($"Slot: {overrideRule.Rule} State: {Enum.Parse(typeof(AdditionalAccessoryVisibilityRulesModifiers), overrideRule.RuleModifier)} Counts as Slot: {overrideRule.OverrideRule} State: {Enum.Parse(typeof(AdditionalAccessoryVisibilityRulesModifiers), overrideRule.OverrideRuleModifier)}");
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.EndVertical();                

                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                GUILayout.Label("Create Override Rule", GUILayout.ExpandWidth(false));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(" Add ", UnityEngine.GUI.skin.button))
                {
                    if (OverrideRuleSource != AdditionalAccessoryVisibilityRules.NONE && OverrideRuleDest != AdditionalAccessoryVisibilityRules.NONE && OverrideRuleSourceMod != AdditionalAccessoryVisibilityRulesModifiers.NONE && OverrideRuleDestMod != AdditionalAccessoryVisibilityRulesModifiers.NONE)
                    {
                        Controller.CoordinateOverrideData.SetOverrideRule(OverrideRuleSource, OverrideRuleSourceMod, OverrideRuleDest, OverrideRuleDestMod);
                        SendRulesUpdateNotification();
                    }
                }
                GUILayout.Label("Slot: ");
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                if (GUILayout.Button("TOP", OverrideRuleSource == AdditionalAccessoryVisibilityRules.TOP ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSource = AdditionalAccessoryVisibilityRules.TOP;
                if (GUILayout.Button("BOT", OverrideRuleSource == AdditionalAccessoryVisibilityRules.BOT ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSource = AdditionalAccessoryVisibilityRules.BOT;
                if (GUILayout.Button("INNER TOP", OverrideRuleSource == AdditionalAccessoryVisibilityRules.INNER_TOP ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSource = AdditionalAccessoryVisibilityRules.INNER_TOP;
                if (GUILayout.Button("INNER BOT", OverrideRuleSource == AdditionalAccessoryVisibilityRules.INNER_BOT ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSource = AdditionalAccessoryVisibilityRules.INNER_BOT;
                if (GUILayout.Button("PANYTHOSE", OverrideRuleSource == AdditionalAccessoryVisibilityRules.PANTYHOSE ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSource = AdditionalAccessoryVisibilityRules.PANTYHOSE;
                if (GUILayout.Button("GLOVES", OverrideRuleSource == AdditionalAccessoryVisibilityRules.GLOVE ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSource = AdditionalAccessoryVisibilityRules.GLOVE;
                if (GUILayout.Button("SOCKS", OverrideRuleSource == AdditionalAccessoryVisibilityRules.SOCK ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSource = AdditionalAccessoryVisibilityRules.SOCK;
                if (GUILayout.Button("SHOES", OverrideRuleSource == AdditionalAccessoryVisibilityRules.SHOE ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSource = AdditionalAccessoryVisibilityRules.SHOE;
                GUILayout.EndVertical();
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                if (GUILayout.Button("ALL", OverrideRuleSourceMod == AdditionalAccessoryVisibilityRulesModifiers.ALL ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSourceMod = AdditionalAccessoryVisibilityRulesModifiers.ALL;
                if (GUILayout.Button("ON", OverrideRuleSourceMod == AdditionalAccessoryVisibilityRulesModifiers.ON ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSourceMod = AdditionalAccessoryVisibilityRulesModifiers.ON;
                if (!(OverrideRuleSource == AdditionalAccessoryVisibilityRules.GLOVE || OverrideRuleSource == AdditionalAccessoryVisibilityRules.SOCK || OverrideRuleSource == AdditionalAccessoryVisibilityRules.SHOE))
                    if (GUILayout.Button("HALF", OverrideRuleSourceMod == AdditionalAccessoryVisibilityRulesModifiers.HALF ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSourceMod = AdditionalAccessoryVisibilityRulesModifiers.HALF;
                if (GUILayout.Button("OFF", OverrideRuleSourceMod == AdditionalAccessoryVisibilityRulesModifiers.OFF ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleSourceMod = AdditionalAccessoryVisibilityRulesModifiers.OFF;
                GUILayout.EndVertical();
                GUILayout.Label("Count as: ");
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                if (GUILayout.Button("TOP", OverrideRuleDest == AdditionalAccessoryVisibilityRules.TOP ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDest = AdditionalAccessoryVisibilityRules.TOP;
                if (GUILayout.Button("BOT", OverrideRuleDest == AdditionalAccessoryVisibilityRules.BOT ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDest = AdditionalAccessoryVisibilityRules.BOT;
                if (GUILayout.Button("INNER TOP", OverrideRuleDest == AdditionalAccessoryVisibilityRules.INNER_TOP ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDest = AdditionalAccessoryVisibilityRules.INNER_TOP;
                if (GUILayout.Button("INNER BOT", OverrideRuleDest == AdditionalAccessoryVisibilityRules.INNER_BOT ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDest = AdditionalAccessoryVisibilityRules.INNER_BOT;
                if (GUILayout.Button("PANYTHOSE", OverrideRuleDest == AdditionalAccessoryVisibilityRules.PANTYHOSE ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDest = AdditionalAccessoryVisibilityRules.PANTYHOSE;
                if (GUILayout.Button("GLOVES", OverrideRuleDest == AdditionalAccessoryVisibilityRules.GLOVE ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDest = AdditionalAccessoryVisibilityRules.GLOVE;
                if (GUILayout.Button("SOCKS", OverrideRuleDest == AdditionalAccessoryVisibilityRules.SOCK ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDest = AdditionalAccessoryVisibilityRules.SOCK;
                if (GUILayout.Button("SHOES", OverrideRuleDest == AdditionalAccessoryVisibilityRules.SHOE ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDest = AdditionalAccessoryVisibilityRules.SHOE;
                GUILayout.EndVertical();
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                if (GUILayout.Button("ALL", OverrideRuleDestMod == AdditionalAccessoryVisibilityRulesModifiers.ALL ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDestMod = AdditionalAccessoryVisibilityRulesModifiers.ALL;
                if (GUILayout.Button("ON", OverrideRuleDestMod == AdditionalAccessoryVisibilityRulesModifiers.ON ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDestMod = AdditionalAccessoryVisibilityRulesModifiers.ON;
                if (!(OverrideRuleDest == AdditionalAccessoryVisibilityRules.GLOVE || OverrideRuleDest == AdditionalAccessoryVisibilityRules.SOCK || OverrideRuleDest == AdditionalAccessoryVisibilityRules.SHOE))
                    if (GUILayout.Button("HALF", OverrideRuleDestMod == AdditionalAccessoryVisibilityRulesModifiers.HALF ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDestMod = AdditionalAccessoryVisibilityRulesModifiers.HALF;
                if (GUILayout.Button("OFF", OverrideRuleDestMod == AdditionalAccessoryVisibilityRulesModifiers.OFF ? selectedButtonStyle : UnityEngine.GUI.skin.button)) OverrideRuleDestMod = AdditionalAccessoryVisibilityRulesModifiers.OFF;
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                if (OverrideRuleSourceMod == AdditionalAccessoryVisibilityRulesModifiers.HALF && (OverrideRuleSource == AdditionalAccessoryVisibilityRules.GLOVE || OverrideRuleSource == AdditionalAccessoryVisibilityRules.SOCK || OverrideRuleSource == AdditionalAccessoryVisibilityRules.SHOE))
                    OverrideRuleSourceMod = AdditionalAccessoryVisibilityRulesModifiers.OFF;
                if (OverrideRuleDestMod == AdditionalAccessoryVisibilityRulesModifiers.HALF && (OverrideRuleDest == AdditionalAccessoryVisibilityRules.GLOVE || OverrideRuleDest == AdditionalAccessoryVisibilityRules.SOCK || OverrideRuleDest == AdditionalAccessoryVisibilityRules.SHOE))
                    OverrideRuleDestMod = AdditionalAccessoryVisibilityRulesModifiers.OFF;

            }
            GUILayout.EndVertical();

            UnityEngine.GUI.DragWindow();
        }

        public AdditionalAccessoryVisibilityRules OverrideRuleSource { get; set; }
        public AdditionalAccessoryVisibilityRulesModifiers OverrideRuleSourceMod { get; set; }
        public AdditionalAccessoryVisibilityRules OverrideRuleDest { get; set; }
        public AdditionalAccessoryVisibilityRulesModifiers OverrideRuleDestMod { get; set; }


        // Rule Links
        public bool TOP_ON
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.ON))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool TOP_HALF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool TOP_OFF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool BOT_ON
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.ON))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool BOT_HALF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool BOT_OFF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool INNER_TOP_ON
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.ON))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool INNER_TOP_HALF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool INNER_TOP_OFF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool INNER_BOT_ON
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.ON))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool INNER_BOT_HALF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool INNER_BOT_OFF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool PANTYHOSE_ON
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.ON))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool PANTYHOSE_HALF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.HALF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.HALF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.HALF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool PANTYHOSE_OFF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool GLOVE_ON
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.ON))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool GLOVE_OFF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool SOCK_ON
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.ON))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool SOCK_OFF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool SHOE_ON
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.ON))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool SHOE_OFF
        {
            get => Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != Controller.CoordinateOverrideData.IsSuppressed(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                {
                    Controller.CoordinateOverrideData.SetSuppressionRule(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
    }
}