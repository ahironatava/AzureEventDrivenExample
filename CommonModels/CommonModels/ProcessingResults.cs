namespace CommonModels
{
    public class ProcessingResults
    {
        public string RequestId { get; set; }
        public bool ProcessingSuccessful { get; set; }
        public string StatusMessage { get; set; }
        public string UserName { get; set; }
        public string StockName { get; set; }
        public string TransactionType { get; set; }
        public int Quantity { get; set; }
        public int BalanceBefore { get; set; }
        public int BalanceAfter { get; set; }
    }
}

