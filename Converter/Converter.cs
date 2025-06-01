using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tabula.Extractors;
using Tabula;
using UglyToad.PdfPig;
using VTBpdfReportConverter.Models;
using System.Diagnostics;

namespace VTBpdfReportConverter.Converter
{
    internal class Converter
    {
        public Converter() { }

        private Accaunt Accaunt { get; set; }

        public void ReadPdfFile(string filepath)
        {
            using (PdfDocument document = PdfDocument.Open(filepath, new ParsingOptions() { ClipPaths = true }))
            {
                Accaunt = ParseAccount(document);
                Accaunt.Transactions.AddRange(ParseTransactions(document));
            }

            Trace.WriteLine(Accaunt.ToString());
        }

        private static Accaunt ParseAccount(PdfDocument document)
        {
            var words = document.GetPage(1).GetWords().ToArray();
            int words_index = 0;


            string FIO = "";
            while (!words[words_index].Text.Equals("Номер")){
                FIO += words[words_index].Text + " ";
                words_index++;
            }
            FIO = FIO.Trim();


            decimal accauntNumber;
            while (!words[words_index].Text.Equals("счёта"))
            {
                words_index++;
            }
            words_index++;
            if (decimal.TryParse(words[words_index].Text, out decimal number))
            {
                accauntNumber = number;
            } else
            {
                throw new Exception("Не удалось спарсить номер счёта");
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
                throw new Exception("Не удалось спарсить даты выписки");
            }

            double startBalance;
            double endBalance;
            while (!words[words_index].Text.Equals("периода"))
            {
                words_index++;
            }
            words_index++;
            string startBalanceStr = words[words_index].Text.Replace(",", "").Replace(".", ",");
            while (!words[words_index].Text.Equals("периода"))
            {
                words_index++;
            }
            words_index++;
            string endBalanceStr = words[words_index].Text.Replace(",", "").Replace(".", ",");
            if (double.TryParse(startBalanceStr, out double startB) && double.TryParse(endBalanceStr, out double endB))
            {
                startBalance = startB;
                endBalance = endB;
            }
            else
            {
                throw new Exception("Не удалось спарсить баланс");
            }

            return new Accaunt(FIO, accauntNumber, startPeriod, endPeriod, startBalance, endBalance);
        }

        private static List<Transaction> ParseTransactions(PdfDocument document)
        {
            var transactions = new List<Transaction>();

            for (int i = 1; i <= document.NumberOfPages; i++)
            {
                PageArea page = ObjectExtractor.Extract(document, i);

                IExtractionAlgorithm ea = new SpreadsheetExtractionAlgorithm();
                IReadOnlyList<Table> tables = ea.Extract(page);

                Table table;
                int r = 2;
                if (i == 1)
                {
                    table = tables[1];
                    r = 4;
                } else
                {
                    table = tables[0];
                }

                for (; r < table.RowCount; r++)
                {
                    if (
                        DateTime.TryParse(table[r, 0].ToString().Replace("\r", " ").Replace('.', '-'), out DateTime dateTime) &&
                        DateOnly.TryParse(table[r, 1].ToString(), out DateOnly bankExecuteDate) &&
                        double.TryParse(table[r, 2].ToString().Replace(" RUB", "").Replace(",","").Replace('.',','), out double amount) &&
                        double.TryParse(table[r, 5].ToString().Replace(" RUB", "").Replace(",", "").Replace('.', ','), out double commission)
                        )
                    {
                        transactions.Add(new Transaction(dateTime, bankExecuteDate, amount, commission, table[r, 6].ToString()));
                    } else
                    {
                        throw new Exception($"Transaction parse error, page {i}, row {r}");
                    }
                } 
            }

            return transactions;
        }
    }
}
