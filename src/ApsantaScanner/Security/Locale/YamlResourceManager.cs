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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using YamlDotNet.RepresentationModel;

namespace ApsantaScanner.Security.Locale
{
    internal class YamlResourceManager : ResourceManager
    {
        private const string MessagesFileName = "Messages.yml";

        private readonly IDictionary<string, string> LocaleString = new Dictionary<string, string>();
        public IReadOnlyList<string> LocaleKeyIds => _LocaleKeyIds;
        private readonly List<string> _LocaleKeyIds = new List<string>();

        public YamlResourceManager() : base("ApsantaScanner.Empty",
                                            typeof(YamlResourceManager).GetTypeInfo().Assembly)
        {
            Load();
        }

        private void Load()
        {
            var assembly = typeof(YamlResourceManager).GetTypeInfo().Assembly;

            using (Stream stream = assembly.GetManifestResourceStream("ApsantaScanner.Security.Config." + MessagesFileName))
            using (var reader = new StreamReader(stream))
            {
                var yaml = new YamlStream();
                yaml.Load(reader);

                var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

                foreach (var entry in mapping.Children)
                {
                    var key = (YamlScalarNode)entry.Key;
                    var value = (YamlMappingNode)entry.Value;

                    _LocaleKeyIds.Add(key.Value);

                    foreach (var child in value.Children)
                    {
                        LocaleString[$"{key.Value}_{child.Key}"] = ((YamlScalarNode)child.Value).Value;
                    }
                }
            }
        }

        public new string GetString(string name)
        {
            return GetString(name, CultureInfo.CurrentCulture);
        }

        public override string GetString(string name, CultureInfo culture)
        {
            if (!LocaleString.TryGetValue(name, out string val))
                return "??" + name + "??";

            return val;
        }
    }
}
