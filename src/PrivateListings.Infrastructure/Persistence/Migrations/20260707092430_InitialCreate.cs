using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace PrivateListings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "listing_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_address = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    anonymized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    subject_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_user_profiles_actor_user_profile_id",
                        column: x => x.actor_user_profile_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consent_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_granted = table.Column<bool>(type: "boolean", nullable: false),
                    policy_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_consent_records_user_profiles_user_profile_id",
                        column: x => x.user_profile_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "data_export_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    storage_key = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    download_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_export_requests", x => x.id);
                    table.CheckConstraint("ck_data_export_requests_download_expires_after_completed", "download_expires_at IS NULL OR completed_at IS NULL OR download_expires_at > completed_at");
                    table.ForeignKey(
                        name: "FK_data_export_requests_user_profiles_user_profile_id",
                        column: x => x.user_profile_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "demo_user_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_user_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_demo_user_accounts_user_profiles_user_profile_id",
                        column: x => x.user_profile_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspension_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listings", x => x.id);
                    table.CheckConstraint("ck_listings_expires_after_created", "expires_at IS NULL OR expires_at > created_at");
                    table.CheckConstraint("ck_listings_published_after_created", "published_at IS NULL OR published_at >= created_at");
                    table.ForeignKey(
                        name: "FK_listings_listing_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "listing_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listings_user_profiles_owner_id",
                        column: x => x.owner_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.id);
                    table.CheckConstraint("ck_reports_reviewed_after_created", "reviewed_at IS NULL OR reviewed_at >= created_at");
                    table.ForeignKey(
                        name: "FK_reports_user_profiles_reporter_id",
                        column: x => x.reporter_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reports_user_profiles_reviewed_by_id",
                        column: x => x.reviewed_by_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    participant_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.id);
                    table.CheckConstraint("ck_conversations_closed_after_created", "closed_at IS NULL OR closed_at >= created_at");
                    table.CheckConstraint("ck_conversations_have_two_participants", "cardinality(participant_ids) >= 2");
                    table.ForeignKey(
                        name: "FK_conversations_listings_listing_id",
                        column: x => x.listing_id,
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "listing_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_in_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    alt_text = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_images", x => x.id);
                    table.CheckConstraint("ck_listing_images_size_positive", "size_in_bytes > 0");
                    table.CheckConstraint("ck_listing_images_sort_order_positive", "sort_order >= 0");
                    table.ForeignKey(
                        name: "FK_listing_images_listings_listing_id",
                        column: x => x.listing_id,
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recommendations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dismissed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendations", x => x.id);
                    table.CheckConstraint("ck_recommendations_score_range", "score >= 0 AND score <= 1");
                    table.ForeignKey(
                        name: "FK_recommendations_listings_listing_id",
                        column: x => x.listing_id,
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recommendations_user_profiles_user_profile_id",
                        column: x => x.user_profile_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.id);
                    table.CheckConstraint("ck_messages_deleted_after_sent", "deleted_at IS NULL OR deleted_at >= sent_at");
                    table.ForeignKey(
                        name: "FK_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messages_user_profiles_sender_id",
                        column: x => x.sender_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_user_profile_id",
                table: "audit_logs",
                column: "actor_user_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_occurred_at",
                table: "audit_logs",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_subject_key",
                table: "audit_logs",
                column: "subject_key");

            migrationBuilder.CreateIndex(
                name: "ix_consent_records_user_type_recorded_at",
                table: "consent_records",
                columns: new[] { "user_profile_id", "type", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_created_at",
                table: "conversations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_listing_id",
                table: "conversations",
                column: "listing_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_participant_ids",
                table: "conversations",
                column: "participant_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_data_export_requests_status",
                table: "data_export_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_data_export_requests_user_requested_at",
                table: "data_export_requests",
                columns: new[] { "user_profile_id", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "ix_demo_user_accounts_role",
                table: "demo_user_accounts",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ux_demo_user_accounts_email",
                table: "demo_user_accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_demo_user_accounts_user_profile_id",
                table: "demo_user_accounts",
                column: "user_profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_listing_categories_is_active",
                table: "listing_categories",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_listing_categories_name",
                table: "listing_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_listing_images_listing_sort_order",
                table: "listing_images",
                columns: new[] { "listing_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_listing_images_storage_key",
                table: "listing_images",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_listings_category_status",
                table: "listings",
                columns: new[] { "category_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_listings_created_at",
                table: "listings",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_listings_owner_status",
                table: "listings",
                columns: new[] { "owner_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_listings_status_published_at",
                table: "listings",
                columns: new[] { "status", "published_at" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_sent_at",
                table: "messages",
                columns: new[] { "conversation_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_sender_sent_at",
                table: "messages",
                columns: new[] { "sender_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_recommendations_listing_id",
                table: "recommendations",
                column: "listing_id");

            migrationBuilder.CreateIndex(
                name: "ix_recommendations_user_score",
                table: "recommendations",
                columns: new[] { "user_profile_id", "score" });

            migrationBuilder.CreateIndex(
                name: "ux_recommendations_user_listing",
                table: "recommendations",
                columns: new[] { "user_profile_id", "listing_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reports_reporter_id",
                table: "reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_reviewed_by_id",
                table: "reports",
                column: "reviewed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_status_created_at",
                table: "reports",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_target_key",
                table: "reports",
                column: "target_key");

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_anonymized_at",
                table: "user_profiles",
                column: "anonymized_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_created_at",
                table: "user_profiles",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ux_user_profiles_email_address",
                table: "user_profiles",
                column: "email_address",
                unique: true,
                filter: "email_address IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "consent_records");

            migrationBuilder.DropTable(
                name: "data_export_requests");

            migrationBuilder.DropTable(
                name: "demo_user_accounts");

            migrationBuilder.DropTable(
                name: "listing_images");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "recommendations");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropTable(
                name: "listings");

            migrationBuilder.DropTable(
                name: "listing_categories");

            migrationBuilder.DropTable(
                name: "user_profiles");
        }
    }
}
