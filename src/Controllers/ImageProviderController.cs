using Microsoft.AspNetCore.Mvc;
using Yuan.ImageProvider.Entities;
using Yuan.ImageProvider.Services;

namespace Yuan.ImageProvider.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageProviderController : ControllerBase
    {
        private IImageProviderService _imageProviderService;
        private IImageSkipService _imageSkipService;
        private ILogger<ImageProviderController> _logger;

        public ImageProviderController(IImageProviderService imageProviderService,
                                       ILogger<ImageProviderController> logger,
                                       IImageSkipService imageSkipService)
        {
            _imageProviderService = imageProviderService;
            _logger = logger;
            _imageSkipService = imageSkipService;
        }

        [HttpGet("random")]
        public async Task<IActionResult> GetRandom([FromQuery] string? bedId = null)
        {
            try
            {
                string imageUrl = await _imageProviderService.GetRandomImageAsync(bedId).ConfigureAwait(false);
                _logger.LogInformation("获取随机图像成功：{url}", imageUrl);
                return Ok(new { url = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取随机图像时出错！");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("skip/{skipKey}")]
        public async Task<IActionResult> GetSkip(string skipKey)
        {
            try
            {
                ImageUriInfo imageUrlInfo = await _imageSkipService.GetSkipImageUrlAsync(skipKey).ConfigureAwait(false);
                _logger.LogInformation("图像跳转：{@imageUrlInfo}", imageUrlInfo);
                if (imageUrlInfo.IsLocal)
                {
                    return PhysicalFile(imageUrlInfo.Uri, GetMimeType(imageUrlInfo.Uri));
                }
                else
                {
                    return RedirectPermanent(imageUrlInfo.Uri);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取图像跳转连接时出错！");
                return StatusCode(500, ex.Message);
            }
        }

        private string GetMimeType(string path)
        {
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var mimeType))
                mimeType = "application/octet-stream";
            return mimeType;
        }
    }
}
