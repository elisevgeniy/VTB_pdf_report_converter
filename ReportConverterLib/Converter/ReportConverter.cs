using System.Text;
using Tabula;
using Tabula.Extractors;
using UglyToad.PdfPig;
using ReportConverterLib.Exceptions;
using ReportConverterLib.Exporters;
using ReportConverterLib.Models;
using Transaction = ReportConverterLib.Models.Transaction;

namespace ReportConverterLib.Converter
{
    public class ReportConverter
    {
        private Account Account { get; set; }

        public ReportConverter(string pdfFilepath)
        {
            ReadPdfFile(pdfFilepath);
            SetExportFormat(FormatType.OFX);
        }
        
        private IExporter Exporter { get; set; }

        public string ConvertToString()
        {
            return Exporter.ExportToString(Account);
        }
        
        public string ConvertToFile(string path)
        {
            return Exporter.ExportToFile(path, Account);
        }

        public void SetExportFormat(FormatType formatType)
        {
            Exporter = formatType switch
            {
                FormatType.OFX => new OFXExporter(),
                FormatType.CSV => new CSVExporter(),
                FormatType.QIF => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException(nameof(formatType), formatType, null)
            };
        }
        
        private void ReadPdfFile(string filepath)
        {
            using (PdfDocument document = PdfDocument.Open(filepath, new ParsingOptions() { ClipPaths = true }))
            {
                Account = ParseAccount(document);
                Account.Transactions.AddRange(ParseTransactions(document));
            }
        }
     
        private static Account ParseAccount(PdfDocument document)
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

                return new Account(FIO, accauntNumber, startPeriod, endPeriod, startBalance, endBalance, currency);
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
                var payeeRange = new Range(24, CleanMemo(memo).IndexOf("по карте"));
                payee = memo[payeeRange].Trim().Replace("\r"," ");
                return true;
            }
            if (memo.StartsWith("Перевод между своими счетам"))
            {
                payee = "Перевод между своими счетам";
                return true;
            }
            if (memo.StartsWith("Переводы через СБП"))
            {
                payee = CleanMemo(memo).Replace("Переводы через СБП. ", "");
                return true;
            }
            if (memo.StartsWith("Поступление заработной платы"))
            {
                payee = "Поступление заработной платы";
                return true;
            }
            if (memo.StartsWith("Внесение наличных через ATM"))
            {
                payee = "Внесение наличных через ATM";
                return true;
            }
            if (memo.StartsWith("Снятие наличных в банкомате"))
            {
                payee = "Снятие наличных в банкомате";
                return true;
            }
            if (memo.StartsWith("Операции по кредитам"))
            {
                payee = CleanMemo(memo).Replace("Операции по кредитам", "");
                return true;
            }
            if (memo.StartsWith("Зачисление перевода"))
            {
                payee = CleanMemo(memo).Replace("Зачисление перевода", "");
                return true;
            }
            if (memo.StartsWith("Платежи клиентов в другие банки"))
            {
                payee = CleanMemo(memo).Replace("Платежи клиентов в другие банки", "");
                return true;
            }
            return false;
        }

        private static string CleanMemo(string memo)
        {
            return memo.Replace(". . ", "").Replace("\r_\r", " ").Replace("\r", " ");
        }
    }
}