using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using CsvHelper;
using Tabula;
using Tabula.Extractors;
using UglyToad.PdfPig;
using ReportConverterLib.Exceptions;
using ReportConverterLib.Models;
using Transaction = ReportConverterLib.Models.Transaction;

namespace ReportConverterLib.Converter
{
    public class ReportConverter
    {
        private Accaunt Accaunt { get; set; }

        public ReportConverter(string pdfFilepath)
        {
            ReadPdfFile(pdfFilepath);
        }

        private void ReadPdfFile(string filepath)
        {
            using (PdfDocument document = PdfDocument.Open(filepath, new ParsingOptions() { ClipPaths = true }))
            {
                Accaunt = ParseAccount(document);
                Accaunt.Transactions.AddRange(ParseTransactions(document));
            }
        }

        public string GetOFX()
        {
            var output = new XDocument(
                new XProcessingInstruction("OFX",
                    "OFXHEADER=\"200\" VERSION=\"220\" SECURITY=\"NONE\" OLDFILEUID=\"NONE\" NEWFILEUID=\"NONE\""),
                new XElement("OFX",
                    new XElement("BANKMSGSRSV1",
                        new XElement("STMTTRNRS",
                            new XElement("STMTRS",
                                new XElement("CURDEF", Accaunt.Currency),
                                new XElement("BANKACCTFROM",
                                    new XElement("BANKID", "VTB"),
                                    new XElement("ACCTID", Accaunt.Number),
                                    new XElement("ACCTTYPE", "UNKNOWN")
                                ),
                                new XElement("BANKTRANLIST",
                                    from transaction in Accaunt.Transactions orderby transaction.DateTime
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

        public string GetCSV()
        {
            var result = new StringBuilder();
            var writer = new StringWriter(result);
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
               csv.WriteRecords(Accaunt.Transactions);
            }
            return result.ToString();
        }

        public string SaveOFXToFile(string ofxFilepath)
        {
            return SaveStringToFile(ofxFilepath, GetOFX());
        }

        public string SaveCSVToFile(string csvFilepath)
        {            
            return SaveStringToFile(csvFilepath, GetCSV());
        }

        private string SaveStringToFile(string filepath, string content)
        {
            using (var file = File.CreateText(filepath))
            {
                file.Write(content);
            }
            
            return filepath;
        }

        private static Accaunt ParseAccount(PdfDocument document)
        {
            try
            {
                var words = document.GetPage(1).GetWords().ToArray();
                int words_index = 0;


                string FIO = "";
                StringBuilder sb = new StringBuilder();
                while (!words[words_index].Text.Equals("Номер"))
                {
                    sb.Append(words[words_index].Text + " ");
                    words_index++;
                }

                FIO = sb.ToString().Trim();


                decimal accauntNumber;
                while (!words[words_index].Text.Equals("счёта"))
                {
                    words_index++;
                }

                words_index++;
                if (decimal.TryParse(words[words_index].Text, out decimal number))
                {
                    accauntNumber = number;
                }
                else
                {
                    throw new ConvertException("Не удалось спарсить номер счёта");
                }

                DateOnly startPeriod;
                DateOnly endPeriod;
                while (!words[words_index].Text.Equals("выписки"))
                {
                    words_index++;
                }

                words_index++;
                string startStr = words[words_index].Text;
                words_index += 2;
                string endStr = words[words_index].Text;
                if (DateOnly.TryParse(startStr, out DateOnly start) && DateOnly.TryParse(endStr, out DateOnly end))
                {
                    startPeriod = start;
                    endPeriod = end;
                }
                else
                {
                    throw new ConvertException("Не удалось спарсить даты выписки");
                }

                string currency;
                double startBalance;
                double endBalance;
                while (!words[words_index].Text.Equals("периода"))
                {
                    words_index++;
                }

                words_index++;

                string startBalanceStr = words[words_index].Text.Replace(",", "").Replace(".", ",");
                currency = words[words_index + 1].Text;
                while (!words[words_index].Text.Equals("периода"))
                {
                    words_index++;
                }

                words_index++;
                string endBalanceStr = words[words_index].Text.Replace(",", "").Replace(".", ",");
                if (double.TryParse(startBalanceStr, out double startB) &&
                    double.TryParse(endBalanceStr, out double endB))
                {
                    startBalance = startB;
                    endBalance = endB;
                }
                else
                {
                    throw new ConvertException("Не удалось спарсить баланс");
                }

                return new Accaunt(FIO, accauntNumber, startPeriod, endPeriod, startBalance, endBalance, currency);
            }
            catch (Exception ex)
            {
                throw new ConvertException(ex.Message);
            }
        }

        private static List<Transaction> ParseTransactions(PdfDocument document)
        {
            try
            {
                var transactions = new List<Transaction>();

                for (int i = 1; i <= document.NumberOfPages; i++)
                {
                    PageArea page = ObjectExtractor.Extract(document, i);

                    IExtractionAlgorithm ea = new SpreadsheetExtractionAlgorithm();
                    IReadOnlyList<Table> tables = ea.Extract(page);

                    if (tables.Count == 0) continue;
                    
                    Table table;
                    int r = 2;
                    if (i == 1)
                    {
                        table = tables[1];
                        r = 4;
                    }
                    else
                    {
                        table = tables[0];
                    }

                    for (; r < table.RowCount; r++)
                    {
                        if (
                            DateTime.TryParse(table[r, 0].ToString().Replace("\r", " ").Replace('.', '-'),
                                out DateTime dateTime) &&
                            DateOnly.TryParse(table[r, 1].ToString(), out DateOnly bankExecuteDate) &&
                            double.TryParse(
                                table[r, 2].ToString().Replace(" RUB", "").Replace(",", "").Replace('.', ','),
                                out double amount) &&
                            double.TryParse(
                                table[r, 5].ToString().Replace(" RUB", "").Replace(",", "").Replace('.', ','),
                                out double commission)
                        )
                        {
                            string memo = table[r, 6].ToString();
                            if (TryParsePayee(memo, out string payee))
                            {
                                transactions.Add(new Transaction(dateTime, bankExecuteDate, amount, commission, memo, payee));
                            }
                            else
                            {
                                transactions.Add(new Transaction(dateTime, bankExecuteDate, amount, commission, memo));
                            }
                            
                        }
                        else
                        {
                            throw new ConvertException($"Transaction parse error, page {i}, row {r}");
                        }
                    }
                }

                return transactions;
            }
            catch (Exception ex)
            {
                throw new ConvertException(ex.Message);
            }
        }

        private static bool TryParsePayee(string memo, out string payee)
        {
            payee = "";
            if (memo.StartsWith("Оплата товаров и услуг. "))
            {
                var payeeRange = new Range(24, memo.Replace("\r_\r"," ").Replace("\r"," ").IndexOf("по карте"));
                payee = memo[payeeRange].Trim().Replace("\r"," ");
                return true;
            }
            return false;
        }
    }
}