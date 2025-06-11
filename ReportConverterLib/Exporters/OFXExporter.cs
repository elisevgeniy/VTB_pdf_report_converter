using System.Xml.Linq;
using ReportConverterLib.Models;

namespace ReportConverterLib.Exporters;

internal class OFXExporter : IExporter
{
    public string ExportToString(Account account)
    {
        var output = new XDocument(
            new XProcessingInstruction("OFX",
                "OFXHEADER=\"200\" VERSION=\"220\" SECURITY=\"NONE\" OLDFILEUID=\"NONE\" NEWFILEUID=\"NONE\""),
            new XElement("OFX",
                new XElement("BANKMSGSRSV1",
                    new XElement("STMTTRNRS",
                        new XElement("STMTRS",
                            new XElement("CURDEF", account.Currency),
                            new XElement("BANKACCTFROM",
                                new XElement("BANKID", "VTB"),
                                new XElement("ACCTID", account.Number),
                                new XElement("ACCTTYPE", "UNKNOWN")
                            ),
                            new XElement("BANKTRANLIST",
                                from transaction in account.Transactions orderby transaction.DateTime
                                select new XElement("STMTTRN",
                                    new XElement("DTPOSTED",
                                        transaction.DateTime.ToString("yyyyMMddHHmmss")),
                                    new XElement("TRNAMT", transaction.Amount),
                                    new XElement("NAME", transaction.Payee),
                                    new XElement("MEMO", transaction.Memo)
                                )
                            )
                        )
                    )
                )
            )
        );

        return output.ToString();
    }
}