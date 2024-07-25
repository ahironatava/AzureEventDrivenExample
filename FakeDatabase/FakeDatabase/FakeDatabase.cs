using CommonModels;

namespace FakeDatabase
{
    public class FakeDatabase : IFakeDatabase
    {
        // Class provides a basic simulation of a database
        // There is only one user to be supported: "fake name", as created in the Facacde API

        private List<UserAccount> _userAccounts;

        public FakeDatabase()
        {
            // Constructor
            var userStocks = new Dictionary<string, int>();
            userStocks.Add("AAPL", 10);
            userStocks.Add("MSFT", 10);
            userStocks.Add("AMZN", 10);

            var userAccount = new UserAccount()
            {
                UserName = "fake name",
                StockBalances = userStocks
            };

            _userAccounts = new List<UserAccount>();
            _userAccounts.Add(userAccount);
        }

        public int GetStockBalance(string userName, string stockName)
        {
            // Get the stock balance for the user
            var userAccount = _userAccounts.FirstOrDefault(u => u.UserName == userName);
            if (userAccount == null)
            {
                throw new Exception($"User not found: {userName}");
            }

            if (userAccount.StockBalances.ContainsKey(stockName))
            {
                return userAccount.StockBalances[stockName];
            }
            else
            {
                return 0;
            }
        }

        public void AddStockBalance(string userName, string stockName, int quantity)
        {
            // Update the stock balance for the user
            var userAccount = _userAccounts.FirstOrDefault(u => u.UserName == userName);
            if (userAccount == null)
            {
                throw new Exception($"User not found: {userName}");
            }

            if (userAccount.StockBalances.ContainsKey(stockName))
            {
                userAccount.StockBalances[stockName] += quantity;
            }
            else
            {
                userAccount.StockBalances.Add(stockName, quantity);
            }
        }

        public void DeductStockBalance(string userName, string stockName, int quantity)
        {
            // Update the stock balance for the user
            var userAccount = _userAccounts.FirstOrDefault(u => u.UserName == userName);
            if (userAccount == null)
            {
                throw new Exception($"User not found: {userName}");
            }

            if (userAccount.StockBalances.ContainsKey(stockName))
            {
                if(userAccount.StockBalances[stockName] < quantity)
                {
                    throw new Exception($"Insufficient stock balance, stock: {stockName}, user: {userName}");
                }
                userAccount.StockBalances[stockName] -= quantity;
            }
            else
            {
                throw new Exception("Stock not found");
            }
        }

    }
}
