using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// contains options related to colors used by gangs and their "translations"
    /// </summary>
    [Serializable]
    public class GangColorsOptions : IModOptionGroup
    {

        public GangColorsOptions() { }

        public void SetOptionsToDefault()
        {
            SetColorTranslationDefaultValues();
        }


        public List<GangColorTranslation> SimilarColors { get; private set; }

        public List<VehicleColor> ExtraPlayerExclusiveColors { get; private set; }

        public GangColorTranslation GetGangColorTranslation(PotentialGangMember.MemberColor baseColor)
        {
            for (int i = 0; i < SimilarColors.Count; i++)
            {
                if (SimilarColors[i].BaseColor == baseColor)
                {
                    return SimilarColors[i];
                }
            }

            return null;
        }

        public PotentialGangMember.MemberColor TranslateVehicleToMemberColor(VehicleColor vehColor)
        {
            for (int i = 0; i < SimilarColors.Count; i++)
            {
                if (SimilarColors[i].VehicleColors.Contains(vehColor))
                {
                    return SimilarColors[i].BaseColor;
                }
            }

            return PotentialGangMember.MemberColor.white;
        }

        /// <summary>
        /// makes sure the list contains valid entries;
        /// if not, resets it to default values.
        /// Returns true if it had to reset to default (had to enforce)
        /// </summary>
        /// <returns></returns>
        public bool EnforceColorsIntegrity()
        {
            if (SimilarColors.Count == 0)
            {
                SetColorTranslationDefaultValues();
                return true;
            }
            else
            {
                if (SimilarColors[0].BlipColors == null)
                {
                    SetColorTranslationDefaultValues();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// declares similarColors and extraPlayerExclusiveColors as new lists with default values
        /// </summary>
        public void SetColorTranslationDefaultValues()
        {
            SimilarColors = GangColorTranslation.GetDefaultColorTranslations();

            ExtraPlayerExclusiveColors = new List<VehicleColor>()
            {
                VehicleColor.MatteOliveDrab,
                VehicleColor.MatteOrange,
                VehicleColor.MetallicBeachSand,
                VehicleColor.MetallicBeechwood,
                VehicleColor.MetallicBistonBrown,
                VehicleColor.MetallicBronze,
                VehicleColor.MetallicChampagne,
                VehicleColor.MetallicChocoBrown,
                VehicleColor.MetallicChocoOrange,
                VehicleColor.MetallicCream,
                VehicleColor.MetallicDarkBeechwood,
                VehicleColor.MetallicGunMetal,
                VehicleColor.MetallicLime,
                VehicleColor.MetallicMossBrown,
                VehicleColor.MetallicOrange,
                VehicleColor.MetallicPuebloBeige,
                VehicleColor.MetallicStrawBeige,
                VehicleColor.MetallicSunBleechedSand,
                VehicleColor.MetallicSunriseOrange,
                VehicleColor.Orange,
                VehicleColor.WornLightOrange,
                VehicleColor.WornOrange,
                VehicleColor.WornSeaWash,
            };
        }
    }
}
