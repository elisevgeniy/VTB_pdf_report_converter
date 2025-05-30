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
                Accaunt = ParseAccaunt(document);

                //for (int i = 1; i <= document.NumberOfPages; i++)
                //{
                //    PageArea page = ObjectExtractor.Extract(document, i);

                //    IExtractionAlgorithm ea = new SpreadsheetExtractionAlgorithm();
                //    IReadOnlyList<Table> tables = ea.Extract(page);

                //    result.AppendLine($"Table count: {tables.Count}");

                //    foreach (var table in tables)
                //    {
                //        result.AppendLine($"Table rows: {table.RowCount}, columns: {table.ColumnCount}");
                //        for (int r = 0; r < table.RowCount; r++)
                //        {
                //            for (int c = 0; c < table.ColumnCount; c++)
                //            {
                //                result.Append($"{table[r, c].ToString().Replace("\r", "")}\t\t\t");
                //            }
                //            result.AppendLine();
                //            result.AppendLine();
                //        }
                //        result.AppendLine();
                //        result.AppendLine();
                //        result.AppendLine();
                //    }
                //}
            }

            Trace.WriteLine(Accaunt.ToString());
        }

        private Accaunt ParseAccaunt(PdfDocument document)
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
    }
}
