using MetadataLocal.Views;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using CommonPluginsShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace MetadataLocal
{
    public class MetadataLocal : MetadataPlugin
    {
        private MetadataLocalSettingsViewModel PluginSettings { get; set; }

        public override Guid Id { get; } = Guid.Parse("ffb390b2-758f-40ac-9b20-9be08fd05a65");

        public string PlayniteConfigurationPath { get; set; }

        // Include addition fields if supported by the metadata source
        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Description
        };

        // Change to something more appropriate
        public override string Name => "MetadataLocal";


        public MetadataLocal(IPlayniteAPI api) : base(api)
        {
            PluginSettings = new MetadataLocalSettingsViewModel(this);
            PlayniteConfigurationPath = api.Paths.ConfigurationPath;

            // Get plugin's location 
            string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Set the common resourses & event
            Common.Load(PluginFolder, PlayniteApi.ApplicationSettings.Language);
            Common.SetEvent(PlayniteApi);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new MetadataLocalProvider(options, this, PlayniteConfigurationPath, PluginSettings.Settings);
        }


        #region Settings
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return PluginSettings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new MetadataLocalSettingsView();
        }
        #endregion
    }
}
