using InCleanHome.CommunicationService.Messaging.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Shared;

namespace InCleanHome.CommunicationService.Messaging.Domain.Repositories;

public interface IMessageRepository : IBaseRepository<Message>
{
    Task<IEnumerable<Message>> FindBetweenAsync(int userAId, int userBId);
    Task<IEnumerable<Message>> FindByUserAsync(int userId);
    Task MarkAsReadAsync(int userId, int otherUserId);
}
