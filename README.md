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
   *Hide on Startup*: This accessorial loads initially as invisible instead of the normal visible. Does not apply in Studio. Use for weapons, shields, hats and other things you sometimes want for pictures but are more normally in the way.\
   *Hide/Show on H Scene Start*: This accessory is made visible/invisible when an HScene starts. This happens AFTER the initial HScene dialog and immediately prior to the first animation playing. Example: Hide a face veil for sex, and then use the next option to turn it back on afterwards.\
   *Hide/Show on H Scene End*: As above, but after the HSceen, happening immediately prior to the exit dialogue.\
   
**Hair/Body Rules**:\
   *Hair Rules*: When this accessory is visible the following hair parts are made invisible. Use to swap in and out a hair part with a wig, or hide hair under a helmet or hood.\
   *Body Rules*: When this accessory is visible the following body parts are scaled to 0. Use this to prevent elf ears clipping through helmets, hands popping through some glove items, etc.
   
# Show/Hide Button

Shows and hides this accessory.
