using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    public class PotentialMemberPool
    {
        public List<PotentialGangMember> memberList;

        public PotentialMemberPool()
        {
            memberList = new List<PotentialGangMember>();
        }

        public bool HasIdenticalEntry(PotentialGangMember potentialEntry)
        {
            if (potentialEntry.GetType() == typeof(FreemodePotentialGangMember))
            {
                return FreemodePotentialGangMember.FreemodeSimilarEntryCheck(potentialEntry as FreemodePotentialGangMember) != null;
            }


            for (int i = 0; i < memberList.Count; i++)
            {
                if (memberList[i].modelHash == potentialEntry.modelHash &&
                    memberList[i].hairDrawableIndex == potentialEntry.hairDrawableIndex &&
                    memberList[i].headDrawableIndex == potentialEntry.headDrawableIndex &&
                    memberList[i].headTextureIndex == potentialEntry.headTextureIndex &&
                    memberList[i].legsDrawableIndex == potentialEntry.legsDrawableIndex &&
                    memberList[i].legsTextureIndex == potentialEntry.legsTextureIndex &&
                    memberList[i].torsoDrawableIndex == potentialEntry.torsoDrawableIndex &&
                    memberList[i].torsoTextureIndex == potentialEntry.torsoTextureIndex)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// gets a similar entry to the member provided.
        /// it may not be the only similar one, however
        /// </summary>
        /// <param name="potentialEntry"></param>
        /// <returns></returns>
        public PotentialGangMember GetSimilarEntry(PotentialGangMember potentialEntry)
        {
            if (potentialEntry.GetType() == typeof(FreemodePotentialGangMember))
            {
                return FreemodePotentialGangMember.FreemodeSimilarEntryCheck(potentialEntry as FreemodePotentialGangMember);
            }

            for (int i = 0; i < memberList.Count; i++)
            {
                if (memberList[i].modelHash == potentialEntry.modelHash &&
                    (memberList[i].hairDrawableIndex == -1 || memberList[i].hairDrawableIndex == potentialEntry.hairDrawableIndex) &&
                    (memberList[i].headDrawableIndex == -1 || memberList[i].headDrawableIndex == potentialEntry.headDrawableIndex) &&
                    (memberList[i].headTextureIndex == -1 || memberList[i].headTextureIndex == potentialEntry.headTextureIndex) &&
                   (memberList[i].legsDrawableIndex == -1 || memberList[i].legsDrawableIndex == potentialEntry.legsDrawableIndex) &&
                    (memberList[i].legsTextureIndex == -1 || memberList[i].legsTextureIndex == potentialEntry.legsTextureIndex) &&
                   (memberList[i].torsoDrawableIndex == -1 || memberList[i].torsoDrawableIndex == potentialEntry.torsoDrawableIndex) &&
                    (memberList[i].torsoTextureIndex == -1 || memberList[i].torsoTextureIndex == potentialEntry.torsoTextureIndex))
                {
                    return memberList[i];
                }
            }
            return null;
        }
    }

}

