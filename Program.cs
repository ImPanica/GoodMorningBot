using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Quartz;
using GoodMorningBot.Data;
using GoodMorningBot.Services;
using GoodMorningBot.Jobs;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace GoodMorningBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            // Initialize database
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
                await db.Database.MigrateAsync();
            }

            var bot = host.Services.GetRequiredService<ITelegramBotClient>();
            var handler = host.Services.GetRequiredService<BotUpdateHandler>();

            using var cts = new System.Threading.CancellationTokenSource();

            // Start receiving updates
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            bot.StartReceiving(
                updateHandler: async (client, update, token) => await handler.HandleUpdateAsync(update, token),
                pollingErrorHandler: async (client, exception, token) => await handler.HandleErrorAsync(exception, token),
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await bot.GetMeAsync(cts.Token);
            Console.WriteLine($"Bot started as @{me.Username}! Press Ctrl+C to exit");
            
            await host.RunAsync(cts.Token);
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;

                    // Configuration
                    var botToken = configuration.GetValue<string>("BotConfiguration:TelegramBotToken")
                        ?? throw new Exception("Telegram Bot Token is not configured");
                    var unsplashApiKey = configuration.GetValue<string>("BotConfiguration:UnsplashApiKey")
                        ?? throw new Exception("Unsplash API Key is not configured");
                    var connectionString = configuration.GetConnectionString("DefaultConnection")
                        ?? throw new Exception("Connection string is not configured");

                    // Database
                    services.AddDbContext<BotDbContext>(options =>
                        options.UseSqlServer(connectionString));

                    // Telegram Bot
                    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
                    services.AddScoped<BotUpdateHandler>();

                    // Services
                    services.AddScoped(sp => new MorningMessageService(
                        sp.GetRequiredService<ITelegramBotClient>(),
                        sp.GetRequiredService<BotDbContext>(),
                        unsplashApiKey
                    ));

                    // Quartz
                    // Расшифровка нового cron-выражения:
                    // Первый 0 - секунды (запускать в 0 секунд)
                    //     * - каждую минуту
                    //     * - каждый час
                    //     * - каждый день месяца
                    //     * - каждый месяц
                    //     ? - любой день недели
                    services.AddQuartz(q =>
                    {
                        var jobKey = new JobKey("MorningMessageJob");
                        q.AddJob<MorningMessageJob>(opts => opts.WithIdentity(jobKey));

                        q.AddTrigger(opts => opts
                            .ForJob(jobKey)
                            .WithIdentity("MorningMessageTrigger")
                            .WithCronSchedule("0 0 9 * * ?", x => x
                                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")))
                        );
                    });

                    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
                });
        }
    }
}
