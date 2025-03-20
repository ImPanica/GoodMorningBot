using System.Net.Mime;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace GoodMorningBot.Services;

public class CitationMessageService
{
    private readonly ITelegramBotClient _botClient;
    private readonly HttpClient _httpClient;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private static long _chatId;

    private static string EscapeMarkdown(string text)
    {
        var specialCharacters = new[]
            { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        return string.Join("", text.Select(x => specialCharacters.Contains(x) ? "\\" + x : x.ToString()));
    }

    public CitationMessageService(
        ITelegramBotClient botClient,
        long chatId)
    {
        _botClient = botClient;
        _chatId = chatId;
        _chatId = chatId;
        _httpClient = new HttpClient();
    }

    private async Task<(string quote, string author)> GetQuoteAsync()
    {
        var quoteText =
            "У того, кто постигнет суть вещей, в одном вершке сердца сойдет лунная дымка Пяти озер. Тот, кто прозреет исток всех превращений, заключит в объятия великих мужей всех времен.";
        try
        {
            var response =
                await _httpClient.GetStringAsync(
                    "https://api.forismatic.com/api/1.0/?method=getQuote&format=json&lang=ru");
            var json = JObject.Parse(response);
            return (
                json["quoteText"]?.ToString() ?? quoteText,
                json["quoteAuthor"]?.ToString() ?? "Хун Цзычен"
            );
        }
        catch
        {
            return (quoteText, string.Empty);
        }
    }

    public async Task SendEveningMessagesAsync()
    {
        // Пытаемся получить блокировку
        if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            return; // Если не удалось получить блокировку за 5 секунд, выходим
        }

        try
        {
            var (quote, author) = await GetQuoteAsync();

            try
            {
                var escapedQuote = EscapeMarkdown(quote);
                var escapedAuthor = EscapeMarkdown(author);
                var caption =
                    $"*Случайная цитата\\!* 🎲:\n\n>{escapedQuote}\n_\\(c\\) {(escapedAuthor == string.Empty ? "Неизвестный автор" : escapedAuthor)}_";
                await _botClient.SendTextMessageAsync(
                    chatId: _chatId,
                    text: caption,
                    parseMode: ParseMode.MarkdownV2
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to chat {_chatId}: {ex.Message}");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}