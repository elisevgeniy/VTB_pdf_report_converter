using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTBpdfReportConverter.Models
{
    internal class Transaction
    {
        public DateTime DateTime { get; }
        public DateOnly BankExecuteDate { get; }
        public double Amount { get; }
        public double Commission { get; }
        public string Memo { get; }

        Transaction(DateTime dateTime, DateOnly bankExecuteDate, double amount, double commission, string memo)
        {
            DateTime = dateTime;
            BankExecuteDate = bankExecuteDate;
            Amount = amount;
            Commission = commission;
            Memo = memo ?? throw new ArgumentNullException(nameof(memo));
        }
    }
}
