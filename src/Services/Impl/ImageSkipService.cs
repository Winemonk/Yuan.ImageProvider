using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using Yuan.ImageProvider.Entities;

namespace Yuan.ImageProvider.Services.Impl
{
    public class ImageSkipService : IImageSkipService
    {
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ImageSkipService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LinkGenerator _linkGenerator;

        public ImageSkipService(IMemoryCache memoryCache,
                                ILogger<ImageSkipService> logger,
                                IHttpContextAccessor httpContextAccessor,
                                LinkGenerator linkGenerator)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _linkGenerator = linkGenerator;
        }

        public Task<ImageUriInfo> GetSkipImageUrlAsync(string skipKey)
        {
            if (_memoryCache.TryGetValue(skipKey, out ImageUriInfo? imageUrlInfo))
            {
                if (imageUrlInfo != null)
                {
                    return Task.FromResult(imageUrlInfo);
                }
            }
            throw new InvalidOperationException("缓存图片不存在！");
        }

        public async Task SetSkipImageUrlAsync(ImageUriInfo imageUrlInfo)
        {
            await _semaphore.WaitAsync();
            try
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10))    // 滑动过期时间
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1))      // 绝对过期时间
                    .SetPriority(CacheItemPriority.Normal);            // 缓存优先级
                _memoryCache.Set(imageUrlInfo.Key, imageUrlInfo, cacheOptions);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public string GetSkipUrl(string skipKey)
        {
            string url = _linkGenerator.GetUriByAction(
                _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext 不可用！"),
                controller: "ImageProvider",
                action: "GetSkip",
                values: new { skipKey = skipKey }) ?? throw new InvalidOperationException("获取跳转地址失败！");
            return url;
        }
    }
}
