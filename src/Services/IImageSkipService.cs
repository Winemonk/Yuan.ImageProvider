using Microsoft.AspNetCore.Routing;
using Yuan.ImageProvider.Entities;

namespace Yuan.ImageProvider.Services
{
    public interface IImageSkipService
    {
        Task SetSkipImageUrlAsync(ImageUriInfo imageUrlInfo);
        Task<ImageUriInfo> GetSkipImageUrlAsync(string skipKey);
        string GetSkipUrl(string skipKey);
    }
}
