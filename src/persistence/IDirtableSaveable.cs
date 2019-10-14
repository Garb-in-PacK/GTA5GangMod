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
    /// <summary>
    /// interface for saveable data objs that should probably be handled by the AutoSaver
    /// </summary>
    public interface IDirtableSaveable
    {

        void LoadData();

        void SaveData(bool notifyMsg = true);

        bool IsDirty { get; set; }

        bool NotifyNextSave { get; set; }
    }
}