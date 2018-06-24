using Newtonsoft.Json;
using StardewModdingAPI;
using System.IO;

namespace Kisekae.Config
{
    /// <summary>The JSON model for a per-save settings file.</summary>
    public class LocalConfig
    {
        /*********
        ** Properties
        *********/
        /// <summary>Global Mod Interface.</summary>
        [JsonIgnore]
        public static IMod s_env { get; set; } = null;
        /// <summary>The relative path to the current per-save config file, or <c>null</c> if the save isn't loaded yet.</summary>
        [JsonIgnore]
        public static string s_perSaveConfigPath
            => Constants.SaveFolderName != null
            ? Path.Combine("psconfigs", $"{Constants.SaveFolderName}.json")
            : null;
        /// <summary>The maximum number of favorites.</summary>
        [JsonIgnore]
        private const int s_nConfigs = 37;

        /****
        ** Metadata
        ****/
        /// <summary>The name of the associated save.</summary>
        [JsonIgnore]
        public string SaveName { get; set; }
        /// <summary>Whether this save hasn't been loaded with Kisekae yet.</summary>
        public bool FirstRun { get; set; } = true;
        /// <summary>Make other can see customized appearance.</summary>
        public bool MutiplayerFix { get; set; } = false;

        /****
        ** Settings
        ****/
        public enum Attributes{
            Skin = 0,
            Hair,
            Shirt,
            Accessory,
            EyeColor,
            HairColor,
            BottomsColor,
            Face,
            Nose,
            Bottoms,
            Shoes,
            ShoeColor,
            nAttributes
        }
        #region Settings
        /// <summary>The accessory chosen for each favorite.</summary>
        public int[] ChosenAccessory = new int[s_nConfigs];
        /// <summary>The face chosen for each favorite.</summary>
        public int[] ChosenFace = new int[s_nConfigs];
        /// <summary>The nose chosen for each favorite.</summary>
        public int[] ChosenNose = new int[s_nConfigs];
        /// <summary>The bottom chosen for each favorite.</summary>
        public int[] ChosenBottoms = new int[s_nConfigs];
        /// <summary>The shoes chosen for each favorite.</summary>
        public int[] ChosenShoes = new int[s_nConfigs];
        /// <summary>The skin chosen for each favorite.</summary>
        public int[] ChosenSkin = new int[s_nConfigs];
        /// <summary>The shirt chosen for each favorite.</summary>
        public int[] ChosenShirt = new int[s_nConfigs];
        /// <summary>The hair style chosen for each favorite.</summary>
        public int[] ChosenHairstyle = new int[s_nConfigs];
        /// <summary>The hair color for each favorite.</summary>
        public uint[] ChosenHairColor = new uint[s_nConfigs];
        /// <summary>The eye color chosen for each favorite.</summary>
        public uint[] ChosenEyeColor = new uint[s_nConfigs];
        /// <summary>The bottom color chosen for each favorite.</summary>
        public uint[] ChosenBottomsColor = new uint[s_nConfigs];
        /// <summary>The shoe color chosen for each favorite.</summary>
        public int[] ChosenShoeColor = new int[s_nConfigs];
        #endregion

        /****
        ** Public functions
        ****/
        /// <summary>Unified int attribute getter.</summary>
        public int GetIntAttribute(Attributes attr, int which = 0) {
            switch (attr) {
                case Attributes.Skin:
                    return ChosenSkin[which];
                case Attributes.Hair:
                    return ChosenHairstyle[which];
                case Attributes.Shirt:
                    return ChosenShirt[which];
                case Attributes.Accessory:
                    return ChosenAccessory[which];
                case Attributes.EyeColor:
                    return (int)ChosenEyeColor[which];
                case Attributes.HairColor:
                    return (int)ChosenHairColor[which];
                case Attributes.BottomsColor:
                    return (int)ChosenBottomsColor[which];
                case Attributes.Face:
                    return ChosenFace[which];
                case Attributes.Nose:
                    return ChosenNose[which];
                case Attributes.Bottoms:
                    return ChosenBottoms[which];
                case Attributes.Shoes:
                    return ChosenShoes[which];
                case Attributes.ShoeColor:
                    return ChosenShoeColor[which];
                default:
                    return 0;
            }
        }

        /// <summary>Unified uint attribute getter.</summary>
        public uint GetUintAttribute(Attributes attr, int which = 0) {
            switch (attr) {
                case Attributes.Skin:
                    return (uint)ChosenSkin[which];
                case Attributes.Hair:
                    return (uint)ChosenHairstyle[which];
                case Attributes.Shirt:
                    return (uint)ChosenShirt[which];
                case Attributes.Accessory:
                    return (uint)ChosenAccessory[which];
                case Attributes.EyeColor:
                    return ChosenEyeColor[which];
                case Attributes.HairColor:
                    return ChosenHairColor[which];
                case Attributes.BottomsColor:
                    return ChosenBottomsColor[which];
                case Attributes.Face:
                    return (uint)ChosenFace[which];
                case Attributes.Nose:
                    return (uint)ChosenNose[which];
                case Attributes.Bottoms:
                    return (uint)ChosenBottoms[which];
                case Attributes.Shoes:
                    return (uint)ChosenShoes[which];
                case Attributes.ShoeColor:
                    return (uint)ChosenShoeColor[which];
                default:
                    return 0;
            }
        }

        /// <summary>Get whether a favorite is defined.</summary>
        /// <param name="favoriteSlot">The favorite index to check.</param>
        public bool HasFavSlot(int favoriteSlot) {
            if (favoriteSlot <= 0 || favoriteSlot > ChosenEyeColor.Length)
                return false;
            return ChosenEyeColor[favoriteSlot] != 0;
        }

        public bool IsCurConfigOldVersion() {
            return (ChosenEyeColor[0] == 0);
        }

        public void save() {
            if (s_env == null) {
                return;
            }
            if (SaveName != null) {
                s_env.Monitor.Log("Save a config:"+ Path.Combine("psconfigs", $"{SaveName}.json"));
                s_env.Helper.WriteJsonFile(Path.Combine("psconfigs", $"{SaveName}.json"), this);
            } else if (s_perSaveConfigPath != null) {
                s_env.Monitor.Log("Save the config:"+ s_perSaveConfigPath);
                s_env.Helper.WriteJsonFile(s_perSaveConfigPath, this);
            }
        }
    }
}
