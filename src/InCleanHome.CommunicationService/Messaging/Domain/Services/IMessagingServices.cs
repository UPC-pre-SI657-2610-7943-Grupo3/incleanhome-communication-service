using InCleanHome.CommunicationService.Messaging.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Commands;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Queries;

namespace InCleanHome.CommunicationService.Messaging.Domain.Services;

public interface IMessageCommandService
{
    Task<Message> Handle(SendMessageCommand command);
    Task Handle(MarkConversationAsReadCommand command);
}

public interface IMessageQueryService
{
    Task<IEnumerable<Message>> Handle(GetMessagesBetweenQuery query);
    Task<IEnumerable<ConversationView>> Handle(GetConversationsForUserQuery query);
}
