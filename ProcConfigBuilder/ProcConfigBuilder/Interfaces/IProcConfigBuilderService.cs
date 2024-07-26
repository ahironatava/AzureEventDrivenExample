using CommonModels;
using Newtonsoft.Json.Linq;

namespace ProcConfigBuilder.Interfaces
{
    public interface IProcConfigBuilderService
    {
        public Task<int> CreateAndPublishProcConfigFile(GridEvent<dynamic> gridEvent);
    }
}
