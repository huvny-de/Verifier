using ClosedXML.Excel;
using System.Collections.Generic;

namespace Verifier.Models
{
    public class IrsEntity
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SSN { get; set; }
        public string DontKnow1 { get; set; }
        public string DontKnow2 { get; set; }
        public string DontKnow3 { get; set; }
        public string DontKnow4 { get; set; }
        public string DontKnow5 { get; set; }
        public string DontKnow6 { get; set; }
        public string DontKnow7 { get; set; }
    }

    public static class ExcelMapper
    {
        public static IrsEntity MapToEntity(this IXLRangeRow row)
        {
            return new IrsEntity
            {
                Id = row.Cell(1).GetValue<int>(),
                FirstName = row.Cell(2).GetString(),
                LastName = row.Cell(3).GetString(),
                SSN = row.Cell(4).GetString() + " " + row.Cell(5).GetString() + " " + row.Cell(6).GetString(),
                DontKnow1 = row.Cell(7).GetString(),
                DontKnow2 = row.Cell(8).GetString(),
                DontKnow3 = row.Cell(9).GetString(),
                DontKnow4 = row.Cell(10).GetString(),
                DontKnow5 = row.Cell(11).GetString(),
                DontKnow6 = row.Cell(12).GetString(),
                DontKnow7 = row.Cell(13).GetString(),
            };
        }

        public static IEnumerable<IrsEntity> MapIrsList(this IXLRangeRows rows)
        {
            foreach (var row in rows)
            {
                yield return MapToEntity(row);
            }
        }

    }
}
