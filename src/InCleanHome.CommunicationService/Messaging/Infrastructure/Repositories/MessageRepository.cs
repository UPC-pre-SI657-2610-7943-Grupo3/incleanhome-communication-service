using InCleanHome.CommunicationService.Infrastructure.Persistence;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Messaging.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.CommunicationService.Messaging.Infrastructure.Repositories;

public class MessageRepository(CommunicationDbContext context)
    : BaseRepository<Message>(context), IMessageRepository
{
    public async Task<IEnumerable<Message>> FindBetweenAsync(int userAId, int userBId)
        => await Context.Set<Message>()
            .Where(m =>
                (m.SenderId == userAId && m.RecipientId == userBId) ||
                (m.SenderId == userBId && m.RecipientId == userAId))
            .OrderBy(m => m.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Message>> FindByUserAsync(int userId)
        => await Context.Set<Message>()
            .Where(m => m.SenderId == userId || m.RecipientId == userId)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();

    public async Task MarkAsReadAsync(int userId, int otherUserId)
    {
        var unread = Context.Set<Message>()
            .Where(m => m.SenderId == otherUserId && m.RecipientId == userId && m.ReadAt == null);

        await unread.ForEachAsync(m => m.MarkAsRead());
    }
}
