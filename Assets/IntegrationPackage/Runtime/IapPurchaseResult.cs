namespace PartnerIntegration
{
    public readonly struct IapPurchaseResult
    {
        public IapPurchaseResult(bool success, string key, string productId, string transactionId, string receipt, string message)
        {
            Success = success;
            Key = key;
            ProductId = productId;
            TransactionId = transactionId;
            Receipt = receipt;
            Message = message;
        }

        public bool Success { get; }
        public string Key { get; }
        public string ProductId { get; }
        public string TransactionId { get; }
        public string Receipt { get; }
        public string Message { get; }
    }
}
