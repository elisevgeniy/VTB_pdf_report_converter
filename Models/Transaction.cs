using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace VTBpdfReportConverter.Models
{
    internal class Transaction
    {
        public DateTime DateTime { get; }
        public DateOnly BankExecuteDate { get; }
        public double Amount { get; }
        public double Commission { get; }
        public string Payee { get; }
        public string Memo { get; }

        public Transaction(DateTime dateTime, DateOnly bankExecuteDate, double amount, double commission, string memo, string payee = "")
        {
            DateTime = dateTime;
            BankExecuteDate = bankExecuteDate;
            Amount = amount;
            Commission = commission;
            Memo = memo ?? throw new ArgumentNullException(nameof(memo));
            Payee = payee;
        }

        public override string? ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,  // Включаем красивое форматирование
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,  // Игнорируем null значения
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // Запрет кодирования юникода
            });
        }
    }
}
