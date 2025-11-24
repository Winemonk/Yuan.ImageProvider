using Yuan.ImageProvider.Configs;
using Yuan.ImageProvider.Utils;

namespace Yuan.ImageProvider.Services
{
    public interface IImageCacheService
    {
        Task<string> GetImageCacheUri(ImageBedSettings bedSettings);
    }
}
