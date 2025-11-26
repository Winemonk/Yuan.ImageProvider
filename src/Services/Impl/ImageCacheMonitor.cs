
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Yuan.ImageProvider.Configs;
using Yuan.ImageProvider.Utils;

namespace Yuan.ImageProvider.Services.Impl
{
    public class ImageCacheMonitorService : BackgroundService
    {
        private readonly static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ImageCacheMonitorService> _logger;
        private readonly IOptionsMonitor<ImageProviderSettings> _imageProviderSettings;
        private readonly List<IDisposable> _disposables = [];

        public ImageCacheMonitorService(IMemoryCache memoryCache,
                                        ILogger<ImageCacheMonitorService> logger,
                                        IOptionsMonitor<ImageProviderSettings> imageProviderSettings)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _imageProviderSettings = imageProviderSettings;
        }

        public override void Dispose()
        {
            foreach (IDisposable disposable in _disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "释放资源失败！");
                }
            }
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ImageProviderSettings providerSettings = _imageProviderSettings.CurrentValue;
            TimeSpan period = TimeSpan.FromSeconds(providerSettings.CacheMonitoredInterval);
            using PeriodicTimer timer = new PeriodicTimer(period);
            IDisposable? disposable = _imageProviderSettings.OnChange(newSettings =>
            {
                if (timer.Period != TimeSpan.FromSeconds(newSettings.CacheMonitoredInterval))
                {
                    timer.Period = TimeSpan.FromSeconds(newSettings.CacheMonitoredInterval);
                    _logger.LogInformation("缓存监控定时任务周期已更新为：{interval}秒", newSettings.CacheMonitoredInterval);
                }

            });
            if (disposable != null)
            {
                _disposables.Add(disposable);
            }
            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await DoWorkAsync(stoppingToken);
                    _logger.LogDebug("缓存监控定时任务执行完成！");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "缓存监控定时任务执行失败！");
                }
            }
        }

        private async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            ImageProviderSettings providerSettings = _imageProviderSettings.CurrentValue;
            if (!providerSettings.EnableLocalCache)
            {
                return;
            }
            ImageBedSettings[]? imageBeds = providerSettings.ImageBeds;
            IEnumerable<Task>? tasks = imageBeds?.Select(async bedSettings =>
            {
                string bedCacheKey = $"{Consts.ImageUriInfoCachePerfix}{bedSettings.Id}";
                bool getCache = _memoryCache.TryGetValue(bedCacheKey, out ConcurrentQueue<string>? imageUriQueue);
                if (!getCache || imageUriQueue == null)
                {
                    imageUriQueue = new ConcurrentQueue<string>();
                    await _semaphore.WaitAsync();
                    try
                    {
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromMinutes(60))    // 滑动过期时间
                                                                               //.SetAbsoluteExpiration(TimeSpan.FromHours(1))      // 绝对过期时间
                            .SetPriority(CacheItemPriority.Normal);            // 缓存优先级
                        _memoryCache.Set(bedCacheKey, imageUriQueue, cacheOptions);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                while (imageUriQueue.Count < providerSettings.CacheSize)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    string cachePath = await CacheImageAsync(providerSettings, bedSettings);
                    if (!string.IsNullOrEmpty(cachePath))
                    {
                        imageUriQueue.Enqueue(cachePath);
                        _logger.LogDebug("添加图床缓存至队列，序号：{index}, 图片地址：{imageUrl}", imageUriQueue.Count, cachePath);
                    }
                    await Task.Delay(10);
                }
            });
            if (tasks!= null)
            {
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is AggregateException aggEx)
                    {
                        foreach (Exception innerEx in aggEx.InnerExceptions)
                        {
                            if (innerEx is not OperationCanceledException || innerEx is not TaskCanceledException)
                            {
                                throw;
                            }
                        }
                    }
                    else if (ex is not OperationCanceledException || ex is not TaskCanceledException)
                    {
                        throw;
                    }
                }
            }
        }

        private async Task<string> CacheImageAsync(ImageProviderSettings providerSettings, ImageBedSettings bedSettings)
        {
            if (bedSettings.IsLocalDirectory)
            {
                if (string.IsNullOrEmpty(bedSettings.Url))
                    throw new ArgumentException($"图床Url为空：{bedSettings.Id}！");
                if (!Directory.Exists(bedSettings.Url))
                    throw new ArgumentException($"图床目录不存在{bedSettings.Url}！");
                string[] imageExtensions = { "*.jpg", "*.jpeg", "*.png", "*.webp" };
                var imageFiles = imageExtensions
                    .SelectMany(ext => Directory.GetFiles(bedSettings.Url, ext, SearchOption.AllDirectories))
                    .ToArray();
                if (imageFiles.Length == 0)
                {
                    throw new ArgumentException($"图床目录下没有图片文件：{bedSettings.Url}！");
                }
                Random random = new Random();
                string randomImage = imageFiles[random.Next(imageFiles.Length)];
                return randomImage;
            }
            else
            {
                string imageUrl;
                if (bedSettings.IsByteResponse)
                {
                    imageUrl = bedSettings.Url;
                }
                else
                {
                    imageUrl = await ImageBedUtil.GetImageUriAsync(bedSettings);
                }

                HttpClient client = await ImageBedUtil.GetImageBedHttpClientAsync(bedSettings);

                string? cachePath = providerSettings.LocalCachePath;
                if (string.IsNullOrEmpty(cachePath))
                {
                    //cachePath = Path.Combine(
                    //    Environment.GetFolderPath(
                    //        Environment.SpecialFolder.CommonApplicationData,
                    //        Environment.SpecialFolderOption.Create),
                    //    "Yuan/ImageProvider");
                    cachePath = Path.Combine(AppContext.BaseDirectory, "caches");
                }
                _logger.LogDebug("缓存根路径：{cachePath}", cachePath);
                if (providerSettings.CreateCategoryCacheDirectory)
                {
                    cachePath = Path.Combine(cachePath, bedSettings.Category ?? "未分类");
                }
                if (providerSettings.CreateBedCacheDirectory)
                {
                    cachePath = Path.Combine(cachePath, bedSettings.Id);
                }
                if (!Directory.Exists(cachePath))
                {
                    Directory.CreateDirectory(cachePath);
                }
                HttpResponseMessage httpResponseMessage = await client.GetAsync(imageUrl);
                using Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync();
                string md5 = await CryptographyUtil.CalculateStreamMD5Async(stream);
                cachePath = Path.Combine(cachePath, md5 + ".jpg");
                if (!File.Exists(cachePath))
                {
                    using FileStream fileStream = new FileStream(cachePath, FileMode.Create, FileAccess.Write);
                    byte[] buffer = new byte[1024 * 80];
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                    fileStream.Flush();
                    _logger.LogDebug("缓存图床：‘{bedId}’的图片：‘{imageUrl}’到‘{cachePath}’", bedSettings.Id, imageUrl, cachePath);
                }
                cachePath = cachePath.Replace('\\', '/');
                return cachePath;
            }
        }
    }
}
