using GoodMorningBot.Data;
using GoodMorningBot.Jobs;
using GoodMorningBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace GoodMorningBot
{
    class Program
    {
        private static readonly Mutex _mutex = new Mutex(true, "GoodMorningBot_SingleInstance");

        static async Task Main(string[] args)
        {
            if (!_mutex.WaitOne(TimeSpan.Zero, true))
            {
                Console.WriteLine("Another instance of the bot is already running!");
                return;
            }

            try
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

                using var cts = new CancellationTokenSource();

                // Start receiving updates
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = Array.Empty<UpdateType>()
                };

                bot.StartReceiving(
                    updateHandler: async (client, update, token) => await handler.HandleUpdateAsync(update, token),
                    pollingErrorHandler: async (client, exception, token) =>
                        await handler.HandleErrorAsync(exception, token),
                    receiverOptions: receiverOptions,
                    cancellationToken: cts.Token
                );

                var me = await bot.GetMeAsync(cts.Token);
                Console.WriteLine($"Bot started as @{me.Username}! Press Ctrl+C to exit");

                await host.RunAsync(cts.Token);
            }
            finally
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                            optional: true, reloadOnChange: true)
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

                    services.AddScoped(sp => new EveningMessageService(
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
                        // Утреннее сообщение
                        var morningJobKey = new JobKey("MorningMessageJob");
                        q.AddJob<MorningMessageJob>(opts => opts.WithIdentity(morningJobKey));

                        q.AddTrigger(opts => opts
                            .ForJob(morningJobKey)
                            .WithIdentity("MorningMessageTrigger")
                            .WithCronSchedule("0 0 9 * * ?", x => x
                                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")))
                        );

                        // Вечернее сообщение
                        var eveningJobKey = new JobKey("EveningMessageJob");
                        q.AddJob<EveningMessageJob>(opts => opts.WithIdentity(eveningJobKey));

                        q.AddTrigger(opts => opts
                            .ForJob(eveningJobKey)
                            .WithIdentity("EveningMessageTrigger")
                            .WithCronSchedule("0 0 21 * * ?", x => x
                                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")))
                        );
                    });

                    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
                });
        }
    }
}