using System.Text.Json.Nodes;
using Yuan.ImageProvider.Configs;

namespace Yuan.ImageProvider.Utils
{
    public static class ImageBedUtil
    {
        /// <summary>
        /// 获取图片地址
        /// </summary>
        /// <param name="providerSettings"> 图像供应器器配置 </param>
        /// <param name="bedSettings"> 图床配置 </param>
        /// <returns> 图片地址 </returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<string> GetImageUriAsync(ImageBedSettings bedSettings)
        {
            HttpClient client = await GetImageBedHttpClientAsync(bedSettings);
            using HttpResponseMessage responseMessage = await client.GetAsync(bedSettings.Url);
            string content = await responseMessage.Content.ReadAsStringAsync();
            string imageUrl = content.Trim();
            string[]? pathKeys = bedSettings.PathKeys;
            if (pathKeys != null && pathKeys.Length > 0)
            {
                JsonObject jsonObject = JsonNode.Parse(content) as JsonObject
                    ?? throw new InvalidOperationException($"获取图片地址失败！图床：{bedSettings.Id}，响应：{content}");
                JsonNode? urlNode = null;
                foreach (string pathKey in pathKeys)
                {
                    urlNode = jsonObject[pathKey]
                        ?? throw new InvalidOperationException($"获取图片地址失败！图床：{bedSettings.Id}，响应：{content}，未找到路径：{pathKey}");
                }
                imageUrl = urlNode!.ToString();
            }
            if (string.IsNullOrEmpty(imageUrl))
            {
                throw new InvalidOperationException(string.Format("获取图片地址失败！bedId:{0}, resp：{1}", bedSettings.Id, content));
            }

            return imageUrl;
        }

        private readonly static Dictionary<string, HttpClient> _HTTP_CLIENT_POOL = new Dictionary<string, HttpClient>();
        private readonly static object _LOCK = new object();

        /// <summary>
        /// 创建 HttpClient
        /// </summary>
        /// <param name="bedSettings"> 图床配置 </param>
        /// <returns> HttpClient </returns>
        public static Task<HttpClient> GetImageBedHttpClientAsync(ImageBedSettings bedSettings)
        {
            if(!_HTTP_CLIENT_POOL.TryGetValue(bedSettings.Id, out HttpClient? client) || client == null)
            {
                lock (_LOCK)
                {
                    if (!_HTTP_CLIENT_POOL.TryGetValue(bedSettings.Id, out client))
                    {
                        client = new HttpClient();
                        _HTTP_CLIENT_POOL.Add(bedSettings.Id, client);
                    }
                    else if (client == null)
                    {
                        client = new HttpClient();
                        _HTTP_CLIENT_POOL[bedSettings.Id] = client;
                    }
                }
            }
            if (bedSettings.Headers?.Count > 0)
            {
                foreach (var header in bedSettings.Headers)
                {
                    if (string.IsNullOrEmpty(header.Key))
                        continue;
                    if (client.DefaultRequestHeaders.Contains(header.Key))
                    {
                        if (client.DefaultRequestHeaders.GetValues(header.Key).Contains(header.Value))
                        {
                            continue;
                        }
                        else
                        {
                            client.DefaultRequestHeaders.Remove(header.Key);
                        }
                    }
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            return Task.FromResult(client);
        }
    }
}
