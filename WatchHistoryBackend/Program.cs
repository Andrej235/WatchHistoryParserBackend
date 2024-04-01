
using WatchHistoryBackend.Data;
using WatchHistoryBackend.DTOs;
using WatchHistoryBackend.Models;
using WatchHistoryBackend.Services.Mapping;
using WatchHistoryBackend.Services.Read;

namespace WatchHistoryBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddTransient<MusicContext>();
            builder.Services.AddSingleton<IMapper<Song, SongDTO>, SongMapper>();
            builder.Services.AddSingleton<IReadService<Song>, SongReadService>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("WebApp",
                    builder =>
                    {
                        builder.WithOrigins("http://192.168.1.100:5500")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                    });

                options.AddPolicy("LocalWebApp",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:5500")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                    });
            });

            var app = builder.Build();

            //app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            app.UseCors("WebApp");
            app.UseCors("LocalWebApp");
            app.Run();
        }
    }
}