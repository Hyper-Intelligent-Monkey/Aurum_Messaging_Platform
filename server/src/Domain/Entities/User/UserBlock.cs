using Domain.Exceptions;

namespace Domain.Entities;

public class UserBlock
{
    private UserBlock() { }

    public int Id { get; private set; }

    public int BlockerId { get; private set; }
    public virtual User Blocker { get; private set; } = null!;

    public int BlockedUserId { get; private set; }
    public virtual User BlockedUser { get; private set; } = null!;


    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public static UserBlock Create(int blockerId, int blockedUserId)
    {
        if (blockerId <= 0 || blockedUserId <= 0)
            throw new DomainException("Valid blocker and blocked user IDs are required.");

        if (blockerId == blockedUserId)
            throw new DomainException("You cannot block yourself.");

        return new UserBlock
        {
            BlockerId = blockerId,
            BlockedUserId = blockedUserId,
            CreatedAt = DateTime.UtcNow
        };
    }
}

