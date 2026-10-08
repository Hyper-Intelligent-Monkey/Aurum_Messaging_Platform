using Application.Common.Interfaces.Persistence.Repositories;
using Application.Common.Interfaces.Persistence;

namespace Infrastructure.Persistence;

public class UnitOfWork : IUnitofWork
{
    private readonly ApplicationDbContext _context;

    public IUserRepository Users {get; }
    public IConversationRepository Conversations {get; }
    public IMessageRepository Messages {get; }

    public UnitOfWork(
        ApplicationDbContext context,
        IUserRepository users,
        IConversationRepository conversations,
        IMessageRepository messages
        )
    {
        _context = context;
        Users = users;
        Conversations = conversations;
        Messages = messages;
    }

    // saves all changes to the database
    public async Task<int> SaveChanges(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}