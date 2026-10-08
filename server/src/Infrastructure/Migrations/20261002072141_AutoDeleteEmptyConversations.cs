using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AutoDeleteEmptyConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION delete_empty_conversations()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM conversation_participants 
                        WHERE ""ConversationId"" = OLD.""ConversationId""
                    ) THEN
                        DELETE FROM conversations WHERE ""Id"" = OLD.""ConversationId"";
                    END IF;
                    RETURN OLD;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_delete_empty_conversations ON conversation_participants;
                CREATE TRIGGER trg_delete_empty_conversations
                AFTER DELETE ON conversation_participants
                FOR EACH ROW
                EXECUTE FUNCTION delete_empty_conversations();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_delete_empty_conversations ON conversation_participants;
                DROP FUNCTION IF EXISTS delete_empty_conversations();
            ");
        }
    }
}
