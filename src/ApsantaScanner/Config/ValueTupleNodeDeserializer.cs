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
using ApsantaScanner;
using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace ApsantaScanner.Config
{
    internal class ValueTupleNodeDeserializer : INodeDeserializer
    {
        public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            if (expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
            {
                var pairArgs = expectedType.GetGenericArguments();
                var args = new object[pairArgs.Length];

                parser.Consume<MappingStart>();

                for (int i = 0; i < pairArgs.Length; ++i)
                {
                    var scalar = parser.Consume<Scalar>();
                    var stringValue = scalar.Value;

                    if (pairArgs[i] == typeof(int))
                        args[i] = int.Parse(stringValue);
                    else if (stringValue == "True")
                        args[i] = true;
                    else if (stringValue == "False")
                        args[i] = false;
                    else if (pairArgs[i] == typeof(object) && scalar.Style == ScalarStyle.Plain)
                        args[i] = int.Parse(stringValue);
                    else
                        args[i] = stringValue;
                }

                parser.Consume<MappingEnd>();

                value = Activator.CreateInstance(expectedType, args);
                return true;
            }

            value = null;
            return false;
        }
    }
}
