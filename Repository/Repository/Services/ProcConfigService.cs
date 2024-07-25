using CommonModels;
using Repository.Interfaces;

namespace Repository.Services
{
    public class ProcConfigService : IProcConfigService
    {
        private readonly Dictionary<string, ProcConfig> _procConfigs;
        public ProcConfigService()
        {
            _procConfigs = new Dictionary<string, ProcConfig>();
        }

        public async Task<ProcConfig> GetProcConfig(string id)
        {
            ProcConfig procConfig = null;
            try
            {
                procConfig = _procConfigs[id];
            }
            catch (KeyNotFoundException)
            {
                //
            }
            return procConfig;
        }

        public async Task<int> AddProcConfig(ProcConfig procConfig)
        {
            try
            {
                _procConfigs.Add(procConfig.UserRequest.RequestId, procConfig);
            }
            catch (ArgumentException)
            {
                return 1;
            }
            return 0;
        }
    }
}
