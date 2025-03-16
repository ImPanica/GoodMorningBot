using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.EntityFrameworkCore;
using GoodMorningBot.Data;
using GoodMorningBot.Models;

namespace GoodMorningBot.Services
{
    public class BotUpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly BotDbContext _dbContext;

        public BotUpdateHandler(ITelegramBotClient botClient, BotDbContext dbContext)
        {
            _botClient = botClient;
            _dbContext = dbContext;
        }

        public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
        {
            if (update.Message?.Type != MessageType.Text)
                return;

            var message = update.Message;
            if (message.Text?.StartsWith("/start") == true)
            {
                var chatId = message.Chat.Id;
                var existingChat = await _dbContext.Chats.FirstOrDefaultAsync(c => c.ChatId == chatId);

                if (existingChat == null)
                {
                    var chatInfo = new ChatInfo
                    {
                        ChatId = chatId,
                        ChatTitle = message.Chat.Title ?? message.Chat.Username ?? message.Chat.FirstName ?? "Unknown",
                        AddedDate = DateTime.UtcNow
                    };

                    _dbContext.Chats.Add(chatInfo);
                    await _dbContext.SaveChangesAsync();

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Привет! Я буду отправлять вам доброе утро каждый день в 9:00 по московскому времени! 🌅",
                        cancellationToken: cancellationToken);
                }
            }
        }

        public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Error handling update: {exception.Message}");
            return Task.CompletedTask;
        }
    }
} 