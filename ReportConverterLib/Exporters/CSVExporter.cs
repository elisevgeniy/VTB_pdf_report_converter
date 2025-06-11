using System.Globalization;
using System.Text;
using CsvHelper;
using ReportConverterLib.Models;

namespace ReportConverterLib.Exporters;

internal class CSVExporter : IExporter
{
    public string ExportToString(Account account)
    {
        var result = new StringBuilder();
        var writer = new StringWriter(result);
        using (var csv = new CsvWriter(writer, CultureInfo.GetCultureInfo("ru-RU")))
        {
            csv.WriteRecords(account.Transactions);
        }
        return result.ToString();
    }
}