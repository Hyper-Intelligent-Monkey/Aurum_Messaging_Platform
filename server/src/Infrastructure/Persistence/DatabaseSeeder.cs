using Application.Common.Interfaces.Security;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default
    )
    {
        var existingUsernames = await context.Users
            .Select(u => u.Username.ToLower())
            .ToHashSetAsync(cancellationToken);

        var hashedPassword = passwordHasher.HashPassword("password");

        User mainUser;
        if (existingUsernames.Contains("bob_steven"))
        {
            mainUser = await context.Users.FirstAsync(u => u.Username.ToLower() == "bob_steven", cancellationToken);
        }
        else
        {
            mainUser = User.Create("bob_steven", "bobsteven@example.com", hashedPassword);
            mainUser.ConfirmEmail();
            await context.Users.AddAsync(mainUser, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            existingUsernames.Add("bob_steven");
        }

        var contactedNames = new[]
        {
            "charlie", "david", "emma", "frank", "grace",
            "henry", "isabella", "jack", "kate", "liam",

            "aaron", "amber", "benjamin", "chloe", "daniel",
            "elena", "felix", "george", "hannah", "ian",
            "julia", "kevin", "laura", "marcus", "natalie",
            "oscar", "penelope", "riley", "sophia", "thomas",
            "uriel", "vanessa", "wyatt", "xavier", "yasmin",
            "zachary", "arthur", "beatrice", "connor", "daphne"
        };

        var uncontactedNames = new[]
        {
            "noah", "olivia", "paul", "quinn", "ruby",
            "samuel", "tara", "victor", "wendy", "zane",

            "adam", "brian", "clara", "derek", "elizabeth",
            "fiona", "gabriel", "harper", "isaac", "jessica",
            "kyle", "lucas", "maya", "nathan", "owen",
            "paige", "rebecca", "simon", "tristan", "ursula",
            "vincent", "walter", "xander", "yvonne", "zelda",
            "aidan", "brooke", "colin", "diana", "ethan"
        };

        // Load existing users, create new users
        var allContactedUsers = new List<User>();
        var newContactedUsers = new List<User>();

        foreach (var name in contactedNames)
        {
            if (existingUsernames.Contains(name.ToLower()))
            {
                var existing = await context.Users.FirstAsync(u => u.Username.ToLower() == name.ToLower(), cancellationToken);
                allContactedUsers.Add(existing);
            }
            else
            {
                // create a brand new user that has not existed before
                var newUser = User.Create(name, $"{name}@example.com", hashedPassword);
                newUser.ConfirmEmail();
                newContactedUsers.Add(newUser);
                allContactedUsers.Add(newUser);
                existingUsernames.Add(name.ToLower());
            }
        }

        if (newContactedUsers.Count > 0)
        {
            await context.Users.AddRangeAsync(newContactedUsers, cancellationToken);
        }

        var newUncontactedUsers = new List<User>();
        foreach (var name in uncontactedNames)
        {
            if (existingUsernames.Contains(name.ToLower()))
            {
                continue;
            }
            var user = User.Create(name, $"{name}@example.com", hashedPassword);
            user.ConfirmEmail();
            newUncontactedUsers.Add(user);
            existingUsernames.Add(name.ToLower());
        }
        if (newUncontactedUsers.Count > 0)
        {
            await context.Users.AddRangeAsync(newUncontactedUsers, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        // 5. Ensure conversations exist between mainUser and each contacted user
        for (int i = 0; i < allContactedUsers.Count; i++)
        {
            var otherUser = allContactedUsers[i];

            // Check if a direct conversation already exists between mainUser and otherUser
            var conversationExists = await context.Conversations.AnyAsync(c =>
                c.Participants.Count == 2 &&
                c.Participants.Any(p => p.UserId == mainUser.Id) &&
                c.Participants.Any(p => p.UserId == otherUser.Id),
                cancellationToken);

            // If a conversation already exists from a previous run, do not duplicate it
            if (conversationExists)
            {
                continue;
            }

            var conversation = Conversation.CreateConversation(mainUser, otherUser);
            await context.Conversations.AddAsync(conversation, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            
            // create a sample message for the conversation
            var sampleText = $"Hello {mainUser.Username}, this is {otherUser.Username}. How are you doing today?";
            var message = Message.Create(
                senderId: otherUser.Id,
                conversationId: conversation.Id,
                content: sampleText,
                type: MessageType.Text
            );

            await context.Messages.AddAsync(message, cancellationToken);
            conversation.UpdateLastMessage(message);
            await context.SaveChangesAsync(cancellationToken);
        }
        
        // safety measure if something doesn't get saved above
        await context.SaveChangesAsync(cancellationToken);

    }
}