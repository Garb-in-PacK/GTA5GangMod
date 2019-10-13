using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// contains names used when generating new gangs
    /// </summary>
    [Serializable]
    public class GangNamesOptions : IModOptionGroup
    {

        public GangNamesOptions() { }

        public void SetOptionsToDefault()
        {
            SetNameListsDefaultValues();
        }


        public List<string> PossibleGangFirstNames { get; private set; }

        public List<string> PossibleGangLastNames { get; private set; }

        public void SetNameListsDefaultValues()
        {

            PossibleGangFirstNames = new List<string>
            {
                "666",
                "American",
                "Angry",
                "Artful",
                "Beach",
                "Big",
                "Bloody",
                "Brazilian",
                "Bright",
                "Brilliant",
                "Business",
                "Canadian",
                "Chemical",
                "Chinese",
                "Colombian",
                "Corrupt",
                "Countryside",
                "Crazy",
                "Cursed",
                "Cute",
                "Desert",
                "Dishonored",
                "Disillusioned",
                "Egyptian",
                "Electric",
                "Epic",
                "Fake",
                "Fallen",
                "Fire",
                "Forbidden",
                "Forgotten",
                "French",
                "Gold",
                "Gothic",
                "Grave",
                "Greedy",
                "Greek",
                "Happy",
                "High",
                "High Poly",
                "Holy",
                "Ice",
                "Ice Cold",
                "Indian",
                "Irish",
                "Iron",
                "Italian",
                "Japanese",
                "Killer",
                "Laser",
                "Laughing",
                "Legendary",
                "Lordly",
                "Lost",
                "Low Poly",
                "Magic",
                "Manic",
                "Mercenary",
                "Merciless",
                "Mexican",
                "Miami",
                "Mighty",
                "Mountain",
                "Neon",
                "New",
                "New Wave",
                "Night",
                "Nihilist",
                "Nordic",
                "Original",
                "Power",
                "Poisonous",
                "Rabid",
                "Roman",
                "Robot",
                "Rocket",
                "Russian",
                "Sad",
                "Scottish",
                "Seaside",
                "Serious",
                "Shadowy",
                "Silver",
                "Snow",
                "Soviet",
                "Steel",
                "Street",
                "Swedish",
                "Sweet",
                "Tundra",
                "Turkish",
                "Vicious",
                "Vigilant",
                "Wise",
            };

            PossibleGangLastNames = new List<string>
            {
                "Bandits",
                "Barbarians",
                "Bears",
                "Cats",
                "Champions",
                "Company",
                "Coyotes",
                "Dealers",
                "Dogs",
                "Eliminators",
                "Fighters",
                "Friends",
                "Gang",
                "Gangsters",
                "Ghosts",
                "Gringos",
                "Group",
                "Gunners",
                "Hobos",
                "Hookers",
                "Hunters",
                "Industry Leaders",
                "Infiltrators",
                "Invaders",
                "Kittens",
                "League",
                "Mafia",
                "Militia",
                "Mob",
                "Mobsters",
                "Monsters",
                "Murderers",
                "Pegasi",
                "People",
                "Pirates",
                "Puppies",
                "Raiders",
                "Reapers",
                "Robbers",
                "Sailors",
                "Sharks",
                "Skull",
                "Soldiers",
                "Sword",
                "Thieves",
                "Tigers",
                "Triad",
                "Unicorns",
                "Vice",
                "Vigilantes",
                "Vikings",
                "Warriors",
                "Watchers",
                "Wolves",
                "Zaibatsu",
            };
        }
    }
}
