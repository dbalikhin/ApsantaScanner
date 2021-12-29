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


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ApsantaScanner.Security.Utils
{
    internal class CodeFixUtil
    {
        /// <summary>
        /// Extract the last indentation from the trivia passed.
        /// </summary>
        /// <param name="leadingTrivia"></param>
        /// <returns></returns>
        public static SyntaxTriviaList KeepLastLine(SyntaxTriviaList leadingTrivia)
        {
            SyntaxTriviaList triviaBuild = SyntaxTriviaList.Empty;
            foreach (SyntaxTrivia trivium in leadingTrivia.Reverse())
            {
                if (!trivium.IsKind(SyntaxKind.WhitespaceTrivia))
                    continue;

                triviaBuild = triviaBuild.Insert(0, trivium);
                break;
            }

            return triviaBuild;
        }
    }
}
