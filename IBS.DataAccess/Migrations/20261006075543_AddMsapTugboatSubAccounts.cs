using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMsapTugboatSubAccounts: Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "msap_tugboat_owners",
                columns: table => new
                {
                    tugboat_owner_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tugboat_owner_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    tugboat_owner_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    fixed_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    msap_recid = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_msap_tugboat_owners", x => x.tugboat_owner_id);
                });

            migrationBuilder.CreateTable(
                name: "msap_tugboats",
                columns: table => new
                {
                    tugboat_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tugboat_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    tugboat_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    is_company_owned = table.Column<bool>(type: "boolean", nullable: false),
                    tugboat_owner_id = table.Column<int>(type: "integer", nullable: true),
                    port_id = table.Column<int>(type: "integer", nullable: false),
                    msap_recid = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_msap_tugboats", x => x.tugboat_id);
                    table.ForeignKey(
                        name: "fk_msap_tugboats_msap_tugboat_owners_tugboat_owner_id",
                        column: x => x.tugboat_owner_id,
                        principalTable: "msap_tugboat_owners",
                        principalColumn: "tugboat_owner_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_msap_tugboats_tugboat_owner_id",
                table: "msap_tugboats",
                column: "tugboat_owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "msap_tugboats");

            migrationBuilder.DropTable(
                name: "msap_tugboat_owners");
        }
    }
}
