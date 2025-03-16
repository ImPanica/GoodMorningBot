using System;

namespace GoodMorningBot.Models
{
    public class ChatInfo
    {
        public long Id { get; set; }
        public long ChatId { get; set; }
        public string ChatTitle { get; set; } = string.Empty;
        public DateTime AddedDate { get; set; }
    }
} 