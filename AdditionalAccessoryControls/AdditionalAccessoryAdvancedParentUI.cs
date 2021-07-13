using AIChara;
using BepInEx.Logging;
using KKAPI.Maker;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AdditionalAccessoryControls
{
    public class AdditionalAccessoryAdvancedParentUI : MonoBehaviour
    {
        private static ManualLogSource Log => AdditionalAccessoryControlsPlugin.Instance.Log;

        private static Rect windowRect = new Rect(120, 220, 705, 700);
        private static readonly GUILayoutOption expandLayoutOption = GUILayout.ExpandWidth(true);

        private static GUIStyle labelStyle;
        private static GUIStyle selectedButtonStyle;

        private static bool guiLoaded = false;

        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 accScrollPosition = Vector2.zero;

        private AdditionalAccessorySlotData CurrentSlot { get; set; }
        private AdditionalAccessoryControlsController Controller { get; set; }        
        private ChaControl ChaControl { get; set; }

        public static AdditionalAccessoryAdvancedParentUI Instance;

        private string SelectedParent { get; set; }
        private string SelectedParentShort
        {
            get
            {
                int lastSlash = SelectedParent == null ? -1 : SelectedParent.LastIndexOf("/") + 1;
                return SelectedParent == null ? "None" : SelectedParent.Substring(lastSlash);
            }
        }


        public static void Show(AdditionalAccessorySlotData slot, ChaControl chaControl)
        {
#if DEBUG
            Log.LogInfo($"Showing UI For: {slot} on {chaControl?.fileParam?.fullname}");
#endif

            Instance.enabled = true;
            Change(slot, chaControl);
            if (slot.AdvancedParent != null && slot.AdvancedParent.Length > 0)
            {
                Transform parentTransform = chaControl.gameObject.transform.Find(slot.AdvancedParent);
                if (parentTransform != null)
                {
                    Instance.OpenParentsOf(parentTransform.gameObject);
                }
            }
            Instance.searchTerm = "";

        }

        public static void Change(AdditionalAccessorySlotData slot, ChaControl chaControl)
        {
#if DEBUG
            Log.LogInfo($"Changing UI For: {slot} on {chaControl?.fileParam?.fullname}");
#endif

            Instance.Controller = chaControl.gameObject.GetComponent<AdditionalAccessoryControlsController>();
            Instance.CurrentSlot = slot;
            Instance.ChaControl = chaControl;
            Instance.SelectedParent = slot.AdvancedParent;
        }

        public static void Hide()
        {
#if DEBUG
            Log.LogInfo("Hide UI");
#endif
            Instance.enabled = false;
            Instance.CurrentSlot = null;
            Instance.ChaControl = null;
            Instance.SelectedParent = null;
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


            var rect = GUILayout.Window(8734, windowRect, DoDraw, $"Advanced Parent for Slot: {(CurrentSlot.SlotNumber + 1)} {CurrentSlot.AccessoryName}");
            windowRect.x = rect.x;
            windowRect.y = rect.y;

            if (windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();

        }

        private string searchTerm = "";
        private void DoDraw(int id)
        {
            GUILayout.BeginVertical();
            {

                string lastSearchTerm = searchTerm;

                // Header
                GUILayout.BeginHorizontal(expandLayoutOption);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close Me", GUILayout.ExpandWidth(false))) enabled = false;
                GUILayout.EndHorizontal();

                // Current Parent
                GUILayout.BeginHorizontal(expandLayoutOption);
                GUILayout.Label($"Current Parent: {CurrentSlot.AdvancedParentShort}");
                GUILayout.Space(30f);
                if (GUILayout.Button("View Selected Parent", GUILayout.ExpandWidth(false)))
                {
                    Transform parentTransform = ChaControl.gameObject.transform.Find(CurrentSlot.AdvancedParent);
                    if (parentTransform != null)
                    {
                        OpenParentsOf(parentTransform.gameObject);
                    }
                    searchTerm = "";
                }
                if (GUILayout.Button("Clear Parent", GUILayout.ExpandWidth(false)))
                {
                    Controller.SetAdvancedParent(null, CurrentSlot.SlotNumber);
                    AdditionalAccessoryControlsPlugin.Instance.RefreshAdvancedParentLabel();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(expandLayoutOption);

                if (GUILayout.Button("Collapse All", GUILayout.ExpandWidth(false))) openedBones.Clear();
                GUILayout.Space(10f);                
                searchTerm = GUILayout.TextField(searchTerm);                

                if (GUILayout.Button("X", GUILayout.ExpandWidth(false))) searchTerm = "";
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                {
                    GUILayout.BeginHorizontal(expandLayoutOption);
                    GUILayout.Label("Shortcuts (Search Terms in a Box)");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);
                    if (GUILayout.Button("Hair: Back", GUILayout.ExpandWidth(false))) searchTerm = "ct_hairB";
                    if (GUILayout.Button("Hair: Front", GUILayout.ExpandWidth(false))) searchTerm = "ct_hairF";
                    if (GUILayout.Button("Hair: Side", GUILayout.ExpandWidth(false))) searchTerm = "ct_hairS";
                    if (GUILayout.Button("Hair: Ext", GUILayout.ExpandWidth(false))) searchTerm = "ct_hairO";
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(expandLayoutOption);
                    if (GUILayout.Button("Tongue", GUILayout.ExpandWidth(false))) searchTerm = "o_tang";
                    if (GUILayout.Button("Teeth", GUILayout.ExpandWidth(false))) searchTerm = "o_tooth";
                    if (GUILayout.Button("Eyelashes", GUILayout.ExpandWidth(false))) searchTerm = "o_eyelashes";
                    if (GUILayout.Button("Head (Lips & Eyebrows)", GUILayout.ExpandWidth(false))) searchTerm = "o_head";
                    if (GUILayout.Button("Body", GUILayout.ExpandWidth(false))) searchTerm = "o_body_cf";
                    GUILayout.EndHorizontal();

                    accScrollPosition = GUILayout.BeginScrollView(accScrollPosition, GUILayout.Height(100));
                    GUILayout.BeginVertical();
                    GUILayout.Label("Accessories - Parent to the slot to share parents, parent to N_Move (one or two deep from the slot) to parent to the accessory or choose a more specific bone.");
                    string[] availableSlots = AvailableSlots();
                    string[] availableSlotsNums = AvailableSlotsNums();
                    for (int index = 0; index < availableSlots.Length;)
                    {
                        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                        for (int i = 0; i < 20 && index < availableSlots.Length; i++, index++)
                        {
                            if (GUILayout.Button(availableSlots[index], GUILayout.ExpandWidth(false))) searchTerm = "ca_slot" + availableSlotsNums[index];                            
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndScrollView();

                    BuildDynamicBoneChains();
                    if (DynamicBoneChains.Keys.Count > 0)
                    {
                        GUILayout.Label("Clothing Dynamic Bone Chains - Root of Available Bone Chains - Expand to attach deeper in the chain");
                        foreach (string clothing in DynamicBoneChains.Keys)
                        {
                            GUILayout.BeginHorizontal(expandLayoutOption);
                            GUILayout.Label(clothing, GUILayout.ExpandWidth(false));
                            foreach (GameObject chainRoot in DynamicBoneChains[clothing])
                            {
                                if (GUILayout.Button(chainRoot.name, GUILayout.ExpandWidth(false))) searchTerm = chainRoot.name;
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndVertical();

                if (lastSearchTerm != searchTerm)
                    openedBones.Clear();

                scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.Height(300));
                if (searchTerm == "o_body_cf")
                    BuildObjectTree(Body, 0);
                else
                    BuildObjectTree(Root, 0);
                GUILayout.EndScrollView();


                int vertexCount = SkinnedMeshRendererCheck();
                GUILayout.BeginHorizontal(expandLayoutOption);
                GUILayout.FlexibleSpace();
                if (SelectedParent != null)
                {
                    
                    if (GUILayout.Button($"Attach to Selected Parent: {SelectedParentShort}", GUILayout.ExpandWidth(false)))
                    {
                        if (vertices.Length > 0)
                        {
                            ChaControl.SetAccessoryPos(CurrentSlot.SlotNumber, 0, nMoveDiff.x, false, 1);
                            ChaControl.SetAccessoryPos(CurrentSlot.SlotNumber, 0, nMoveDiff.y, false, 2);
                            ChaControl.SetAccessoryPos(CurrentSlot.SlotNumber, 0, nMoveDiff.z, false, 4);
                            GameObject.Find("CharaCustom").GetComponentInChildren<CharaCustom.CustomAcsCorrectSet>().UpdateCustomUI();
                            Controller.SetAdvancedParent(SelectedParent + "|" + vertices, CurrentSlot.SlotNumber);                            
                        }
                        else
                        {
                            Controller.SetAdvancedParent(SelectedParent, CurrentSlot.SlotNumber);
                        }
                        AdditionalAccessoryControlsPlugin.Instance.RefreshAdvancedParentLabel();
                    }                    
                    if (vertexCount > 0)
                    {
                        if (vertices == "")
                            vertices = "0";
                        else if (int.Parse(vertices) >= vertexCount)
                            vertices = (vertexCount - 1).ToString();

                        GUILayout.Label("Vertex Index: ");
                        vertices = GUILayout.TextField(vertices, GUILayout.Width(60));
                        if (!int.TryParse(vertices, out int result))
                            vertices = "0";

                        if (GUILayout.Button("Select Closest Vertex", GUILayout.ExpandWidth(false)))
                        {
                            StartCoroutine(DoFindClosestVertice());
                        }
                        GUILayout.Label($" (of {vertexCount})");
                    }
                    else
                    {
                        vertices = "";
                    }
                }
                else
                {
                    GUILayout.Box($"Select a Bone Above...", GUILayout.ExpandWidth(false));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (vertexCount > 0)
                {
                    GUILayout.BeginHorizontal(expandLayoutOption);
                    GUILayout.Label("Position the accessory to target location. Click 'Select Closest Vertex' to find index #. Click Attach, then fine adjust position. Or direct enter Index #");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }



            }
            GUILayout.EndVertical();
            UnityEngine.GUI.DragWindow();
        }

        private string vertices = "";

        private HashSet<GameObject> openedBones = new HashSet<GameObject>();

        private void BuildObjectTree(GameObject go, int indentLevel)
        {
            if (searchTerm.Length == 0 || go.name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) > -1 || openedBones.Contains(go.transform.parent.gameObject))
            {
                Color c = GUI.color;

                if (BuildParentString(go) == CurrentSlot.AdvancedParent)
                    GUI.color = Color.red;
                else if (go.name == SelectedParentShort)
                    GUI.color = Color.cyan;

                GUILayout.BeginHorizontal();

                if (openedBones.Contains(go.transform.parent.gameObject))
                    GUILayout.Space(indentLevel * 25f);
                else
                    indentLevel = 0;

                if (go.transform.childCount > 0)
                {
                    if (GUILayout.Toggle(openedBones.Contains(go), "", GUILayout.ExpandWidth(false)))
                    {
                        openedBones.Add(go);
                    }
                    else
                    {
                        openedBones.Remove(go);
                    }
                }
                else
                    GUILayout.Space(25f);

                if (GUILayout.Button(go.name, GUILayout.ExpandWidth(false))) SelectedParent = BuildParentString(go);

                GUILayout.EndHorizontal();
                GUI.color = c;
            }
            if (go.name != "o_body_cf" && (searchTerm.Length > 0 || openedBones.Contains(go)))
            {
                foreach (Transform child in go.transform)
                {
                    BuildObjectTree(child.gameObject, indentLevel + 1);
                }
            }            
        }

        private void OpenParentsOf(GameObject go)
        {
            openedBones.Add(go);
            if (go != Root)
                OpenParentsOf(go.transform.parent.gameObject);
        }

        private GameObject Root
        {
            get
            {
                return ChaControl.objAnim.transform.Find("cf_J_Root").gameObject;
            }
        }

        private GameObject Body
        {
            get
            {
                return ChaControl.gameObject.transform.Find("BodyTop/p_cf_body_00/n_o_root/n_body_base/n_body_cf/o_body_cf").gameObject;
            }
        }

        private string BuildParentString(GameObject go)
        {            
            string fullParentString = $"{go.name}";
            GameObject nowGO = go.transform.parent.gameObject;
            while (nowGO != ChaControl.gameObject)
            {                
                fullParentString = $"{nowGO.name}/{fullParentString}";
                nowGO = nowGO.transform.parent.gameObject;
            };
            return fullParentString;
        }

        private int SkinnedMeshRendererCheck()
        {
            if (SelectedParent == null || SelectedParent.Length == 0)
                return -1;

            SkinnedMeshRenderer smr = ChaControl.transform.Find(SelectedParent)?.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
                return smr.sharedMesh.vertexCount;
            else
                return -1;
        }

        Vector3 nMoveDiff;
        List<Vector3> vertexList = new List<Vector3>();
        private IEnumerator DoFindClosestVertice()
        {
            yield return new WaitForEndOfFrame();

            Transform accMoveTransform = Controller.GetAccessorialMoveTransform(CurrentSlot.SlotNumber);
            if (accMoveTransform == null)
            {
                vertices = "0";
            }
            else
            {
#if DEBUG
                Log.LogInfo($"Current ACC Move Transform Location {accMoveTransform.position}");
#endif

                Transform parentTransform = ChaControl.transform.Find(SelectedParent);
                SkinnedMeshRenderer smr = parentTransform?.GetComponent<SkinnedMeshRenderer>();
                if (parentTransform == null || smr == null)
                {
                    vertices = "0";
                }
                Mesh mesh = new Mesh();
                smr.BakeMesh(mesh);
                mesh.GetVertices(vertexList);

                int closestVertexIndex = 0;
                float closestDistance = float.MaxValue;
                for (int i = 0; i < vertexList.Count; i++)
                {
                    Vector3 worldPosition = parentTransform.TransformPoint(vertexList[i]);
                    Vector3 accWorldPosition = accMoveTransform.position;
                    float distance = Vector3.Distance(worldPosition, accWorldPosition);                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestVertexIndex = i;
                        nMoveDiff = accWorldPosition - worldPosition;
#if DEBUG
                        Log.LogInfo($"Closer: Index: {i} Distance: {distance} Pos: {worldPosition.x:0.0000} {worldPosition.y:0.0000} {worldPosition.z:0.0000} Acc Pos: {accMoveTransform.position.x:0.0000} {accMoveTransform.position.y:0.0000} {accMoveTransform.position.z:0.0000} New NMove: {nMoveDiff.x:0.0000} {nMoveDiff.y:0.0000} {nMoveDiff.z:0.0000}");
#endif
                    }
                }
                vertices = closestVertexIndex.ToString();
            }
        }

        private string[] AvailableSlots()
        {
            AdditionalAccessoryControlsController aacController = Controller.gameObject.GetComponent<AdditionalAccessoryControlsController>();
            return aacController.SlotData.Where<AdditionalAccessorySlotData>(slot => !slot.IsEmpty && slot.SlotNumber != CurrentSlot.SlotNumber).Select<AdditionalAccessorySlotData, string>(slot => (slot.SlotNumber + 1).ToString()).ToArray();
        }
        private string[] AvailableSlotsNums()
        {
            AdditionalAccessoryControlsController aacController = Controller.gameObject.GetComponent<AdditionalAccessoryControlsController>();
            return aacController.SlotData.Where<AdditionalAccessorySlotData>(slot => !slot.IsEmpty && slot.SlotNumber != CurrentSlot.SlotNumber).Select<AdditionalAccessorySlotData, string>(slot => (slot.SlotNumber).ToString("00")).ToArray();
        }

        private Dictionary<string, List<GameObject>> DynamicBoneChains = new Dictionary<string, List<GameObject>>();
        private void BuildDynamicBoneChains()
        {
            DynamicBoneChains.Clear();

            ScanClothingItem("Top", ChaControl.transform.Find("BodyTop/ct_clothesTop"));
            ScanClothingItem("Bot", ChaControl.transform.Find("BodyTop/ct_clothesBot"));
            ScanClothingItem("Inner Top", ChaControl.transform.Find("BodyTop/ct_inner_t"));
            ScanClothingItem("Inner Bot", ChaControl.transform.Find("BodyTop/ct_inner_b"));
            ScanClothingItem("Gloves", ChaControl.transform.Find("BodyTop/ct_gloves"));
            ScanClothingItem("Pantyhose", ChaControl.transform.Find("BodyTop/ct_panst"));
            ScanClothingItem("Socks", ChaControl.transform.Find("BodyTop/ct_socks"));
            ScanClothingItem("Shoes", ChaControl.transform.Find("BodyTop/ct_shoes"));

        }

        private void ScanClothingItem(string clothing, Transform clothingTrans)
        {
            if (clothingTrans == null)
                return;

            DynamicBone[] bones = clothingTrans.gameObject.GetComponents<DynamicBone>();
            DynamicBone_Ver02[] bonesV2 = clothingTrans.gameObject.GetComponents<DynamicBone_Ver02>();

            if (bones.Length > 0 || bonesV2.Length > 0)
            {
                List<GameObject> chainRoots = new List<GameObject>();
                foreach (DynamicBone bone in bones)
                {
                    if (bone.m_Root != null)
                        chainRoots.Add(bone.m_Root.gameObject);
                }
                foreach (DynamicBone_Ver02 bone in bonesV2)
                {
                    if (bone.Root != null)
                        chainRoots.Add(bone.Root.gameObject);
                }
                if (chainRoots.Count > 0)
                    DynamicBoneChains[clothing] = chainRoots;
            }            
        }
    }
}
