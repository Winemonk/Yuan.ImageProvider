using Serilog;
using System.Diagnostics;
using Yuan.ImageProvider.Configs;
using Yuan.ImageProvider.Services;
using Yuan.ImageProvider.Services.Impl;

namespace Yuan.ImageProvider
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            var exeDirectory = Path.GetDirectoryName(exePath);
            if (!string.IsNullOrEmpty(exeDirectory))
                Directory.SetCurrentDirectory(exeDirectory);
            // Add services to the container.
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();
            builder.Host.UseSerilog();
            builder.Host.UseWindowsService();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddOptions();
            builder.Services.AddMemoryCache();
            builder.Services.Configure<ImageProviderSettings>(
                builder.Configuration.GetSection(nameof(ImageProviderSettings)));

            builder.Services.AddTransient<IImageProviderService, ImageProviderService>();
            builder.Services.AddTransient<IImageSkipService, ImageSkipService>();
            builder.Services.AddTransient<IImageCacheService, ImageCacheService>();
            builder.Services.AddHostedService<ImageCacheMonitorService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
