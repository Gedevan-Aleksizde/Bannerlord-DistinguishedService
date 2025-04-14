using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DistinguishedServiceRedux
{
    public static class NameList
    {
        private const string EXTERNAL_NAME_FILE = "external_namelist.txt";
        private static readonly string filePath = Path.Combine(TaleWorlds.ModuleManager.ModuleHelper.GetModuleFullPath(SubModule.moduleName), EXTERNAL_NAME_FILE);
        public static bool IsFileExists()
        {
            return File.Exists(filePath);
        }
        /// <summary>
        /// Get a name format string randomly
        /// random name is determined by IsRanged and FirstBattleEquipment propertyies.
        /// </summary>
        public static TextObject DrawNameFormat(CharacterObject chracater)
        {

            List<TextObject> formats = new();
            string troopType = "infantly";
            if (chracater.IsRanged)
            {
                troopType = "ranged";
            }
            else if (chracater.FirstBattleEquipment[EquipmentIndex.Horse].IsEmpty)
            {
                troopType = "cavalry";
            }
            formats.AppendList(GameTexts.FindAllTextVariations($"DistServ_name_format_{troopType}").ToList());
            formats.AppendList(GameTexts.FindAllTextVariations($"DistServ_name_format_culture_{chracater.Culture.StringId}").ToList());
            if (formats.Count == 0) return GameTexts.FindText("DistServ_name_format_default.fallback");
            return formats[MBRandom.RandomInt(formats.Count)];
        }
        /// <summary>
        /// Draw a user-defined name from the external text file, then **remove drawn name from the file**
        /// </summary>
        public static TextObject PullOutNameFromExternalFile()
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(filePath);
                    if (lines.Length == 0)
                        return new("");
                    int index = MBRandom.RandomInt(0, lines.Length);
                    string name = lines[index];
                    string newText = "";
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (i != index)
                        {
                            newText += lines[i] + "\n";
                        }
                    }
                    File.WriteAllText(filePath, newText);
                    return new(name);
                }
                catch (Exception e)
                {
                    InformationManager.DisplayMessage(new(e.Message, Colors.Red));
                    return new("");
                }

            }
            else
            {
                return new("");
            }
        }
    }

}
