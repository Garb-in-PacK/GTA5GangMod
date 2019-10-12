﻿using System;
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
namespace GTA.GangAndTurfMod {
	class AutoSaver : Script {

		public static AutoSaver instance;

		public bool gangDataDirty = false, zoneDataDirty = false;
		public bool gangDataNotifySave = false, zoneDataNotifySave = false;

		void OnTick(object sender, EventArgs e) {
			if (ModOptions.instance == null) return;
			if (ModOptions.instance.msAutoSaveInterval <= 0) { //reset if invalid
				ModOptions.instance.msAutoSaveInterval = 3000;
			}
			Wait(ModOptions.instance.msAutoSaveInterval);
			if (gangDataDirty) {
				PersistenceHandler.SaveToFile(GangManager.gangData, "GangData", gangDataNotifySave);
				gangDataDirty = false;
				gangDataNotifySave = false;
				return;
			}
			if (zoneDataDirty) {
				PersistenceHandler.SaveToFile(ZoneManager.instance.zoneData, "TurfZoneData", zoneDataNotifySave);
				zoneDataDirty = false;
				zoneDataNotifySave = false;
				return;
			}
		}


		public AutoSaver() {
			this.Tick += OnTick;
			instance = this;
		}

	}
}
