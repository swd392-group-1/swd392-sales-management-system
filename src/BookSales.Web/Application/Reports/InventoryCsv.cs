using System.Globalization;
using System.Text;
namespace BookSales.Application.Reports;

public static class InventoryCsv
{
    private static string Cell(string? value)
    {
        value ??= "";
        // Spreadsheet formula protection applies also after leading whitespace.
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && "=+-@".Contains(trimmed[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    public static byte[] Export(InventoryReport report)
    {
        var csv = new StringBuilder("Book ID,Title,Current price,Quantity\r\n");
        foreach (var row in report.Rows)
            csv.Append(Cell(row.BookId)).Append(',').Append(Cell(row.Title)).Append(',')
                .Append(row.CurrentPrice?.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.Quantity?.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }
}
