using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Yuan.ImageProvider.Configs;
using Yuan.ImageProvider.Entities;
using Yuan.ImageProvider.Utils;

namespace Yuan.ImageProvider.Services.Impl
{
    public class ImageProviderService : IImageProviderService
    {
        private readonly IOptionsSnapshot<ImageProviderSettings> _imageProviderSettings;
        private readonly ILogger<ImageProviderService> _logger;
        private readonly IImageSkipService _imageSkipService;
        private readonly IImageCacheService _imageCacheService;

        public ImageProviderService(IOptionsSnapshot<ImageProviderSettings> imageProviderSettings,
                                    ILogger<ImageProviderService> logger,
                                    IImageSkipService imageSkipService,
                                    IImageCacheService imageCacheService)
        {
            _imageProviderSettings = imageProviderSettings;
            _logger = logger;
            _imageSkipService = imageSkipService;
            _imageCacheService = imageCacheService;
        }

        public async Task<string> GetRandomImageAsync(string? bedId)
        {
            ImageProviderSettings providerSettings = _imageProviderSettings.Value;
            ImageBedSettings bedSettings = GetImageBedSettings(bedId, providerSettings);
            string returnUrl;
            if (providerSettings.EnableLocalCache)
            {
                string key = Guid.NewGuid().ToString("N");
                returnUrl = _imageSkipService.GetSkipUrl(key);
                string cachePath = await _imageCacheService.GetImageCacheUri(bedSettings);
                ImageUriInfo imageUrlInfo = new ImageUriInfo
                {
                    Key = key,
                    IsLocal = true,
                    Uri = cachePath,
                };
                await _imageSkipService.SetSkipImageUrlAsync(imageUrlInfo);
            }
            else
            {
                if (bedSettings.IsByteResponse)
                {
                    string key = Guid.NewGuid().ToString("N");
                    returnUrl = _imageSkipService.GetSkipUrl(key);
                    ImageUriInfo imageUrlInfo = new ImageUriInfo
                    {
                        Key = key,
                        Uri = bedSettings.Url,
                        IsLocal = false
                    };
                    await _imageSkipService.SetSkipImageUrlAsync(imageUrlInfo);
                }
                else
                {
                    returnUrl = await ImageBedUtil.GetImageUriAsync(bedSettings);
                }
            }
            return returnUrl;
        }

        private static ImageBedSettings GetImageBedSettings(string? bedId, ImageProviderSettings providerSettings)
        {
            ImageBedSettings[]? imageBeds = providerSettings.ImageBeds;
            if (imageBeds == null || imageBeds.Length < 1)
            {
                throw new InvalidOperationException("未配置图床！");
            }
            ImageBedSettings bedSettings;
            if (string.IsNullOrEmpty(bedId))
            {
                Random random = new Random();
                int index = random.Next(0, imageBeds.Length);
                bedSettings = imageBeds[index];
            }
            else
            {
                bedSettings = imageBeds.FirstOrDefault(x => x.Id == bedId) ?? throw new ArgumentException($"未找到图床：{bedId}");
            }
            return bedSettings;
        }
    }
}
