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
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Phone { get; set; }
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
                Street = row.Cell(7).GetString(),
                City = row.Cell(8).GetString(),
                ZipCode = row.Cell(9).GetString(),
                State = row.Cell(10).GetString(),
                Phone = row.Cell(11).GetString() + " " + row.Cell(12).GetString() + " " + row.Cell(13).GetString(),
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
