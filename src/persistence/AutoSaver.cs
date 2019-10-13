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

        //TODO make those "dirtable" data containers use a Dirtable interface, 
        //and then just keep a list so that they can register here

        public static AutoSaver instance;

        public bool gangDataDirty = false, zoneDataDirty = false;
        public bool gangDataNotifySave = false, zoneDataNotifySave = false;

        private void OnTick(object sender, EventArgs e)
        {
            if (ModOptions.Instance == null) return;
            if (ModOptions.Instance.MsAutoSaveInterval <= 0)
            { //reset if invalid
                ModOptions.Instance.MsAutoSaveInterval = 3000;
            }
            Wait(ModOptions.Instance.MsAutoSaveInterval);
            if (gangDataDirty)
            {
                PersistenceHandler.SaveToFile(GangManager.GangData, "GangData", gangDataNotifySave);
                gangDataDirty = false;
                gangDataNotifySave = false;
                return;
            }
            if (zoneDataDirty)
            {
                PersistenceHandler.SaveToFile(ZoneManager.instance.zoneData, "TurfZoneData", zoneDataNotifySave);
                zoneDataDirty = false;
                zoneDataNotifySave = false;
                return;
            }
        }


        public AutoSaver()
        {
            this.Tick += OnTick;
            instance = this;
        }

    }
}
