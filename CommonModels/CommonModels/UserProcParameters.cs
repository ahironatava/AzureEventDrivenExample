namespace CommonModels
{
    public class UserProcParameters
    {
        public bool AccountIsLocked { get; set; }
        public Dictionary<string, bool> AccessibleStocks { get; set; }
        public List<string> Restrictions { get; set; }
    }
}
