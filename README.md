# AdditionalAccessoryControls

Plugin to add some much needed quality of life improvements to accessory handling and outfits. Found on the Mods subtab of the accessory details menu (over on the right next to Adjust).

# **Features**

# Character Accessories

Marks an accessoy as being a character accessory (attached to the character) instead of a coordinate (outfit) accessory. This results in the following behaviour changes:

1: Character accessories are not saved to coordinate cards.\
2: When a coordinate card is loaded, character accessories currently worn are merged with the coordinate card accessories. Note, this results in slot movement as the character accessories are slotted into available empty slots. If no empty slots are available, new More Accessory slots are created and used.

# Match Hair Color on Coord Load

If this accessory is a hair type accessory (has the match to hair color buttons on the Color tab), you are loading a coordinate with this accessory AND this is checked, the accessory will automatically be matched to the Back Hair color of the character loading the coordinate outfit.

# Visibility Rules

Opens up a sub-dialog with a variety of visibility rules that can be applied to this accessory.

**Outfit State Rules**: These rules will hide the accessory if ANY of the matching clothing states apply. Use to prevent an accessory from clipping through items OR to hide an accessory when the 'attached' clothing item is removed.

**Accessory Slot Link**: Links this accessory to another slot. When the linked slot is visible, this accessory is visible and vice versa. Note: Bi-directional links do not work (to avoid recursion), pick a parent/child direction and stick with it. Use to setup blocks of accessories that can be activated/deactivated in a single click.\
**Accessory Slot Inverse-Link**: Same as link but backwards. When the parent slot is visibile this is made invisible and vice versa. Use to create alternate accessory looks, switch between two different hair wigs with one click for example.

**LifeCycle Rules**:\
   *Apply Rules on Studio Scene Character/Outfit Change*: Normally visibility rules don't fire when loading/reloading a character or outfit into studio. This overrides that. Initial scene load is always exempt.\
   *Hide on Startup*: This accessorial loads initially as invisible instead of the normal visible. Does not apply in Studio. Use for weapons, shields, hats and other things you sometimes want for pictures but are more normally in the way.\
   *Hide/Show on H Scene Start*: This accessory is made visible/invisible when an HScene starts. This happens AFTER the initial HScene dialog and immediately prior to the first animation playing. Example: Hide a face veil for sex, and then use the next option to turn it back on afterwards.\
   *Hide/Show on H Scene End*: As above, but after the HSceen, happening immediately prior to the exit dialogue.\
   
**Hair/Body Rules**:\
   *Hair Rules*: When this accessory is visible the following hair parts are made invisible. Use to swap in and out a hair part with a wig, or hide hair under a helmet or hood.\
   *Body Rules*: When this accessory is visible the following body parts are scaled to 0. Use this to prevent elf ears clipping through helmets, hands popping through some glove items, etc.

# Coordinate Visibility Override Rules

Accessed from the toggle button in the Plugin Settings section of the menu (right side).

Adds options to help deal with some common collisions where character accessory visibility rules don't work as desired when loaded into some coordinates.

**Slot Suppression Rules**: Setting one of these suppresses rules based on this slot for firing for this coordinate. An example might be an open bra shouldn't suppress nipple accessories even though it's in an on state. 

**Slot Overrides**: These allow you to map one slot or even specific slot/state combinations to other slots or slot/states. Use this when you have an item in a non-standard slot, for example a glove in a pantyhose slot. Mapping the pantyhose to gloves allows accessory rules for gloves to work as desired for this item.

Note: Overriding a slot means it no longer counts as it's original slot (so as in the prior example, any rules for pantyhose would not fire as it would be considered a glove). If it is desired that the slot count as another slot AND as itself, simply override the slot to itself in addition to other targets. A use case would be tops that should fire bot rules, as the cover the bot, but aren't flagged as covering the bot in the item flags. Override top->top AND top->bottom means that the item will now fire rules for for accessories with either top or bottom visibility rules set.
   
# Show/Hide Button

Shows and hides this accessory.

# Advanced Parents

Allows you to parent the accessory to...just about anything on the character. Includes other accessories, hair, clothing and more.

Click the button to open the advanced parent GUI. Find the bone you want to attach to, select it and hit the attach button at the bottom. Then tweak positioning and rotation as normal. Remove the link by clicking clear up at the top.

If the attachment point is a SkinnedMeshRenderer (such as the tongue, teeth, eyebrows or head) you will be prompted for a vertex point to attach the accessory to more specifically. To help you find the appropriate point, a 'Find Closest' button appears. Position the accessory at the point you want to attach it and click the find closest, the vertex index will fill in with the closest available vertex point number. Then click Attach as per normal to link the accessory. Some final position adjustment may be required.

Note 1: If you remove the think the accessory is parented to, it will revert to it's nominal parent location and wait for the parent to reappear. There is a performance penalty for this so clear parents you no longer intend to use.\
Note 2: When linking accessories together, use the same base parent location or there will be small differences in positioning depending on animation.

Body Mesh Attachment: The special body search button allows attaching accessories to the body mesh. The attachment process works like other mesh parents. However, the body mesh, being animated, is more expensive to attach to. The first accessory attached to the body mesh incurs an overhead in processing (additional body mesh accessories on the same character are free however). To help control the cost, position extrapolation is used to cut frame hit, but can cause a slight choppiness to the accessory positioning, fast enough action will bypass this and force a real update. This can be controlled in plugin settings:

- Update Body Mesh Parented Accessories Every N Frames: Controls how many frames are real mesh synchronizations vs. interpolations. A value of 1 updates every frame (best results, worst performance). 2 updates every other frame, 3 every 3 frames and so on. 
- History Key Frames for Body Mesh Parents (Advanced Option): How many frames of back history is used to build interpolation. 3 is more responsive, 5 is smoother.
- Body Mesh Parent Fast Action Threshold (Advanced Option): How fast moving things must be to force real calculation instead of interpolation.
