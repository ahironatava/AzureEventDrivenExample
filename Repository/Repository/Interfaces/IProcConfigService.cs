using CommonModels;

namespace Repository.Interfaces
{
    public interface IProcConfigService
    {
        public Task<ProcConfig> GetProcConfig(string id);

        public Task<int> AddProcConfig(ProcConfig procConfig);
    }
}

