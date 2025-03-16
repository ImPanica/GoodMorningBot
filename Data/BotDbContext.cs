using Microsoft.EntityFrameworkCore;
using GoodMorningBot.Models;

namespace GoodMorningBot.Data
{
    public class BotDbContext : DbContext
    {
        public BotDbContext(DbContextOptions<BotDbContext> options) : base(options)
        {
        }

        public DbSet<ChatInfo> Chats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatInfo>()
                .HasIndex(c => c.ChatId)
                .IsUnique();
        }
    }
} 