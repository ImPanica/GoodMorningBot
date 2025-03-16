using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using GoodMorningBot.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoodMorningBot.Services
{
    public class MorningMessageService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly BotDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly string _unsplashApiKey;

        private static string EscapeMarkdown(string text)
        {
            var specialCharacters = new[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
            return string.Join("", text.Select(x => specialCharacters.Contains(x) ? "\\" + x : x.ToString()));
        }

        public MorningMessageService(
            ITelegramBotClient botClient,
            BotDbContext dbContext,
            string unsplashApiKey)
        {
            _botClient = botClient;
            _dbContext = dbContext;
            _httpClient = new HttpClient();
            _unsplashApiKey = unsplashApiKey;
        }

        public async Task SendMorningMessagesAsync()
        {
            var chats = await _dbContext.Chats.ToListAsync();
            var (quote, author) = await GetQuoteAsync();
            var imageUrl = await GetMorningImageAsync();

            foreach (var chat in chats)
            {
                try
                {
                    using (var stream = await _httpClient.GetStreamAsync(imageUrl))
                    {
                        var escapedQuote = EscapeMarkdown(quote);
                        var escapedAuthor = EscapeMarkdown(author);
                        var caption = $"*Доброе утро\\!* ☀️\n\nЦитата дня:\n`{escapedQuote}` _\\(c\\) {(escapedAuthor == string.Empty ? "Неизвестный автор" : escapedAuthor)}_";
                        await _botClient.SendPhotoAsync(
                            chatId: chat.ChatId,
                            photo: InputFile.FromStream(stream),
                            caption: caption,
                            parseMode: ParseMode.MarkdownV2
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message to chat {chat.ChatId}: {ex.Message}");
                }
            }
        }

        private async Task<(string quote, string author)> GetQuoteAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("http://api.forismatic.com/api/1.0/?method=getQuote&format=json&lang=ru");
                var json = JObject.Parse(response);
                var quote = json["quoteText"]?.ToString() ?? "Каждое утро - это новая возможность изменить свою жизнь к лучшему!";
                var author = json["quoteAuthor"]?.ToString() ?? "Неизвестный автор";
                return (quote, author);
            }
            catch
            {
                return ("Каждое утро - это новая возможность изменить свою жизнь к лучшему!", "Неизвестный автор");
            }
        }

        private async Task<string> GetMorningImageAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://api.unsplash.com/photos/random?query=good%20morning&client_id={_unsplashApiKey}");
                var json = JObject.Parse(response);
                var urls = json["urls"];
                return urls?["regular"]?.ToString() ?? "https://images.unsplash.com/photo-1496903029469-38a1dabe9079";
            }
            catch
            {
                return "https://images.unsplash.com/photo-1496903029469-38a1dabe9079";
            }
        }
    }
} 