using CommonPluginsShared.Extensions;
using System;

namespace MetadataLocal.Models
{
    /// <summary>
    /// Supported store backends for MetadataLocal description lookup and store-selection UI.
    /// </summary>
    public enum MetadataLocalStoreKind
    {
        /// <summary>Unrecognized source / store name.</summary>
        Unknown = 0,

        /// <summary>Steam.</summary>
        Steam,

        /// <summary>GOG.</summary>
        Gog,

        /// <summary>EA app (formerly Origin).</summary>
        Ea,

        /// <summary>Epic Games Store.</summary>
        Epic,

        /// <summary>Xbox / Xbox Game Pass.</summary>
        Xbox,

        /// <summary>Ubisoft Connect (formerly Uplay).</summary>
        Ubisoft
    }

    /// <summary>
    /// Resolves a Playnite source or store label to a <see cref="MetadataLocalStoreKind"/>
    /// using case-insensitive equality or one-way containment aliases.
    /// </summary>
    public static class MetadataLocalStoreResolver
    {
        /// <summary>
        /// Maps <paramref name="storeName"/> to a store kind (aliases and partial match).
        /// Returns <see cref="MetadataLocalStoreKind.Unknown"/> when empty or unrecognized.
        /// </summary>
        /// <param name="storeName">Normalized or raw Playnite source / store name.</param>
        /// <returns>Resolved store kind.</returns>
        public static MetadataLocalStoreKind Resolve(string storeName)
        {
            if (storeName.IsNullOrEmpty())
            {
                return MetadataLocalStoreKind.Unknown;
            }

            string normalized = storeName.Trim();

            if (Matches(normalized, "steam"))
            {
                return MetadataLocalStoreKind.Steam;
            }

            if (Matches(normalized, "gog"))
            {
                return MetadataLocalStoreKind.Gog;
            }

            if (Matches(normalized, "ea app", "origin", "electronic arts"))
            {
                return MetadataLocalStoreKind.Ea;
            }

            if (Matches(normalized, "epic"))
            {
                return MetadataLocalStoreKind.Epic;
            }

            if (Matches(normalized, "xbox"))
            {
                return MetadataLocalStoreKind.Xbox;
            }

            if (Matches(normalized, "ubisoft", "uplay"))
            {
                return MetadataLocalStoreKind.Ubisoft;
            }

            return MetadataLocalStoreKind.Unknown;
        }

        /// <summary>
        /// Returns true when <paramref name="storeName"/> equals or contains any of <paramref name="tokens"/> (case-insensitive).
        /// One-way containment only (avoids reverse false positives on short source names).
        /// </summary>
        /// <param name="storeName">Source or store name to test.</param>
        /// <param name="tokens">Alias or keyword tokens (e.g. <c>xbox</c>, <c>origin</c>).</param>
        /// <returns><c>true</c> if at least one token matches.</returns>
        public static bool Matches(string storeName, params string[] tokens)
        {
            if (storeName.IsNullOrEmpty() || tokens == null || tokens.Length == 0)
            {
                return false;
            }

            foreach (string token in tokens)
            {
                if (token.IsNullOrEmpty())
                {
                    continue;
                }

                if (storeName.Equals(token, StringComparison.OrdinalIgnoreCase)
                    || storeName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
