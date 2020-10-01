using MetadataLocal.Views;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PluginCommon;
using PluginCommon.PlayniteResources;
using PluginCommon.PlayniteResources.API;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace MetadataLocal
{
    public class MetadataLocal : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private MetadataLocalSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("ffb390b2-758f-40ac-9b20-9be08fd05a65");

        public string PlayniteConfigurationPath { get; set; }


        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Description
            // Include addition fields if supported by the metadata source
        };

        // Change to something more appropriate
        public override string Name => "MetadataLocal";

        public MetadataLocal(IPlayniteAPI api) : base(api)
        {
            settings = new MetadataLocalSettings(this);
            PlayniteConfigurationPath = api.Paths.ConfigurationPath;


            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.ApplicationSettings.Language);

            // Check version
            if (settings.EnableCheckVersion)
            {
                CheckVersion cv = new CheckVersion();

                if (cv.Check("MetadataLocal", pluginFolder))
                {
                    cv.ShowNotification(api, "MetadataLocal - " + resources.GetString("LOCUpdaterWindowTitle"));
                }
            }
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new MetadataLocalProvider(options, this, PlayniteConfigurationPath, settings);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new MetadataLocalSettingsView();
        }
    }
}