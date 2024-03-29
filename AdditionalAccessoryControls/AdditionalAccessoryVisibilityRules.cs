﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AdditionalAccessoryControls
{
    public enum AdditionalAccessoryVisibilityRules
    {
        NONE = 0,

        // General Rules
        STARTUP = 1,
        H_START=2,
        H_END=3, 
        STUDIO_LOAD=4,
        
        // Slot Rules
        TOP = 10,
        BOT = 11,
        INNER_TOP = 12,
        INNER_BOT = 13,
        PANTYHOSE = 14,
        GLOVE = 15,
        SOCK = 16,
        SHOE = 17,

        // Other Accessory Rules
        ACCESSORY_LINK = 20,
        ACCESSORY_INVERSE_LINK = 21,

        // Special
        HAIR = 30,
        NOSE = 31,
        EAR = 32,
        HAND = 33,
        FOOT = 34,
        EYELASH = 35
    }

    public enum AdditionalAccessoryVisibilityRulesModifiers
    {
        // When Not Needed
        NONE = 0,

        // General Options
        SHOW = 1,
        HIDE = 2,

        // Clothing Options
        ON = 10,
        HALF = 11,
        OFF = 12,

        // Special
        // Hair Visibility Options
        HAIR_FRONT = 30,
        HAIR_BACK = 31,
        HAIR_SIDE = 32,
        HAIR_EXT =33,
        HAIR_ACC=34,

        LEFT = 40,
        RIGHT = 41,

        ALL = 50

    }
}
