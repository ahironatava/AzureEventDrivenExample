﻿namespace FakeDatabase
{
    public interface IFakeDatabase
    {
        public int GetStockBalance(string userName, string stockName);
        public void AddStockBalance(string userName, string stockName, int quantity);
        public void DeductStockBalance(string userName, string stockName, int quantity);
    }
}