using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RacingTicketsAgent.Agent
{
    public interface ILlmClient
    {
        Task<string> ChatAsync(string userPrompt, CancellationToken ct = default);
        Task<bool> IsAvailableAsync();
    }
}
