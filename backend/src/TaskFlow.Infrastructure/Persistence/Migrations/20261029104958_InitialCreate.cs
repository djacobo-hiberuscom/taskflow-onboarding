using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    keycloak_sub = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project", x => x.id);
                    table.CheckConstraint("ck_project_description_length", "description IS NULL OR char_length(description) <= 5000");
                    table.CheckConstraint("ck_project_name_not_empty", "name ~ '[^[:space:]]'");
                    table.ForeignKey(
                        name: "FK_project_app_user_created_by",
                        column: x => x.created_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "app_user_project",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user_project", x => new { x.project_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_app_user_project_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_app_user_project_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "ToDo"),
                    priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task", x => x.id);
                    table.CheckConstraint("ck_task_description_length", "description IS NULL OR char_length(description) <= 5000");
                    table.CheckConstraint("ck_task_priority", "priority IN ('Low', 'Medium', 'High')");
                    table.CheckConstraint("ck_task_status", "status IN ('ToDo', 'InProgress', 'Done')");
                    table.CheckConstraint("ck_task_title_not_empty", "title ~ '[^[:space:]]'");
                    table.ForeignKey(
                        name: "FK_task_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_task_app_user_project_assignment",
                        columns: x => new { x.project_id, x.assigned_id },
                        principalTable: "app_user_project",
                        principalColumns: new[] { "project_id", "user_id" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_user_keycloak_sub",
                table: "app_user",
                column: "keycloak_sub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_user_project_user_id_project_id",
                table: "app_user_project",
                columns: new[] { "user_id", "project_id" });

            migrationBuilder.CreateIndex(
                name: "IX_project_archived_at",
                table: "project",
                column: "archived_at");

            migrationBuilder.CreateIndex(
                name: "IX_project_created_by",
                table: "project",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_task_assigned_id",
                table: "task",
                column: "assigned_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_project_id_assigned_id",
                table: "task",
                columns: new[] { "project_id", "assigned_id" });

            migrationBuilder.Sql("""
                ALTER TABLE task
                    DROP CONSTRAINT fk_task_app_user_project_assignment;

                ALTER TABLE task
                    ADD CONSTRAINT fk_task_app_user_project_assignment
                    FOREIGN KEY (project_id, assigned_id)
                    REFERENCES app_user_project (project_id, user_id)
                    ON DELETE SET NULL (assigned_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task");

            migrationBuilder.DropTable(
                name: "app_user_project");

            migrationBuilder.DropTable(
                name: "project");

            migrationBuilder.DropTable(
                name: "app_user");
        }
    }
}
