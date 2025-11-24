using Yuan.ImageProvider.Entities;

namespace Yuan.ImageProvider.Services
{
    public interface IImageProviderService
    {
        Task<string> GetRandomImageAsync(string? bedId);
    }
}
