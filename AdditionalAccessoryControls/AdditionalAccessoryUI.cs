using AIChara;
using BepInEx.Logging;
using KKAPI.Maker;
using System.Linq;
using UnityEngine;
using static AdditionalAccessoryControls.AdditionalAccessorySlotData;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryUI : MonoBehaviour
    {

        private static ManualLogSource Log => AdditionalAccessoryControlsPlugin.Instance.Log;

        private static Rect windowRect = new Rect(120, 220, 705, 700);
        private static readonly GUILayoutOption expandLayoutOption = GUILayout.ExpandWidth(true);        

        private static GUIStyle labelStyle;
        private static GUIStyle selectedButtonStyle;

        private static bool guiLoaded = false;

        private Vector2 scrollPosition = Vector2.zero;

        private AdditionalAccessorySlotData CurrentSlot { get; set; }
        private AdditionalAccessoryControlsController Controller { get; set; }

        public static AdditionalAccessoryUI Instance;

        public static void Show(AdditionalAccessorySlotData slot, ChaControl chaControl)
        {
#if DEBUG
            Log.LogInfo($"Showing UI For: {slot} on {chaControl?.fileParam?.fullname}");
#endif

            Instance.enabled = true;
            Change(slot, chaControl);
                       
        }

        public static void Change(AdditionalAccessorySlotData slot, ChaControl chaControl)
        {
#if DEBUG
            Log.LogInfo($"Changing UI For: {slot} on {chaControl?.fileParam?.fullname}");
#endif

            Instance.Controller = chaControl.gameObject.GetComponent<AdditionalAccessoryControlsController>();
            Instance.CurrentSlot = slot;

            string[] availableSlots = Instance.AvailableSlots();

            AdditionalAccessoryVisibilityRuleData rule = slot.FindVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK);
            if (rule == null)
                rule = slot.FindVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK);

            if (rule != null)
            {
                if (!availableSlots.Contains(rule.Modifier))
                {
                    slot.ClearVisibilityRule(rule.Rule);
                    Instance.accessorySlotString = availableSlots.Length > 0 ? availableSlots[0] : "0";
                }
                else
                {
                    Instance.accessorySlotString = rule.Modifier;
                }
            }
            else
            {
                Instance.accessorySlotString = availableSlots.Length > 0 ? availableSlots[0] : "0";
            }
        }

        public static void Hide()
        {
#if DEBUG
            Log.LogInfo("Hide UI");
#endif
            Instance.enabled = false;
            Instance.CurrentSlot = null;
            Instance._accessorySlotString = "0";
        }

        private void Awake()
        {            
            Instance = this;
            enabled = false;
#if DEBUG
            Log.LogInfo("UI AWAKE");
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
            Controller = null;
            CurrentSlot = null;
        }

        private string[] AvailableSlots()
        {
            AdditionalAccessoryControlsController aacController = Controller.gameObject.GetComponent<AdditionalAccessoryControlsController>();
            return aacController.SlotData.Where<AdditionalAccessorySlotData>(slot => !slot.IsEmpty && slot.SlotNumber != CurrentSlot.SlotNumber).Select<AdditionalAccessorySlotData, string>(slot => (slot.SlotNumber + 1).ToString()).ToArray();
        }

        private void OnGUI()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

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


            var rect = GUILayout.Window(8724, windowRect, DoDraw, $"Visibility Rules for Slot: {(CurrentSlot.SlotNumber + 1)} {CurrentSlot.AccessoryName}");
            windowRect.x = rect.x;
            windowRect.y = rect.y;

            if (windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();

        }

        private void SendRulesUpdateNotification()
        {
            Controller.HandleVisibilityRulesForSlot(CurrentSlot, ruleUpdate: true);
        }

        private void DoDraw(int id)
        {
            GUILayout.BeginVertical();
            {
                UnityEngine.GUI.changed = true;

                // Header
                GUILayout.BeginHorizontal(expandLayoutOption);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close Me", GUILayout.ExpandWidth(false))) enabled = false;
                GUILayout.EndHorizontal();

                // Render Options

                // Clothing Slot Options
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                {
                    GUILayout.BeginHorizontal(expandLayoutOption);
                    GUILayout.Label("Hide When ANY of the Following Clothing Slot States are Set", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    TOP_ON = GUILayout.Toggle(TOP_ON, "TOP ON");
                    TOP_HALF = GUILayout.Toggle(TOP_HALF, "TOP HALF");
                    TOP_OFF = GUILayout.Toggle(TOP_OFF, "TOP OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    BOT_ON = GUILayout.Toggle(BOT_ON, "BOT ON");
                    BOT_HALF = GUILayout.Toggle(BOT_HALF, "BOT HALF");
                    BOT_OFF = GUILayout.Toggle(BOT_OFF, "BOT OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    INNER_TOP_ON = GUILayout.Toggle(INNER_TOP_ON, "INNER TOP ON");
                    INNER_TOP_HALF = GUILayout.Toggle(INNER_TOP_HALF, "INNER TOP HALF");
                    INNER_TOP_OFF = GUILayout.Toggle(INNER_TOP_OFF, "INNER TOP OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    INNER_BOT_ON = GUILayout.Toggle(INNER_BOT_ON, "INNER BOT ON");
                    INNER_BOT_HALF = GUILayout.Toggle(INNER_BOT_HALF, "INNER BOT HALF");
                    INNER_BOT_OFF = GUILayout.Toggle(INNER_BOT_OFF, "INNER BOT OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    PANTYHOSE_ON = GUILayout.Toggle(PANTYHOSE_ON, "PANTYHOSE ON");
                    PANTYHOSE_HALF = GUILayout.Toggle(PANTYHOSE_HALF, "PANTYHOSE HALF");
                    PANTYHOSE_OFF = GUILayout.Toggle(PANTYHOSE_OFF, "PANTYHOSE OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    GLOVE_ON = GUILayout.Toggle(GLOVE_ON, "GLOVES ON");
                    GLOVE_OFF = GUILayout.Toggle(GLOVE_OFF, "GLOVES OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    SOCK_ON = GUILayout.Toggle(SOCK_ON, "SOCKS ON");
                    SOCK_OFF = GUILayout.Toggle(SOCK_OFF, "SOCKS OFF");


                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    SHOE_ON = GUILayout.Toggle(SHOE_ON, "SHOES ON");
                    SHOE_OFF = GUILayout.Toggle(SHOE_OFF, "SHOES OFF");


                    GUILayout.EndHorizontal();

                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                {
                    GUILayout.BeginHorizontal(expandLayoutOption);
                    GUILayout.BeginVertical(GUILayout.ExpandHeight(false));

                    GUILayout.Label("Link to Status of Specified Slot # (Empty slots always counts as Not Visible\nLinks can be chained, limited to 20 deep. Bi-Directional Links don't function. Pick one relationship direction.)", GUILayout.ExpandWidth(false));
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();

                    GUILayout.BeginVertical(GUILayout.ExpandHeight(false));

                    LINK_ACCESSORY = GUILayout.Toggle(LINK_ACCESSORY, "Link");
                    INVERSE_LINK_ACCESSORY = GUILayout.Toggle(INVERSE_LINK_ACCESSORY, "Inverse Link");

                    GUILayout.EndVertical();

                    scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(500), GUILayout.Height(60));
                    GUILayout.BeginVertical();

                    string[] availableSlots = AvailableSlots();
                    for (int index = 0; index < availableSlots.Length; )
                    {
                        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                        for (int i = 0; i < 5 && index < availableSlots.Length; i++, index++)
                        {
                            bool selected = GUILayout.Button(availableSlots[index], (availableSlots[index].Equals(accessorySlotString)) ? selectedButtonStyle : UnityEngine.GUI.skin.button);
                            if (selected)
                            {
                                accessorySlotString = availableSlots[index];
                                if (CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK))
                                {
                                    CurrentSlot.ClearVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK);
                                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK, accessorySlotString, true);
                                    SendRulesUpdateNotification();
                                }
                                else if (CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK))
                                {
                                    CurrentSlot.ClearVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK);
                                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK, accessorySlotString, true);
                                    SendRulesUpdateNotification();
                                }
                            }                            
                        }
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();
                    GUILayout.EndScrollView();

                    GUILayout.EndHorizontal();


                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                {
                    GUILayout.BeginHorizontal(expandLayoutOption);
                    STUDIO_LOAD = GUILayout.Toggle(STUDIO_LOAD, "Apply Rules on Studio Scene Character/Outfit Change (Normally Skipped) - Initial Scene State is Preserved");
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                    GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                {
                    GUILayout.BeginHorizontal(expandLayoutOption);
                    GUILayout.Label("Hide or Show On Lifecycle Events", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);
                    GUILayout.Label("Startup is Onload - First entry into any scene. Studio is excluded (unless above option is selected).", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    HIDE_ON_STARTUP = GUILayout.Toggle(HIDE_ON_STARTUP, "Hide on Startup (Defaults to Hidden)");                    
                    GUILayout.EndHorizontal();


                    GUILayout.BeginHorizontal(expandLayoutOption);
                    GUILayout.Label("H Scene Start is after initial conversation, before first sex animation (use Startup for initial conversation).", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    HIDE_ON_HSTART = GUILayout.Toggle(HIDE_ON_HSTART, "Hide on H Scene Start");
                    SHOW_ON_HSTART = GUILayout.Toggle(SHOW_ON_HSTART, "Show on H Scene Start");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);
                    GUILayout.Label("H Scene End is before final conversation", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    HIDE_ON_HEND = GUILayout.Toggle(HIDE_ON_HEND, "Hide on H Scene End");
                    SHOW_ON_HEND = GUILayout.Toggle(SHOW_ON_HEND, "Show on H Scene End");
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                {
                    GUILayout.BeginHorizontal(expandLayoutOption);
                    GUILayout.BeginVertical(GUILayout.ExpandHeight(false));
                    GUILayout.Label("Hide Specified Hair/Body Part when this Accessorial is Visible\nNote: Hair is invisible but present. Body parts are scaled to 0.", GUILayout.ExpandWidth(false));
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    HIDE_BACK_HAIR = GUILayout.Toggle(HIDE_BACK_HAIR, "Hair Back");
                    HIDE_FRONT_HAIR = GUILayout.Toggle(HIDE_FRONT_HAIR, "Hair Front");
                    HIDE_SIDE_HAIR = GUILayout.Toggle(HIDE_SIDE_HAIR, "Hair Side");
                    HIDE_EXT_HAIR = GUILayout.Toggle(HIDE_EXT_HAIR, "Hair Extension");
                    HIDE_ACC_HAIR = GUILayout.Toggle(HIDE_ACC_HAIR, "Hair Accessories");

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);

                    HIDE_NOSE = GUILayout.Toggle(HIDE_NOSE, "Nose");
                    HIDE_LEFT_EAR = GUILayout.Toggle(HIDE_LEFT_EAR, "L Ear");
                    HIDE_RIGHT_EAR = GUILayout.Toggle(HIDE_RIGHT_EAR, "R Ear");
                    HIDE_LEFT_HAND = GUILayout.Toggle(HIDE_LEFT_HAND, "L Hand");
                    HIDE_RIGHT_HAND = GUILayout.Toggle(HIDE_RIGHT_HAND, "R Hand");
                    HIDE_LEFT_FOOT = GUILayout.Toggle(HIDE_LEFT_FOOT, "L Foot");
                    HIDE_RIGHT_FOOT = GUILayout.Toggle(HIDE_RIGHT_FOOT, "R Foot");
                    HIDE_LEFT_EYELASH = GUILayout.Toggle(HIDE_LEFT_EYELASH, "L Eyelash");
                    HIDE_RIGHT_EYELASH = GUILayout.Toggle(HIDE_RIGHT_EYELASH, "R Eyelash");

                    GUILayout.EndHorizontal();


                }
                GUILayout.EndVertical();

            }

            GUILayout.EndVertical();
            UnityEngine.GUI.DragWindow();
        }


        // Rule Links
        public bool TOP_ON
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.ON))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool TOP_HALF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool TOP_OFF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool BOT_ON
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.ON))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool BOT_HALF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool BOT_OFF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool INNER_TOP_ON
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.ON))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool INNER_TOP_HALF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.HALF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool INNER_TOP_OFF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_TOP, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool INNER_BOT_ON
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.ON))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool INNER_BOT_HALF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.HALF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool INNER_BOT_OFF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.INNER_BOT, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool PANTYHOSE_ON
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.ON))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool PANTYHOSE_HALF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.HALF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.HALF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.HALF, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool PANTYHOSE_OFF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.PANTYHOSE, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool GLOVE_ON
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.ON))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool GLOVE_OFF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.GLOVE, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool SOCK_ON
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.ON))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool SOCK_OFF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.SOCK, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool SHOE_ON
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.ON);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.ON))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.ON, value);
                    SendRulesUpdateNotification();
                }

            }
        }
        public bool SHOE_OFF
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.OFF);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.OFF))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.SHOE, AdditionalAccessoryVisibilityRulesModifiers.OFF, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool LINK_ACCESSORY
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK) && !_accessorySlotString.Equals("0"))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK, _accessorySlotString, value);
                    if (value)
                    {
                        CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK, _accessorySlotString, false);
                    }
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool INVERSE_LINK_ACCESSORY
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK) && !_accessorySlotString.Equals("0"))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_INVERSE_LINK, _accessorySlotString, value);
                    if (value)
                    {
                        CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.ACCESSORY_LINK, _accessorySlotString, false);
                    }
                    SendRulesUpdateNotification();
                }
            }
        }
        
        private string _accessorySlotString = "0";
        private string accessorySlotString
        {
            get
            {
                return _accessorySlotString;
            }
            set
            {
                if (value != _accessorySlotString)
                {
                    _accessorySlotString = value;
                }
            }
        }        

        public bool HIDE_ON_STARTUP
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.STARTUP, AdditionalAccessoryVisibilityRulesModifiers.HIDE);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.STARTUP, AdditionalAccessoryVisibilityRulesModifiers.HIDE))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.STARTUP, AdditionalAccessoryVisibilityRulesModifiers.HIDE, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool STUDIO_LOAD
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.STUDIO_LOAD, AdditionalAccessoryVisibilityRulesModifiers.NONE);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.STUDIO_LOAD, AdditionalAccessoryVisibilityRulesModifiers.NONE))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.STUDIO_LOAD, AdditionalAccessoryVisibilityRulesModifiers.NONE, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool HIDE_ON_HSTART
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_START, AdditionalAccessoryVisibilityRulesModifiers.HIDE);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_START, AdditionalAccessoryVisibilityRulesModifiers.HIDE))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.H_START, AdditionalAccessoryVisibilityRulesModifiers.HIDE, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool HIDE_ON_HEND
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_END, AdditionalAccessoryVisibilityRulesModifiers.HIDE);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_END, AdditionalAccessoryVisibilityRulesModifiers.HIDE))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.H_END, AdditionalAccessoryVisibilityRulesModifiers.HIDE, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool SHOW_ON_HSTART
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_START, AdditionalAccessoryVisibilityRulesModifiers.SHOW);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_START, AdditionalAccessoryVisibilityRulesModifiers.SHOW))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.H_START, AdditionalAccessoryVisibilityRulesModifiers.SHOW, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool SHOW_ON_HEND
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_END, AdditionalAccessoryVisibilityRulesModifiers.SHOW);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.H_END, AdditionalAccessoryVisibilityRulesModifiers.SHOW))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.H_END, AdditionalAccessoryVisibilityRulesModifiers.SHOW, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool HIDE_FRONT_HAIR
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_FRONT);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_FRONT))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_FRONT, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool HIDE_BACK_HAIR
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_BACK);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_BACK)) 
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_BACK, value);
                    SendRulesUpdateNotification();
                }

            }
        }

        public bool HIDE_SIDE_HAIR
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_SIDE);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_SIDE))
                { 
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_SIDE, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_EXT_HAIR
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_EXT);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_EXT))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_EXT, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_ACC_HAIR
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_ACC);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_ACC))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.HAIR, AdditionalAccessoryVisibilityRulesModifiers.HAIR_ACC, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_NOSE
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.NOSE, AdditionalAccessoryVisibilityRulesModifiers.NONE);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.NOSE, AdditionalAccessoryVisibilityRulesModifiers.NONE))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.NOSE, AdditionalAccessoryVisibilityRulesModifiers.NONE, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_LEFT_EAR
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.EAR, AdditionalAccessoryVisibilityRulesModifiers.LEFT);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.EAR, AdditionalAccessoryVisibilityRulesModifiers.LEFT))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.EAR, AdditionalAccessoryVisibilityRulesModifiers.LEFT, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_RIGHT_EAR
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.EAR, AdditionalAccessoryVisibilityRulesModifiers.RIGHT);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.EAR, AdditionalAccessoryVisibilityRulesModifiers.RIGHT))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.EAR, AdditionalAccessoryVisibilityRulesModifiers.RIGHT, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_LEFT_HAND
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAND, AdditionalAccessoryVisibilityRulesModifiers.LEFT);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAND, AdditionalAccessoryVisibilityRulesModifiers.LEFT))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.HAND, AdditionalAccessoryVisibilityRulesModifiers.LEFT, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_RIGHT_HAND
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAND, AdditionalAccessoryVisibilityRulesModifiers.RIGHT);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.HAND, AdditionalAccessoryVisibilityRulesModifiers.RIGHT))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.HAND, AdditionalAccessoryVisibilityRulesModifiers.RIGHT, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_LEFT_FOOT
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.FOOT, AdditionalAccessoryVisibilityRulesModifiers.LEFT);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.FOOT, AdditionalAccessoryVisibilityRulesModifiers.LEFT))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.FOOT, AdditionalAccessoryVisibilityRulesModifiers.LEFT, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_RIGHT_FOOT
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.FOOT, AdditionalAccessoryVisibilityRulesModifiers.RIGHT);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.FOOT, AdditionalAccessoryVisibilityRulesModifiers.RIGHT))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.FOOT, AdditionalAccessoryVisibilityRulesModifiers.RIGHT, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_LEFT_EYELASH
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.EYELASH, AdditionalAccessoryVisibilityRulesModifiers.LEFT);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.EYELASH, AdditionalAccessoryVisibilityRulesModifiers.LEFT))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.EYELASH, AdditionalAccessoryVisibilityRulesModifiers.LEFT, value);
                    SendRulesUpdateNotification();
                }
            }
        }

        public bool HIDE_RIGHT_EYELASH
        {
            get => CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.EYELASH, AdditionalAccessoryVisibilityRulesModifiers.RIGHT);
            set
            {
                if (value != CurrentSlot.ContainsVisibilityRule(AdditionalAccessoryVisibilityRules.EYELASH, AdditionalAccessoryVisibilityRulesModifiers.RIGHT))
                {
                    CurrentSlot.SetVisibilityRule(AdditionalAccessoryVisibilityRules.EYELASH, AdditionalAccessoryVisibilityRulesModifiers.RIGHT, value);
                    SendRulesUpdateNotification();
                }
            }
        }

    }



}
