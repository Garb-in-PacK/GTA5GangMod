using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using GTA;
using GTA.Math;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// basically a potential gang member with more data to be saved
    /// </summary>
    public class FreemodePotentialGangMember : PotentialGangMember
    {
        /// <summary>
        /// male indexes seem to go from 0-20, female from 21-41.
        /// dlc faces seem to be 42 43 44 male and 45 female
        /// </summary>
        private const int NUM_FACE_INDEXES = 46;

        public int[] ExtraDrawableIndexes { get; set; }
        public int[] ExtraTextureIndexes { get; set; }
        public int[] PropDrawableIndexes { get; set; }
        public int[] PropTextureIndexes { get; set; }
        public int[] HeadOverlayIndexes { get; set; }

        public enum FreemodeGender
        {
            any,
            male,
            female
        }

        public FreemodePotentialGangMember()
        {
            HeadOverlayIndexes = new int[13];
            ExtraDrawableIndexes = new int[8];
            ExtraTextureIndexes = new int[8];
            PropDrawableIndexes = new int[3];
            PropTextureIndexes = new int[3];
            modelHash = -1;
            myStyle = DressStyle.special;
            linkedColor = MemberColor.white;
            torsoDrawableIndex = -1;
            torsoTextureIndex = -1;
            legsDrawableIndex = -1;
            legsTextureIndex = -1;
            hairDrawableIndex = -1;
            headDrawableIndex = -1;
            headTextureIndex = -1;
        }

        public FreemodePotentialGangMember(Ped targetPed, DressStyle myStyle, MemberColor linkedColor) : base(targetPed, myStyle, linkedColor)
        {
            HeadOverlayIndexes = new int[13];
            ExtraDrawableIndexes = new int[8];
            ExtraTextureIndexes = new int[8];
            PropDrawableIndexes = new int[3];
            PropTextureIndexes = new int[3];

            //we've already got the model hash, torso indexes and stuff.
            //time to get the new data
            for (int i = 0; i < HeadOverlayIndexes.Length; i++)
            {
                HeadOverlayIndexes[i] = Function.Call<int>(Hash._GET_PED_HEAD_OVERLAY_VALUE, targetPed, i);

                if (i < PropDrawableIndexes.Length)
                {
                    PropDrawableIndexes[i] = Function.Call<int>(Hash.GET_PED_PROP_INDEX, targetPed, i);
                    PropTextureIndexes[i] = Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, targetPed, i);
                }

                //extra drawable indexes
                if (i == 1)
                {
                    ExtraDrawableIndexes[0] = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, targetPed, i);
                    ExtraTextureIndexes[0] = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, targetPed, i);
                }

                //indexes from 5 to 11
                if (i > 4 && i < 12)
                {
                    ExtraDrawableIndexes[i - 4] = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, targetPed, i);
                    ExtraTextureIndexes[i - 4] = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, targetPed, i);
                }
            }

        }

        public override void SetPedAppearance(Ped targetPed)
        {
            int pedPalette = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, targetPed, 1);

            base.SetPedAppearance(targetPed);

            SetPedFaceBlend(targetPed);

            //hair colors seem to go till 64
            int randomHairColor = RandoMath.CachedRandom.Next(0, 64);
            int randomHairStreaksColor = RandoMath.CachedRandom.Next(0, 64);

            Function.Call(Hash._SET_PED_HAIR_COLOR, targetPed, randomHairColor, randomHairStreaksColor);

            //according to what I saw using menyoo, eye colors go from 0 to 32.
            //colors after 23 go pretty crazy, like demon-eyed, so I've decided to stop at 23
            Function.Call(Hash._SET_PED_EYE_COLOR, targetPed, RandoMath.CachedRandom.Next(0, 23));

            //new data time!
            for (int i = 0; i < HeadOverlayIndexes.Length; i++)
            {
                //indexes for overlays
                Function.Call(Hash.SET_PED_HEAD_OVERLAY, targetPed, i, HeadOverlayIndexes[i], 1.0f);

                //attempt to keep eyebrow and other colors similar to hair
                //we only mess with beard, eyebrow, blush, lipstick and chest hair colors
                if (i == 1 || i == 2 || i == 5 || i == 8 || i == 10)
                {
                    Function.Call(Hash._SET_PED_HEAD_OVERLAY_COLOR, targetPed, i, 2, randomHairColor, 0);
                }


                if (i < PropDrawableIndexes.Length)
                {
                    Function.Call<int>(Hash.SET_PED_PROP_INDEX, targetPed, i, PropDrawableIndexes[i], PropTextureIndexes[i], true);
                }

                //extra drawable indexes
                if (i == 1)
                {
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, i, ExtraDrawableIndexes[0], ExtraTextureIndexes[0], pedPalette);
                }

                //indexes from 5 to 11
                if (i > 4 && i < 12)
                {
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, i, ExtraDrawableIndexes[i - 4], ExtraTextureIndexes[i - 4], pedPalette);
                }
            }

        }

        public static int GetAFaceIndex(FreemodeGender desiredGender)
        {
            int returnedIndex;
            if (desiredGender == FreemodeGender.any)
            {
                returnedIndex = RandoMath.CachedRandom.Next(NUM_FACE_INDEXES);
            }
            else if (desiredGender == FreemodeGender.female)
            {
                returnedIndex = RandoMath.CachedRandom.Next(21, 43);
                if (returnedIndex == 42) returnedIndex = 45;
            }
            else
            {
                returnedIndex = RandoMath.CachedRandom.Next(0, 24);
                if (returnedIndex > 20) returnedIndex += 21;
            }

            return returnedIndex;
        }

        public static void SetPedFaceBlend(Ped targetPed)
        {
            if (targetPed == null) throw new ArgumentNullException(nameof(targetPed));

            FreemodeGender pedGender = FreemodeGender.any;
            if (targetPed.Model == PedHash.FreemodeMale01)
            {
                pedGender = FreemodeGender.male;
            }
            else if (targetPed.Model == PedHash.FreemodeFemale01)
            {
                pedGender = FreemodeGender.female;
            }
            else
            {
                UI.Notification.Show(string.Concat("attempted face blending for invalid ped type: ", targetPed.Model));
            }

            Function.Call(Hash.SET_PED_HEAD_BLEND_DATA, targetPed, GetAFaceIndex(pedGender), GetAFaceIndex(pedGender), 0, GetAFaceIndex(0),
                GetAFaceIndex(0), 0, 0.5f, 0.5f, 0, false);
        }

        public static FreemodePotentialGangMember FreemodeSimilarEntryCheck(FreemodePotentialGangMember potentialEntry)
        {
            if (potentialEntry == null) throw new ArgumentNullException(nameof(potentialEntry));

            for (int i = 0; i < MemberPool.memberList.Count; i++)
            {
                if (MemberPool.memberList[i].GetType() == typeof(FreemodePotentialGangMember))
                {
                    FreemodePotentialGangMember freeListEntry = MemberPool.memberList[i] as FreemodePotentialGangMember;

                    if (freeListEntry.modelHash == potentialEntry.modelHash &&
                    freeListEntry.hairDrawableIndex == potentialEntry.hairDrawableIndex &&
                    freeListEntry.headDrawableIndex == potentialEntry.headDrawableIndex &&
                    freeListEntry.headTextureIndex == potentialEntry.headTextureIndex &&
                    freeListEntry.legsDrawableIndex == potentialEntry.legsDrawableIndex &&
                    freeListEntry.legsTextureIndex == potentialEntry.legsTextureIndex &&
                    freeListEntry.torsoDrawableIndex == potentialEntry.torsoDrawableIndex &&
                    freeListEntry.torsoTextureIndex == potentialEntry.torsoTextureIndex &&
                    AreArrayContentsTheSame(freeListEntry.ExtraDrawableIndexes, potentialEntry.ExtraDrawableIndexes) &&
                    AreArrayContentsTheSame(freeListEntry.ExtraTextureIndexes, potentialEntry.ExtraTextureIndexes) &&
                    AreArrayContentsTheSame(freeListEntry.PropDrawableIndexes, potentialEntry.PropDrawableIndexes) &&
                    AreArrayContentsTheSame(freeListEntry.PropTextureIndexes, potentialEntry.PropTextureIndexes) &&
                    AreArrayContentsTheSame(freeListEntry.HeadOverlayIndexes, potentialEntry.HeadOverlayIndexes))
                    {
                        return freeListEntry;
                    }
                }
                else continue;

            }
            return null;
        }

        private static bool AreArrayContentsTheSame(int[] arrayX, int[] arrayY)
        {
            if (arrayX == null || arrayY == null) return false;
            if (arrayX.Length != arrayY.Length) return false;

            for (int i = 0; i < arrayX.Length; i++)
            {
                if (arrayX[i] != arrayY[i]) return false;
            }

            return true;
        }

    }
}
