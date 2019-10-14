using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using GTA.Native;

/// <summary>
/// the autosaver has "dirty" flags;
/// every once in a while (interval configurable in ModOptions), 
/// it checks those flags and saves if one of them is true (only one per check)
/// </summary>
namespace GTA.GangAndTurfMod
{
    public class AutoSaver : Script
    {
        public List<IDirtableSaveable> Dirtables { get; } = new List<IDirtableSaveable>();

        public ModOptions ModOptions { get; set; }
        public static AutoSaver Instance { get; private set; }

        private void OnTick(object sender, EventArgs e)
        {
            if (ModOptions == null) return;
            if (ModOptions.MsAutoSaveInterval <= 0)
            { //reset if invalid
                ModOptions.MsAutoSaveInterval = 3000;
            }
            Wait(ModOptions.MsAutoSaveInterval);

            foreach(IDirtableSaveable dirtable in Dirtables)
            {
                if (dirtable.IsDirty)
                {
                    dirtable.SaveData(dirtable.NotifyNextSave);
                    dirtable.IsDirty = false;
                    dirtable.NotifyNextSave = false;
                }
            }
        }


        public AutoSaver()
        {
            this.Tick += OnTick;
            Instance = this;
        }

    }
}
