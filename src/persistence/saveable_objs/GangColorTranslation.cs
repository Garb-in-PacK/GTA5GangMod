using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// an object representing a relation of similarity between a group of vehicle colors, a member color and a group of blip colors
    /// </summary>
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

        /// <summary>
        /// returns a list with the default, hard-coded color translations
        /// </summary>
        /// <returns></returns>
        public static List<GangColorTranslation> GetDefaultColorTranslations()
        {
            return new List<GangColorTranslation>
            {
                new GangColorTranslation(PotentialGangMember.MemberColor.black, new List<VehicleColor> {
                     VehicleColor.BrushedBlackSteel,
                    VehicleColor.MatteBlack,
                    VehicleColor.MetallicBlack,
                    VehicleColor.MetallicGraphiteBlack,
                    VehicleColor.UtilBlack,
                    VehicleColor.WornBlack,
                    VehicleColor.ModshopBlack1
                }, new int[]{40}
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.blue, new List<VehicleColor> {
                     VehicleColor.Blue,
                    VehicleColor.EpsilonBlue,
                    VehicleColor.MatteBlue,
                    VehicleColor.MatteDarkBlue,
                    VehicleColor.MatteMidnightBlue,
                    VehicleColor.MetaillicVDarkBlue,
                    VehicleColor.MetallicBlueSilver,
                    VehicleColor.MetallicBrightBlue,
                    VehicleColor.MetallicDarkBlue,
                    VehicleColor.MetallicDiamondBlue,
                    VehicleColor.MetallicHarborBlue,
                    VehicleColor.MetallicMarinerBlue,
                    VehicleColor.UtilBlue,
                    VehicleColor.MetallicUltraBlue
                }, new int[]{3, 12, 15, 18, 26, 30, 38, 42, 54, 57, 63, 67, 68, 74, 77, 78, 84}
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.green, new List<VehicleColor> {
                     VehicleColor.Green,
                    VehicleColor.HunterGreen,
                    VehicleColor.MatteFoliageGreen,
                    VehicleColor.MatteForestGreen,
                    VehicleColor.MatteGreen,
                    VehicleColor.MatteLimeGreen,
                    VehicleColor.MetallicDarkGreen,
                    VehicleColor.MetallicGreen,
                    VehicleColor.MetallicRacingGreen,
                    VehicleColor.UtilDarkGreen,
                    VehicleColor.MetallicOliveGreen,
                    VehicleColor.WornGreen,
                }, new int[]{2, 11, 25, 43, 52, 69, 82}
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.pink, new List<VehicleColor> {
                     VehicleColor.HotPink,
                    VehicleColor.MetallicVermillionPink,
                }, new int[]{8, 23, 34, 35, 41, 48, 61 }
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.purple, new List<VehicleColor> {
                     VehicleColor.MatteDarkPurple,
                    VehicleColor.MattePurple,
                    VehicleColor.MetallicPurple,
                    VehicleColor.MetallicPurpleBlue,
                }, new int[]{19, 7, 27, 50, 58, 83}
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.red, new List<VehicleColor> {
                     VehicleColor.MatteDarkRed,
                    VehicleColor.MatteRed,
                    VehicleColor.MetallicBlazeRed,
                    VehicleColor.MetallicCabernetRed,
                    VehicleColor.MetallicCandyRed,
                    VehicleColor.MetallicDesertRed,
                    VehicleColor.MetallicFormulaRed,
                    VehicleColor.MetallicGarnetRed,
                    VehicleColor.MetallicGracefulRed,
                    VehicleColor.MetallicLavaRed,
                    VehicleColor.MetallicRed,
                    VehicleColor.MetallicTorinoRed,
                    VehicleColor.UtilBrightRed,
                    VehicleColor.UtilGarnetRed,
                    VehicleColor.UtilRed,
                    VehicleColor.WornDarkRed,
                    VehicleColor.WornGoldenRed,
                    VehicleColor.WornRed,
                }, new int[]{1, 6, 49, 59, 75, 76 }
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.white, new List<VehicleColor> {
                     VehicleColor.MatteWhite,
                    VehicleColor.MetallicFrostWhite,
                    VehicleColor.MetallicWhite,
                    VehicleColor.PureWhite,
                    VehicleColor.UtilOffWhite,
                    VehicleColor.WornOffWhite,
                    VehicleColor.WornWhite,
                    VehicleColor.MetallicDarkIvory,
                }, new int[]{0, 4, 13, 37, 45 }
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.yellow, new List<VehicleColor> {
                     VehicleColor.MatteYellow,
                    VehicleColor.MetallicRaceYellow,
                    VehicleColor.MetallicTaxiYellow,
                    VehicleColor.MetallicYellowBird,
                    VehicleColor.WornTaxiYellow,
                    VehicleColor.BrushedGold,
                    VehicleColor.MetallicClassicGold,
                    VehicleColor.PureGold,
                    VehicleColor.MetallicGoldenBrown,
                    VehicleColor.MatteDesertTan,
                }, new int[]{66, 5, 28, 16, 36, 33, 46, 56, 60, 70, 71, 73, 81 }
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.gray, new List<VehicleColor> {
                     VehicleColor.MatteGray,
                   VehicleColor.MatteLightGray,
                   VehicleColor.MetallicAnthraciteGray,
                   VehicleColor.MetallicSteelGray,
                   VehicleColor.WornSilverGray,
                   VehicleColor.BrushedAluminium,
                    VehicleColor.BrushedSteel,
                    VehicleColor.Chrome,
                    VehicleColor.WornGraphite,
                    VehicleColor.WornShadowSilver,
                    VehicleColor.MetallicDarkSilver,
                    VehicleColor.MetallicMidnightSilver,
                    VehicleColor.MetallicShadowSilver,
                }, new int[]{20, 39, 55, 65}
               )
            };
        }
    }

}
