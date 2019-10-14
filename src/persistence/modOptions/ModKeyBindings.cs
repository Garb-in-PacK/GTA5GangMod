using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    [Serializable]
    public class ModKeyBindings : IModOptionGroup
    {

        public ModKeyBindings() { }

        public enum ChangeableKeyBinding
        {
            GangMenuBtn,
            ZoneMenuBtn,
            MindControlBtn,
            AddGroupBtn,
        }

        public Keys OpenGangMenuKey { get; set; } = Keys.B;
        public Keys OpenZoneMenuKey { get; set; } = Keys.N;
        public Keys MindControlKey { get; set; } = Keys.J;
        public Keys AddToGroupKey { get; set; } = Keys.H;

        public bool JoypadControls { get; set; } = false;

        public void SetOptionsToDefault()
        {
            OpenGangMenuKey = Keys.B;
            OpenZoneMenuKey = Keys.N;
            MindControlKey = Keys.J;
            AddToGroupKey = Keys.H;
            JoypadControls = false;
        }

        public void SetKey(ChangeableKeyBinding keyToChange, Keys newKey, IDirtableSaveable modOptions)
        {
            if (modOptions == null) throw new ArgumentNullException(nameof(modOptions));

            if (newKey == Keys.Escape || newKey == Keys.ShiftKey ||
                newKey == Keys.Insert || newKey == Keys.ControlKey)
            {
                UI.ShowSubtitle("That key can't be used because some settings would become unaccessible due to conflicts.");
                return;
            }

            //verify if this key isn't being used by the other commands from this mod
            //if not, set the chosen key as the new one for the command!
            List<Keys> curKeys = new List<Keys> {
                OpenGangMenuKey,
                OpenZoneMenuKey,
                MindControlKey,
                AddToGroupKey
            };

            if (curKeys.Contains(newKey))
            {
                UI.ShowSubtitle("That key is already being used by this mod's commands.");
                return;
            }
            else
            {
                switch (keyToChange)
                {
                    case ChangeableKeyBinding.AddGroupBtn:
                        AddToGroupKey = newKey;
                        break;
                    case ChangeableKeyBinding.GangMenuBtn:
                        OpenGangMenuKey = newKey;
                        break;
                    case ChangeableKeyBinding.MindControlBtn:
                        MindControlKey = newKey;
                        break;
                    case ChangeableKeyBinding.ZoneMenuBtn:
                        OpenZoneMenuKey = newKey;
                        break;
                }

                UI.ShowSubtitle("Key changed!");

                modOptions.IsDirty = true;
            }
        }
    }
}
