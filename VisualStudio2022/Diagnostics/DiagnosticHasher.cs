using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudio2022.Diagnostics
{
    internal class DiagnosticHasher
    {
        /*
         * DiagnosticData: https://github.com/dotnet/roslyn/blob/main/src/Workspaces/Core/Portable/Diagnostics/DiagnosticData.cs#L175-#L184
        public override int GetHashCode()
            => Hash.Combine(Id,
               Hash.Combine(Category,
               Hash.Combine(Message,
               Hash.Combine(WarningLevel,
               Hash.Combine(IsSuppressed,
               Hash.Combine(ProjectId,
               Hash.Combine(DocumentId,
               Hash.Combine(DataLocation?.OriginalStartLine ?? 0,
               Hash.Combine(DataLocation?.OriginalStartColumn ?? 0, (int)Severity)))))))));
			   
			   
			   
DiagnosticTableItem.cs: https://github.com/dotnet/roslyn/blob/main/src/VisualStudio/Core/Def/Implementation/TableDataSource/DiagnosticTableItem.cs#L121-L141
            public int GetHashCode(DiagnosticData data)
            {
                var location = data.DataLocation;

                // location-less or project level diagnostic:
                if (location == null ||
                    location.OriginalFilePath == null ||
                    data.DocumentId == null)
                {
                    return data.GetHashCode();
                }

                return
                    Hash.Combine(location.OriginalStartColumn,
                    Hash.Combine(location.OriginalStartLine,
                    Hash.Combine(location.OriginalEndColumn,
                    Hash.Combine(location.OriginalEndLine,
                    Hash.Combine(location.OriginalFilePath,
                    Hash.Combine(data.IsSuppressed,
                    Hash.Combine(data.Id, data.Severity.GetHashCode())))))));
            }
        */
    }
}
