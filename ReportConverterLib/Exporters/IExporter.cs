using ReportConverterLib.Models;

namespace ReportConverterLib.Exporters;

internal interface IExporter
{
    public string ExportToString(Account account)
    {
        return string.Empty;
    }
    public string ExportToFile(string path, Account account){
        using (var file = File.CreateText(path))
        {
            file.Write(ExportToString(account));
        }
        return path;
    }
}