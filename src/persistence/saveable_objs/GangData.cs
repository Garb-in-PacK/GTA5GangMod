using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    public class GangData
    {

        public GangData()
        {
            Gangs = new List<Gang>();
        }

        public List<Gang> Gangs { get; private set; }
    }

}
