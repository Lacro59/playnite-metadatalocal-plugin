using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetadataLocal
{
    class CodeLang
    {
        //https://partner.steamgames.com/doc/store/localization?#supported_languages
        public static string GetSteamLang(string PlayniteLanguage)
        {
            string SteamLang = "english";

            switch (PlayniteLanguage)
            {
                case "ar_SA":
                    SteamLang = "arabic";
                    break;
                case "ca_ES":
                    SteamLang = "arabic";
                    break;
                case "cs_CZ":
                    SteamLang = "czech";
                    break;
                case "de_DE":
                    SteamLang = "german";
                    break;
                case "el_GR":
                    SteamLang = "greek";
                    break;
                case "es_ES":
                    SteamLang = "spanish";
                    break;
                case "fi_FI":
                    SteamLang = "finnish";
                    break;
                case "fr_FR":
                    SteamLang = "french";
                    break;
                case "he_IL":
                    break;
                case "hu_HU":
                    SteamLang = "hungarian";
                    break;
                case "id_ID":
                    break;
                case "it_IT":
                    SteamLang = "italian";
                    break;
                case "ja_JP":
                    SteamLang = "japanese";
                    break;
                case "ko_KO":
                    SteamLang = "koreana";
                    break;
                case "nl_NL":
                    SteamLang = "dutch";
                    break;
                case "no_NO":
                    SteamLang = "norwegian";
                    break;
                case "pl_PL":
                    SteamLang = "polish";
                    break;
                case "pt_BR":
                    SteamLang = "brazilian";
                    break;
                case "pt_PT":
                    break;
                case "ro_RO":
                    SteamLang = "romanian";
                    break;
                case "ru_RU":
                    SteamLang = "russian";
                    break;
                case "sv_SE":
                    SteamLang = "swedish";
                    break;
                case "tr_TR":
                    SteamLang = "turkish";
                    break;
                case "uk_UA":
                    SteamLang = "ukrainian";
                    break;
                case "vi_VN":
                case "zh_CN":
                    SteamLang = "schinese";
                    break;
                case "zh_TW":
                    SteamLang = "tchinese";
                    break;
            }

            return SteamLang;
        }

        
        // If not exist, Oring return english data.
        public static string GetOriginLang(string PlayniteLanguage)
        {
            return PlayniteLanguage;
        }

        public static string GetOriginLangCountry(string PlayniteLanguage)
        {
            return PlayniteLanguage.Substring((PlayniteLanguage.Length - 2));
        }

        // If not exist, Epic return english data.
        public static string GetEpicLang(string PlayniteLanguage)
        {
            if (PlayniteLanguage == "english")
            {
                PlayniteLanguage = "en_US";
            }
            return PlayniteLanguage.Replace("_", "-");
        }

        public static string GetEpicLangCountry(string PlayniteLanguage)
        {
            if (PlayniteLanguage == "english")
            {
                PlayniteLanguage = "en_US";
            }
            return PlayniteLanguage.Substring(0, 2);
        }
    }
}
