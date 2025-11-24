using System.Text.Json.Serialization;

namespace Yuan.ImageProvider.Configs
{
    public class ImageProviderSettings
    {
        public bool EnableLocalCache { get; set; }
        public string? LocalCachePath { get; set; }
        public bool CreateCategoryCacheDirectory { get; set; }
        public bool CreateBedCacheDirectory { get; set; }
        public int CacheMonitoredInterval { get; set; } = 60;
        public int CacheSize { get; set; } = 5;
        public ImageBedSettings[]? ImageBeds { get; set; }
    }

    public class ImageBedSettings
    {
        public string Id { get; set; }
        public string? Category { get; set; }
        public string Url { get; set; }
        public bool IsByteResponse { get; set; }
        public bool IsLocalDirectory { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public string[]? PathKeys { get; set; }
    }

    //[JsonConverter(typeof(JsonStringEnumConverter))]
    //public enum RestfulAction
    //{
    //    Get,
    //    Post,
    //    Put,
    //    Delete
    //}
}
