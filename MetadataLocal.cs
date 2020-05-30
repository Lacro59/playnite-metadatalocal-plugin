using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MetadataLocal
{
    public class MetadataLocal : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private MetadataLocalSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("ffb390b2-758f-40ac-9b20-9be08fd05a65");

        public string PlayniteConfigurationPath { get; set; }


        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Description
            // Include addition fields if supported by the metadata source
        };

        // Change to something more appropriate
        public override string Name => "Metadata Local";

        public MetadataLocal(IPlayniteAPI api) : base(api)
        {
            settings = new MetadataLocalSettings(this);
            PlayniteConfigurationPath = api.Paths.ConfigurationPath;
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new MetadataLocalProvider(options, this, PlayniteConfigurationPath);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        //public override UserControl GetSettingsView(bool firstRunSettings)
        //{
        //    return new MetadataLocalSettingsView();
        //}
    }
}