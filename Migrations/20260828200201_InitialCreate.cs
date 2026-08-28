using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xml.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Chairs",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chairs", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Discs",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discs", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Preps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "BuildingsRooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    RoomNumber = table.Column<string>(type: "text", nullable: false),
                    BuildingName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingsRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildingsRooms_Buildings_BuildingName",
                        column: x => x.BuildingName,
                        principalTable: "Buildings",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    IdSubg = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Week = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    Less = table.Column<int>(type: "integer", nullable: false),
                    PrepId = table.Column<string>(type: "text", nullable: true),
                    GroupId = table.Column<string>(type: "text", nullable: true),
                    DiscName = table.Column<string>(type: "text", nullable: true),
                    ChairName = table.Column<string>(type: "text", nullable: true),
                    RoomId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.IdSubg);
                    table.ForeignKey(
                        name: "FK_Events_BuildingsRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "BuildingsRooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Events_Chairs_ChairName",
                        column: x => x.ChairName,
                        principalTable: "Chairs",
                        principalColumn: "Name");
                    table.ForeignKey(
                        name: "FK_Events_Discs_DiscName",
                        column: x => x.DiscName,
                        principalTable: "Discs",
                        principalColumn: "Name");
                    table.ForeignKey(
                        name: "FK_Events_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Events_Preps_PrepId",
                        column: x => x.PrepId,
                        principalTable: "Preps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingsRooms_BuildingName",
                table: "BuildingsRooms",
                column: "BuildingName");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ChairName",
                table: "Events",
                column: "ChairName");

            migrationBuilder.CreateIndex(
                name: "IX_Events_DiscName",
                table: "Events",
                column: "DiscName");

            migrationBuilder.CreateIndex(
                name: "IX_Events_GroupId",
                table: "Events",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_PrepId",
                table: "Events",
                column: "PrepId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_RoomId",
                table: "Events",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Week_Day_Less",
                table: "Events",
                columns: new[] { "Week", "Day", "Less" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "BuildingsRooms");

            migrationBuilder.DropTable(
                name: "Chairs");

            migrationBuilder.DropTable(
                name: "Discs");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Preps");

            migrationBuilder.DropTable(
                name: "Buildings");
        }
    }
}
