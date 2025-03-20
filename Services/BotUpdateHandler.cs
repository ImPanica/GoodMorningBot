using GoodMorningBot.Data;
using GoodMorningBot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
            // Проверяем, что пришло текстовое сообщение
            if (update.Message?.Type != MessageType.Text)
                return;

            // Поулчаем сообщение
            var message = update.Message;
            // Проверка на команду /start
            if (message.Text?.StartsWith("/start") == true)
            {
                // Получение ID чата
                var chatId = message.Chat.Id;
                // Поиск, ID чата в базе данных
                var existingChat = await _dbContext.Chats.FirstOrDefaultAsync(c => c.ChatId == chatId);

                // Если такого чата нет
                if (existingChat == null)
                {
                    // Создаем новый объект чата
                    var chatInfo = new ChatInfo
                    {
                        ChatId = chatId, // ID чата 
                        ChatTitle = message.Chat.Title ??
                                    message.Chat.Username ?? message.Chat.FirstName ?? "Unknown", // Название чата
                        AddedDate = DateTime.UtcNow // Дата добавления
                    };

                    // Добавляем в базу данных
                    _dbContext.Chats.Add(chatInfo);
                    // Сохранение изменений
                    await _dbContext.SaveChangesAsync();

                    // Отправка сообщения 
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text:
                        "Привет! Я буду отправлять вам «Доброе утро» и «Доброй ночи» каждый день в 8:00 и 21:00 по московскому времени. 🌅",
                        cancellationToken: cancellationToken);
                }
            }

            // Проверка на команду /citation
            else if (message.Text.StartsWith("/citation") == true)
            {
                // Получение ID чата
                var chatId = message.Chat.Id;

                // Отправка сообщения с случайной цитатой
                await new CitationMessageService(_botClient, chatId)
                    .SendEveningMessagesAsync();
            }
        }

        public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Error handling update: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}