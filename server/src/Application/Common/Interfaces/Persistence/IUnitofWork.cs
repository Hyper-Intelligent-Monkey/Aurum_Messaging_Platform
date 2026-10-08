using Application.Common.Interfaces.Persistence.Repositories;

namespace Application.Common.Interfaces.Persistence;

public interface IUnitofWork
{
    IUserRepository Users { get; }
    IConversationRepository Conversations { get; }
    IMessageRepository Messages { get; }

    Task<int> SaveChanges(CancellationToken cancellationToken = default);
}