using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    public class GangColorTranslation
    {
        public List<VehicleColor> VehicleColors { get; private set; }
        public PotentialGangMember.MemberColor BaseColor { get; private set; }
        public int[] BlipColors { get; private set; }

        public GangColorTranslation()
        {
            VehicleColors = new List<VehicleColor>();
        }

        public GangColorTranslation(PotentialGangMember.MemberColor baseColor, List<VehicleColor> vehicleColors, int[] blipColors)
        {
            BaseColor = baseColor;
            VehicleColors = vehicleColors;
            BlipColors = blipColors;
        }
    }

}
