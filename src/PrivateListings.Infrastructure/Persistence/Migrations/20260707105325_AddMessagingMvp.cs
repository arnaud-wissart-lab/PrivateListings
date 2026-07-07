using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace PrivateListings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversation_read_states",
                columns: table => new
                {
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_marked_unread = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_read_states", x => new { x.conversation_id, x.user_profile_id });
                    table.CheckConstraint("ck_conversation_read_states_last_read_after_created", "last_read_at IS NULL OR last_read_at >= created_at");
                    table.CheckConstraint("ck_conversation_read_states_updated_after_created", "updated_at IS NULL OR updated_at >= created_at");
                    table.ForeignKey(
                        name: "FK_conversation_read_states_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_conversation_read_states_user_profiles_user_profile_id",
                        column: x => x.user_profile_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_blocks",
                columns: table => new
                {
                    blocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocked_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_blocks", x => new { x.blocker_id, x.blocked_id });
                    table.CheckConstraint("ck_user_blocks_distinct_users", "blocker_id <> blocked_id");
                    table.CheckConstraint("ck_user_blocks_revoked_after_created", "revoked_at IS NULL OR revoked_at >= created_at");
                    table.ForeignKey(
                        name: "FK_user_blocks_user_profiles_blocked_id",
                        column: x => x.blocked_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_blocks_user_profiles_blocker_id",
                        column: x => x.blocker_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO conversation_read_states (
                    conversation_id,
                    user_profile_id,
                    created_at,
                    last_read_at,
                    is_marked_unread,
                    updated_at)
                SELECT
                    conversations.id,
                    participant_id,
                    conversations.created_at,
                    NULL,
                    FALSE,
                    NULL
                FROM conversations
                CROSS JOIN LATERAL unnest(conversations.participant_ids) AS participant_id
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_conversation_read_states_user_unread",
                table: "conversation_read_states",
                columns: new[] { "user_profile_id", "is_marked_unread" });

            migrationBuilder.CreateIndex(
                name: "ix_user_blocks_blocked_active",
                table: "user_blocks",
                columns: new[] { "blocked_id", "revoked_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_read_states");

            migrationBuilder.DropTable(
                name: "user_blocks");
        }
    }
}
