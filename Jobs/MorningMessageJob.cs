using System.Threading.Tasks;
using GoodMorningBot.Services;
using Quartz;

namespace GoodMorningBot.Jobs
{
    public class MorningMessageJob : IJob
    {
        private readonly MorningMessageService _morningMessageService;

        public MorningMessageJob(MorningMessageService morningMessageService)
        {
            _morningMessageService = morningMessageService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _morningMessageService.SendMorningMessagesAsync();
        }
    }
} 