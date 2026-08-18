using System.Collections.ObjectModel;

namespace MetadataLocal.Models
{
    /// <summary>
    /// Display entry for a store tile in the store-selection dialog sidebar.
    /// Brand labels stay in English on purpose (product names).
    /// </summary>
    public class StoreSelectionOption
    {
        /// <summary>
        /// Initializes a new store sidebar option.
        /// </summary>
        /// <param name="kind">Store backend kind.</param>
        /// <param name="displayName">Brand label shown under the glyph.</param>
        /// <param name="glyph">CommonFont glyph character.</param>
        public StoreSelectionOption(MetadataLocalStoreKind kind, string displayName, string glyph)
        {
            Kind = kind;
            DisplayName = displayName;
            Glyph = glyph;
        }

        /// <summary>Store backend used for multi-search.</summary>
        public MetadataLocalStoreKind Kind { get; }

        /// <summary>Brand label (English).</summary>
        public string DisplayName { get; }

        /// <summary>Icon glyph for <c>CommonFont</c>.</summary>
        public string Glyph { get; }
    }
}
