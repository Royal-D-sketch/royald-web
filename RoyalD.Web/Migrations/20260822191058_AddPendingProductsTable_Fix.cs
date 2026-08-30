using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoyalD.Web.Migrations
{
    public partial class AddPendingProductsTable_Fix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    District = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    IPAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Latitude = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Longitude = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Area = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    District = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerCode);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SalesRepCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AllowedRegion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AllowedProvinces = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AllowedDistricts = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CanDownloadOrScreenCapture = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentSessionToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SessionTimeoutMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutstandingDebts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    District = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BillNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BillDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Credit = table.Column<int>(type: "INTEGER", nullable: false),
                    SalesRep = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FullyPaidDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PostponedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BadDebtDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BadDebtAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeliveringDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WaitingGoodsDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReturnAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsReturnCutFromBill = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutstandingDebts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutstandingDebts_Customers_CustomerCode",
                        column: x => x.CustomerCode,
                        principalTable: "Customers",
                        principalColumn: "CustomerCode");
                });

            migrationBuilder.CreateTable(
                name: "SalesBills",
                columns: table => new
                {
                    BillNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BillDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CustomerCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    District = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Credit = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalesRep = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SourceMonth = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesBills", x => x.BillNo);
                    table.ForeignKey(
                        name: "FK_SalesBills_Customers_CustomerCode",
                        column: x => x.CustomerCode,
                        principalTable: "Customers",
                        principalColumn: "CustomerCode");
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OutstandingDebtId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    CheckStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    CheckNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CheckDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_OutstandingDebts_OutstandingDebtId",
                        column: x => x.OutstandingDebtId,
                        principalTable: "OutstandingDebts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OutstandingDebtId = table.Column<int>(type: "INTEGER", nullable: false),
                    BillNo = table.Column<string>(type: "TEXT", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingProducts_OutstandingDebts_OutstandingDebtId",
                        column: x => x.OutstandingDebtId,
                        principalTable: "OutstandingDebts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesBillItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BillNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesBillItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesBillItems_SalesBills_BillNo",
                        column: x => x.BillNo,
                        principalTable: "SalesBills",
                        principalColumn: "BillNo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OutstandingDebtId = table.Column<int>(type: "INTEGER", nullable: true),
                    PaymentRecordId = table.Column<int>(type: "INTEGER", nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UploadedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAttachments_OutstandingDebts_OutstandingDebtId",
                        column: x => x.OutstandingDebtId,
                        principalTable: "OutstandingDebts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileAttachments_PaymentRecords_PaymentRecordId",
                        column: x => x.PaymentRecordId,
                        principalTable: "PaymentRecords",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_OutstandingDebtId",
                table: "FileAttachments",
                column: "OutstandingDebtId");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_PaymentRecordId",
                table: "FileAttachments",
                column: "PaymentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_OutstandingDebts_CustomerCode",
                table: "OutstandingDebts",
                column: "CustomerCode");

            migrationBuilder.CreateIndex(
                name: "IX_OutstandingDebts_SalesRep",
                table: "OutstandingDebts",
                column: "SalesRep");

            migrationBuilder.CreateIndex(
                name: "IX_OutstandingDebts_Status",
                table: "OutstandingDebts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_OutstandingDebtId",
                table: "PaymentRecords",
                column: "OutstandingDebtId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingProducts_OutstandingDebtId",
                table: "PendingProducts",
                column: "OutstandingDebtId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesBillItems_BillNo",
                table: "SalesBillItems",
                column: "BillNo");

            migrationBuilder.CreateIndex(
                name: "IX_SalesBills_BillDate",
                table: "SalesBills",
                column: "BillDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesBills_CustomerCode",
                table: "SalesBills",
                column: "CustomerCode");

            migrationBuilder.CreateIndex(
                name: "IX_SalesBills_SalesRep",
                table: "SalesBills",
                column: "SalesRep");

            migrationBuilder.CreateIndex(
                name: "IX_SalesBills_SourceMonth",
                table: "SalesBills",
                column: "SourceMonth");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "FileAttachments");

            migrationBuilder.DropTable(
                name: "PendingProducts");

            migrationBuilder.DropTable(
                name: "SalesBillItems");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "SalesBills");

            migrationBuilder.DropTable(
                name: "OutstandingDebts");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
