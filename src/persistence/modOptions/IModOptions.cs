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
    public interface IModOptions
    {

        void LoadOptions();

        void SaveOptions(bool notifyMsg = true);

        bool IsDirty { get; set; }
    }
}