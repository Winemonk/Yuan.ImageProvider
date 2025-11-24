using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Yuan.ImageProvider.Configs;
using Yuan.ImageProvider.Utils;

namespace Yuan.ImageProvider.Services.Impl
{
    public class ImageCacheService : IImageCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IOptionsSnapshot<ImageProviderSettings> _providerSettings;
        private readonly ILogger<ImageCacheService> _logger;

        public ImageCacheService(IOptionsSnapshot<ImageProviderSettings> providerSettings,
                                 ILogger<ImageCacheService> logger,
                                 IMemoryCache memoryCache)
        {
            _providerSettings = providerSettings;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<string> GetImageCacheUri(ImageBedSettings bedSettings)
        {
            string bedCacheKey = $"{Consts.ImageUriInfoCachePerfix}{bedSettings.Id}";

            bool getCache = _memoryCache.TryGetValue(bedCacheKey, out ConcurrentQueue<string>? imageUriQueue);
            while (!getCache || imageUriQueue == null)
            {
                await Task.Delay(100);
                _memoryCache.TryGetValue(bedCacheKey, out  imageUriQueue);
            }
            string? cachePath;
            while (!imageUriQueue.TryDequeue(out cachePath) || string.IsNullOrEmpty(cachePath) || !File.Exists(cachePath))
            {
                await Task.Delay(100);
            }
            return cachePath;
        }
    }
}
