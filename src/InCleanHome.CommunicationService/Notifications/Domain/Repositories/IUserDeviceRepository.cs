using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Shared;

namespace InCleanHome.CommunicationService.Notifications.Domain.Repositories;

public interface IUserDeviceRepository : IBaseRepository<UserDevice>
{
    Task<UserDevice?> FindByUserIdAsync(int userId);
    Task<string?> FindTokenByUserIdAsync(int userId);
    Task<string?> FindRoleByUserIdAsync(int userId);
}
