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
        public async Task<IActionResult> GetRandom([FromQuery] string? type = null, [FromQuery] string? bedId = null)
        {
            try
            {
                if(!string.IsNullOrEmpty(type) && type.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    string imageUrl = await _imageProviderService.GetRandomImageUrlAsync(bedId).ConfigureAwait(false);
                    _logger.LogInformation("获取随机图像成功：{url}", imageUrl);
                    return Ok(new { url = imageUrl });
                }
                else
                {
                    ImageUriInfo imageUriInfo = await _imageProviderService.GetRandomImageAsync(bedId).ConfigureAwait(false);
                    _logger.LogInformation("获取随机图像成功：{@imageUriInfo}", imageUriInfo);
                    if (imageUriInfo.IsLocal)
                    {
                        return PhysicalFile(imageUriInfo.Uri, GetMimeType(imageUriInfo.Uri));
                    }
                    else
                    {
                        return RedirectPermanent(imageUriInfo.Uri);
                    }
                }
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
                ImageUriInfo imageUriInfo = await _imageSkipService.GetSkipImageUrlAsync(skipKey).ConfigureAwait(false);
                _logger.LogInformation("图像跳转：{@imageUrlInfo}", imageUriInfo);
                if (imageUriInfo.IsLocal)
                {
                    byte[] imageBytes = System.IO.File.ReadAllBytes(imageUriInfo.Uri);
                    _logger.LogInformation("获取图像大小：{size}", imageBytes.Length);
                    return File(imageBytes, "image/jpeg", $"{imageUriInfo.Key}.jpg");
                    //return PhysicalFile(imageUriInfo.Uri, GetMimeType(imageUriInfo.Uri));
                }
                else
                {
                    return RedirectPermanent(imageUriInfo.Uri);
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
