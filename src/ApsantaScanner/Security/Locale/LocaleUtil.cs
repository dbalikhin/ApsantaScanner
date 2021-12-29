/*
 * From Security Code Scan
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ApsantaScanner.Security.Locale
{
    public class LocaleUtil
    {
        private static YamlResourceManager ResourceManager => ResourceManagerCached.Value;
        private static readonly Lazy<YamlResourceManager> ResourceManagerCached = new Lazy<YamlResourceManager>(() => new YamlResourceManager());

        public static DiagnosticDescriptor GetDescriptor(string id,
                                                         string titleId = "title",
                                                         string descriptionId = "description",
                                                         DiagnosticSeverity severity = DiagnosticSeverity.Warning,
                                                         bool isEnabledByDefault = true,
                                                         string[] args = null)
        {
            var localTitle = GetLocalString($"{id}_{titleId}");
            var localDesc = GetLocalString($"{id}_{descriptionId}");
            return new DiagnosticDescriptor(id,
                                            localTitle,
                                            localTitle,
                                            "Security",
                                            severity,
                                            isEnabledByDefault,
                                            helpLinkUri: "https://security-code-scan.github.io/#" + id,
                                            description: args == null ?
                                                             localDesc :
                                                             string.Format(localDesc.ToString(), args));
        }

        public static DiagnosticDescriptor GetDescriptorByText(string id,
                                                               string localTitle,
                                                               string localDesc,
                                                               DiagnosticSeverity severity = DiagnosticSeverity.Warning,
                                                               bool isEnabledByDefault = true,
                                                               string[] args = null)
        {
            return new DiagnosticDescriptor(id,
                                            localTitle,
                                            localTitle,
                                            "Security",
                                            severity,
                                            isEnabledByDefault,
                                            helpLinkUri: "https://security-code-scan.github.io/#" + id,
                                            description: args == null ?
                                                             localDesc :
                                                             string.Format(localDesc.ToString(), args));
        }

        public static IEnumerable<DiagnosticDescriptor> GetAllAvailableDescriptors()
        {
            var localeIds = ResourceManager.LocaleKeyIds;
            return localeIds.Select(localeId => GetDescriptor(localeId));
        }

        public static LocalizableString GetLocalString(string id)
        {
            return new LocalizableResourceString(id, ResourceManager, typeof(LocaleUtil));
        }
    }
}
