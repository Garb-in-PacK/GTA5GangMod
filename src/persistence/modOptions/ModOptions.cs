using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Resolvers;

namespace GTA.GangAndTurfMod
{
    [System.Serializable]
    public class ModOptions : IDirtableSaveable
    {
        public ModOptions()
        {
            LoadData();
            WeaponOptions.SetupPrimaryWeapons(this);
        }

        /// <summary>
        /// 0 = nothing,
        /// 1 = errors only,
        /// 2 = notifications on save/load success, important notifications,
        /// 3 = more sensitive procedures (might be spammy),
        /// 4 = sensitive, but more common, procedures, like spawning,
        /// 5 = updates (spam)
        /// </summary>
        public int LoggerLevel { get; set; } = 1;

        public bool NotificationsEnabled { get; set; } = true;

        public int MsAutoSaveInterval { get; set; } = 3000;


        public ModKeyBindings Keys { get; private set; }

        public GangsOptions GangsOptions { get; private set; }

        public ZoneOptions ZoneOptions { get; private set; }

        public WarOptions WarOptions { get; private set; }

        public MemberUpgradeOptions MemberUpgradeOptions { get; private set; }

        public BackupOptions BackupOptions { get; private set; }

        public MemberAIOptions MemberAIOptions { get; private set; }

        public GangAIOptions GangAIOptions { get; private set; }

        public SpawningOptions SpawningOptions { get; private set; }

        public WeaponOptions WeaponOptions { get; private set; }

        public GangColorsOptions GangColorsOptions { get; private set; }

        public GangNamesOptions GangNamesOptions { get; private set; }

        public bool IsDirty { get; set; }
        public bool NotifyNextSave { get; set; }


        /// <summary>
        /// resets all values, except for the first and last gang names and the color translations
        /// </summary>
        public void SetAllValuesToDefault()
        {
            MsAutoSaveInterval = 3000;

            Keys.SetOptionsToDefault();

            WarOptions.SetOptionsToDefault();

            SpawningOptions.SetOptionsToDefault();

            MemberAIOptions.SetOptionsToDefault();

            GangAIOptions.SetOptionsToDefault();

            BackupOptions.SetOptionsToDefault();
            
            NotificationsEnabled = true;
            LoggerLevel = 1;

            GangsOptions.SetOptionsToDefault();

            SaveData();

            GangManager.ResetGangUpdateIntervals();
        }

        public void LoadData()
        {
            ModOptions loadedOptions = PersistenceHandler.LoadFromFile<ModOptions>("ModOptions");
            if (loadedOptions != null)
            {
                //get the loaded options
                this.MsAutoSaveInterval = loadedOptions.MsAutoSaveInterval;
                this.NotificationsEnabled = loadedOptions.NotificationsEnabled;
                this.LoggerLevel = loadedOptions.LoggerLevel;

                this.Keys = loadedOptions.Keys;

                this.MemberUpgradeOptions = loadedOptions.MemberUpgradeOptions;

                this.MemberAIOptions = loadedOptions.MemberAIOptions;

                this.GangsOptions = loadedOptions.GangsOptions;

                this.BackupOptions = loadedOptions.BackupOptions;

                this.GangAIOptions = loadedOptions.GangAIOptions;

                this.WarOptions = loadedOptions.WarOptions;

                this.GangColorsOptions = loadedOptions.GangColorsOptions;

                SaveData();
            }
            else
            {
                SetAllValuesToDefault();
                GangNamesOptions.SetNameListsDefaultValues();
                GangColorsOptions.SetColorTranslationDefaultValues();
                SaveData(true);
            }
        }

        public void SaveData(bool notifyMsg = true)
        {
            PersistenceHandler.SaveToFile(this, "ModOptions", notifyMsg);
        }
    }
}
