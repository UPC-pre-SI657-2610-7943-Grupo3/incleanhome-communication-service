using InCleanHome.CommunicationService.Infrastructure.ExternalServices.ProfileService;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Queries;
using InCleanHome.CommunicationService.Messaging.Domain.Repositories;
using InCleanHome.CommunicationService.Messaging.Domain.Services;

namespace InCleanHome.CommunicationService.Messaging.Application.QueryServices;

public class MessageQueryService(
    IMessageRepository repository,
    IProfileServiceClient profileClient,
    IHttpContextAccessor httpContextAccessor) : IMessageQueryService
{
    public async Task<IEnumerable<Message>> Handle(GetMessagesBetweenQuery query)
        => await repository.FindBetweenAsync(query.UserAId, query.UserBId);

    public async Task<IEnumerable<ConversationView>> Handle(GetConversationsForUserQuery query)
    {
        var all = (await repository.FindByUserAsync(query.UserId)).ToList();

        var conversations = all
            .GroupBy(m => m.SenderId == query.UserId ? m.RecipientId : m.SenderId)
            .Select(g =>
            {
                var ordered = g.OrderByDescending(m => m.CreatedDate).ToList();
                var last    = ordered.First();
                var unread  = ordered.Count(m => m.RecipientId == query.UserId && m.ReadAt == null);
                return new { OtherId = g.Key, Last = last, Unread = unread };
            })
            .OrderByDescending(c => c.Last.CreatedDate)
            .ToList();

        var bearer = GetBearer();
        var result = new List<ConversationView>();
        foreach (var c in conversations)
        {
            var person = await profileClient.GetAnyPersonAsync(c.OtherId, bearer);
            var name   = string.IsNullOrEmpty(person?.Name) ? $"User {c.OtherId}" : person!.Name;
            result.Add(new ConversationView(
                c.OtherId, name, person?.PhotoUrl,
                c.Last.Content, c.Last.CreatedDate, c.Unread));
        }
        return result;
    }

    private string GetBearer()
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null) return string.Empty;
        var raw = http.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        return raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? raw["Bearer ".Length..] : raw;
    }
}
