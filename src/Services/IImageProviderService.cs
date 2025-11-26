using Yuan.ImageProvider.Entities;

namespace Yuan.ImageProvider.Services
{
    public interface IImageProviderService
    {
        Task<ImageUriInfo> GetRandomImageAsync(string? bedId);
        Task<string> GetRandomImageUrlAsync(string? bedId);
    }
}
