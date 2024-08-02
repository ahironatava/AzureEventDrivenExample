using CommonModels;
using Newtonsoft.Json;
using System.IO;

namespace RepositoryClient
{
    public class RepoClient
    {
        private readonly HttpClient _client;
        private readonly string _repoUrl;

        public RepoClient(string repoUrl)
        {
            _repoUrl = repoUrl;
            _client = new HttpClient();
        }

        public async Task<(string, UserRequest?)> GetUserRequestAsync(string id)
        {
            var path = $"{_repoUrl}/api/UserRequest/{id}";
            (string errMsg, string objAsString) = await GetContentAsync(path);

            if (!string.IsNullOrEmpty(objAsString))
            {
                return (errMsg, JsonConvert.DeserializeObject<UserRequest>(objAsString));
            }
            else
            {
                return (errMsg, null);
            }
        }

        public async Task<(bool, string)> SaveUserRequestAsync(UserRequest userRequest)
        {
            var path = $"{_repoUrl}/api/UserRequest/";
            var content = JsonConvert.SerializeObject(userRequest);

            return await PostContentAsync(path, content);
        }

        public async Task<(string, ProcConfig?)> GetProcConfigAsync(string id)
        {
            var path = $"{_repoUrl}/api/ProcConfig/{id}";
            (string errMsg, string objAsString) = await GetContentAsync(path);

            if (!string.IsNullOrEmpty(objAsString))
            {
                return (errMsg, JsonConvert.DeserializeObject<ProcConfig>(objAsString));
            }
            else
            {
                return (errMsg, null);
            }
        }

        public async Task<(bool, string)> SaveProcConfigAsync(ProcConfig procConfig)
        {
            var path = $"{_repoUrl}/api/ProcConfig/";
            var content = JsonConvert.SerializeObject(procConfig);

            return await PostContentAsync(path, content);
        }

        public async Task<(string, ProcessingResults?)> GetProcResultAsync(string id)
        {
            var path = $"{_repoUrl}/api/ProcResult/{id}";
            (string errMsg, string objAsString) = await GetContentAsync(path);

            if (!string.IsNullOrEmpty(objAsString))
            {
                return (errMsg, JsonConvert.DeserializeObject<ProcessingResults>(objAsString));
            }
            else
            {
                return (errMsg, null);
            }
        }

        public async Task<(bool, string)> SaveProcResultAsync(ProcessingResults processingResults)
        {
            var path = $"{_repoUrl}/api/ProcResult/";
            var content = JsonConvert.SerializeObject(processingResults);

            return await PostContentAsync(path, content);
        }

        private async Task<(bool, string)> PostContentAsync(string path, string content)
        {
            StringContent stringContent = new StringContent(content);
            stringContent.Headers.ContentType.MediaType = "application/json";

            bool returnValue = false;
            string errMsg = string.Empty;
            try
            {
                var response = await _client.PostAsync(path, stringContent);
                if (response.IsSuccessStatusCode)
                {
                    returnValue = true;
                }
            }
            catch (Exception e)
            {
                errMsg = e.Message;
            }
            return (returnValue, errMsg);
        }

        private async Task<(string, string)> GetContentAsync(string path)
        {
            string errMsg = string.Empty;
            string objectString = string.Empty;
            try
            {
                var response = await _client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    objectString = await response.Content.ReadAsStringAsync();
                }
            }
            catch(Exception e)
            {
                errMsg = e.Message;
            }
            return (errMsg, objectString);
        }

        //////////////////////////////////////////////////////////////////

        // Debug: get the containing Dictionaries

        public async Task<(string, Dictionary<string, UserRequest>)> GetUserRequestDictionary()
        {
            string errMsg = string.Empty;
            string objectString = string.Empty;
            Dictionary<string, UserRequest> dictionary = new Dictionary<string, UserRequest>();
            try
            {
                var response = await _client.GetAsync($"{_repoUrl}/api/UserRequest/");

                if (response.IsSuccessStatusCode)
                {
                    objectString = await response.Content.ReadAsStringAsync();
                    dictionary = JsonConvert.DeserializeObject<Dictionary<string, UserRequest>>(objectString);
                }
            }
            catch (Exception e)
            {
                errMsg = e.Message;
            }
            return (errMsg, dictionary);
        }

        public async Task<(string, Dictionary<string, ProcConfig>)> GetProcConfigDictionary()
        {
            string errMsg = string.Empty;
            string objectString = string.Empty;
            Dictionary<string, ProcConfig> dictionary = new Dictionary<string, ProcConfig>();
            try
            {
                var response = await _client.GetAsync($"{_repoUrl}/api/ProcConfig/");

                if (response.IsSuccessStatusCode)
                {
                    objectString = await response.Content.ReadAsStringAsync();
                    dictionary = JsonConvert.DeserializeObject<Dictionary<string, ProcConfig>>(objectString);
                }
            }
            catch (Exception e)
            {
                errMsg = e.Message;
            }
            return (errMsg, dictionary);
        }

        public async Task<(string, Dictionary<string, ProcessingResults>)> GetProcessingResultsDictionary()
        {
            string errMsg = string.Empty;
            string objectString = string.Empty;
            Dictionary<string, ProcessingResults> dictionary = new Dictionary<string, ProcessingResults>();
            try
            {
                var response = await _client.GetAsync($"{_repoUrl}/api/ProcResult/");

                if (response.IsSuccessStatusCode)
                {
                    objectString = await response.Content.ReadAsStringAsync();
                    dictionary = JsonConvert.DeserializeObject<Dictionary<string, ProcessingResults>>(objectString);
                }
            }
            catch (Exception e)
            {
                errMsg = e.Message;
            }
            return (errMsg, dictionary);
        }
    }
}
