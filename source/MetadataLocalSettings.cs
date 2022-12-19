using CommonPluginsShared.Extensions;
using MetadataLocal.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace MetadataLocal
{
    public class MetadataLocalSettings : ObservableObject
    {
        #region Settings variables
        public bool EnableSelectStore { get; set; } = false;

        public List<Store> Stores { get; set; } = new List<Store>();
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed

        #endregion  
    }


    public class MetadataLocalSettingsViewModel : ObservableObject, ISettings
    {
        private readonly MetadataLocal Plugin;
        private MetadataLocalSettings EditingClone { get; set; }

        private MetadataLocalSettings _Settings;
        public MetadataLocalSettings Settings { get => _Settings; set => SetValue(ref _Settings, value); }


        public MetadataLocalSettingsViewModel(MetadataLocal plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            MetadataLocalSettings savedSettings = plugin.LoadPluginSettings<MetadataLocalSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            Settings = savedSettings != null ? savedSettings : new MetadataLocalSettings();

            if (Settings.Stores.Count == 0)
            {
                Settings.Stores.Add(new Store { Name = "Steam" });
                Settings.Stores.Add(new Store { Name = "Epic" });
                Settings.Stores.Add(new Store { Name = "EA app" });
                Settings.Stores.Add(new Store { Name = "Xbox" });
                Settings.Stores.Add(new Store { Name = "Ubisoft Connect" });
                Settings.Stores.Add(new Store { Name = "GOG" });
            }
            else
            {
                if (Settings.Stores.Find(x => x.Name.IsEqual("Origin")) != null)
                {
                    Settings.Stores.Find(x => x.Name.IsEqual("Origin")).Name = "EA app";
                }

                if (Settings.Stores.Find(x => x.Name.IsEqual("Ubisoft")) != null)
                {
                    Settings.Stores.Find(x => x.Name.IsEqual("Ubisoft")).Name = "Ubisoft Connect";
                }

                if (Settings.Stores.Find(x => x.Name.IsEqual("GOG")) == null)
                {
                    Settings.Stores.Add(new Store { Name = "GOG" });
                }
            }
        }

        // Code executed when settings view is opened and user starts editing values.
        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
        }

        // Code executed when user decides to cancel any changes made since BeginEdit was called.
        // This method should revert any changes made to Option1 and Option2.
        public void CancelEdit()
        {
            Settings = EditingClone;
        }

        // Code executed when user decides to confirm changes made since BeginEdit was called.
        // This method should save settings made to Option1 and Option2.
        public void EndEdit()
        {
            Plugin.SavePluginSettings(Settings);
        }

        // Code execute when user decides to confirm changes made since BeginEdit was called.
        // Executed before EndEdit is called and EndEdit is not called if false is returned.
        // List of errors is presented to user if verification fails.
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
