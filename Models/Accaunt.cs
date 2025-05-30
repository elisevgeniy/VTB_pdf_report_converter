using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace VTBpdfReportConverter.Models
{
    internal class Accaunt
    {
        public string Owner { get; }
        public decimal Number { get; }
        public DateOnly PeriodStart { get; }
        public DateOnly PeriodEnd { get; }
        public double BalanceAtStart { get; }
        public double BalanceAtEnd { get; }

        public List<Transaction> Transactions { get; }

        public Accaunt(string owner, decimal number, DateOnly periodStart, DateOnly periodEnd, double balanceAtStart, double balanceAtEnd)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Number = number;
            PeriodStart = periodStart;
            PeriodEnd = periodEnd;
            BalanceAtStart = balanceAtStart;
            BalanceAtEnd = balanceAtEnd;
            Transactions = new List<Transaction>();
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
