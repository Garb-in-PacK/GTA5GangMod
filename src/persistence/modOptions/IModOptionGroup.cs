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
    public interface IModOptionGroup
    {
        /// <summary>
        /// sets all options defined in this group to their default, hard-coded values
        /// </summary>
        void SetOptionsToDefault();

    }
}