using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.Models;

namespace VyinChatSdk.Internal.Domain.Repositories
{
    public interface IMessageRepository
    {
        Task<MessageBO> SendMessageAsync(
            string channelUrl,
            VcUserMessageCreateParams createParams,
            CancellationToken cancellationToken = default);
    }
}
