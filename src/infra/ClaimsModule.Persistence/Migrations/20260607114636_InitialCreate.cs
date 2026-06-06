using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClaimsModule.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CauseOfLossCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PerilCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauseOfLossCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaimAuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimAuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaimNumberSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CurrentValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PolicyNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReportedDate = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    AssignedHandlerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ClosureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManagerOverrideApplied = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    UserCreated = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserModified = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaimStatusTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiredRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimStatusTransitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CoverageTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaimDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BlobPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    UserCreated = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserModified = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimDocuments_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimParties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartyType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    UserCreated = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserModified = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimParties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimParties_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimReserveComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Component = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    UserCreated = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserModified = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimReserveComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimReserveComponents_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimRiskObjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DamageDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AssetReference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    UserCreated = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserModified = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimRiskObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimRiskObjects_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LossEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LossDate = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    LossDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LossLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CauseOfLossCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EstimatedLossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    ReportDate = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    PoliceReportNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    UserCreated = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserModified = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LossEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LossEvents_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReserveHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReserveComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PreviousBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NewBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PostingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PostingJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChangeSequence = table.Column<int>(type: "int", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReserveHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReserveHistory_ClaimReserveComponents_ReserveComponentId",
                        column: x => x.ReserveComponentId,
                        principalTable: "ClaimReserveComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CauseOfLossCodes",
                columns: new[] { "Id", "Code", "IsActive", "Name", "PerilCategory", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("0ede865f-afdd-884f-32e5-e71f6d514c33"), "COL-OTHER", true, "Other / Unknown", "General", 100 },
                    { new Guid("23f86f7e-861e-c713-0142-0542fae2fcaa"), "COL-FIRE", true, "Fire", "Property", 10 },
                    { new Guid("3f36627b-f5e2-e706-603e-e810376ae345"), "COL-THEFT", true, "Theft", "Crime", 30 },
                    { new Guid("7540981d-8a1e-7ac9-6a82-f08d37c07678"), "COL-FLOOD", true, "Flood", "Weather", 20 },
                    { new Guid("8d5c10c9-40a9-e289-3230-09aa954e9d2d"), "COL-LIAB", true, "Third Party Liability", "Liability", 60 },
                    { new Guid("9f2502dc-2f98-c3a9-29ca-391ba3e082af"), "COL-VEH-COL", true, "Vehicle Collision", "Auto", 40 },
                    { new Guid("aecc8d6a-f9d1-602f-0f61-05161963a438"), "COL-EQUIP", true, "Equipment Breakdown", "Equipment", 70 },
                    { new Guid("af53c41d-96c7-e607-0bca-4e5059c100ef"), "COL-VEH-COMP", true, "Vehicle Comprehensive", "Auto", 50 },
                    { new Guid("e874757c-3ee2-4d51-611e-b8a4b20c28bb"), "COL-INJURY", true, "Bodily Injury", "Liability", 90 },
                    { new Guid("f5a8002e-dbae-a29e-c166-78757f354c2c"), "COL-WIND", true, "Wind / Storm", "Weather", 80 }
                });

            migrationBuilder.InsertData(
                table: "ClaimNumberSequences",
                columns: new[] { "Id", "CurrentValue", "OrganisationId", "Year" },
                values: new object[,]
                {
                    { new Guid("08b02e71-d0ed-83d2-c009-c2a0938ffc58"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2034 },
                    { new Guid("2a401f18-7699-6ae9-fad4-ad02c5baa9b1"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2030 },
                    { new Guid("2f571518-fcb2-c600-394b-65a623093b26"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2031 },
                    { new Guid("5f4023ec-00d9-5943-f7b0-01a501f9b8f7"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2025 },
                    { new Guid("63f0c1f2-a7b2-5d3e-240a-9d64828f9e6e"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2024 },
                    { new Guid("818895e9-2705-1bd4-5a90-19aea13bf92c"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2032 },
                    { new Guid("9a277f30-e5b8-3667-b0b1-ebe3427ade19"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2029 },
                    { new Guid("afc92be1-4632-f977-f3c9-8fc47302b8fe"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2035 },
                    { new Guid("d6928ae7-0fd5-f44c-a360-1eeaa0571df5"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2033 },
                    { new Guid("e061b649-dde3-2e8e-c570-61d289301dc0"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2028 },
                    { new Guid("e1f56050-d0b4-253b-6883-5263b1c1938f"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2027 },
                    { new Guid("f451fb0f-bebf-c15b-2330-770ce18b438c"), 0L, new Guid("11111111-1111-1111-1111-111111111111"), 2026 }
                });

            migrationBuilder.InsertData(
                table: "ClaimStatusTransitions",
                columns: new[] { "Id", "FromStatus", "RequiredRole", "ToStatus" },
                values: new object[,]
                {
                    { new Guid("0ab35873-474c-bb1f-3baa-7ace617d03c8"), "Open", null, "PendingPayment" },
                    { new Guid("15b98e75-4afc-ba8c-8ae9-e9ca20708c9d"), "Draft", null, "Open" },
                    { new Guid("4a45b070-a8b7-9999-53a2-b0a0710b1aac"), "UnderInvestigation", null, "Open" },
                    { new Guid("4a9cae82-d2a4-9825-3c6a-e0ec55fb4247"), "UnderInvestigation", null, "Closed" },
                    { new Guid("5c83eb55-a453-fe99-f6e6-497539a85cec"), "UnderInvestigation", null, "PendingPayment" },
                    { new Guid("7203b252-7106-b7b2-23b9-cc14360ad7c5"), "Open", null, "Closed" },
                    { new Guid("842d4c33-ac02-3d4b-f2bd-0e6f7f58fd68"), "Open", null, "UnderInvestigation" },
                    { new Guid("943e1a50-9725-026b-6ace-9e33b4d66427"), "Closed", "Supervisor", "Reopened" },
                    { new Guid("98802910-00dd-ecd9-86f1-cf156732f01b"), "UnderInvestigation", null, "Withdrawn" },
                    { new Guid("9f71666c-e9b6-1358-12c7-ce5b4b8b56b5"), "Reopened", null, "Open" },
                    { new Guid("b39c90d3-22d7-3822-93ef-1bb289d880e4"), "PendingPayment", null, "Closed" },
                    { new Guid("eaac4a84-7cf4-9c56-790b-c61225c1f7b3"), "Open", null, "Withdrawn" }
                });

            migrationBuilder.InsertData(
                table: "Policies",
                columns: new[] { "Id", "ClientName", "CoverageTypes", "EffectiveDate", "ExpirationDate", "OrganisationId", "PolicyNumber", "Status" },
                values: new object[,]
                {
                    { new Guid("0cf86c04-2a25-343e-2c4f-4fa6c7e0a02b"), "Coastal Builders Group", "Property,Equipment", new DateOnly(2025, 3, 1), new DateOnly(2027, 2, 28), new Guid("11111111-1111-1111-1111-111111111111"), "POL-2025-002001", "Active" },
                    { new Guid("127e03a8-ef89-cce5-1928-fe92094dfa60"), "Stanton Medical Group", "Liability,Vehicle", new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31), new Guid("11111111-1111-1111-1111-111111111111"), "POL-2025-002002", "Active" },
                    { new Guid("1947fcaf-f596-9f66-7d69-c4c8429c82df"), "Harborview Properties Inc", "Property,Liability", new DateOnly(2024, 6, 1), new DateOnly(2026, 5, 31), new Guid("11111111-1111-1111-1111-111111111111"), "POL-2024-001002", "Active" },
                    { new Guid("1e71f5e6-5993-9788-964e-5c080745dd72"), "Meridian Transport LLC", "Vehicle,Cargo", new DateOnly(2024, 1, 1), new DateOnly(2026, 12, 31), new Guid("11111111-1111-1111-1111-111111111111"), "POL-2024-001001", "Active" },
                    { new Guid("e39a965c-f3de-4b7d-be17-a46cffe0827e"), "Archived Corp", "Property", new DateOnly(2020, 1, 1), new DateOnly(2021, 12, 31), new Guid("11111111-1111-1111-1111-111111111111"), "POL-2023-000099", "Expired" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CauseOfLossCodes_Code",
                table: "CauseOfLossCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAuditLog_ClaimId_CreatedAt",
                table: "ClaimAuditLog",
                columns: new[] { "ClaimId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAuditLog_EventType",
                table: "ClaimAuditLog",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAuditLog_RelatedEntityId",
                table: "ClaimAuditLog",
                column: "RelatedEntityId",
                unique: true,
                filter: "[EventType] = 'GL_POSTING_SIMULATED'");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_ClaimId",
                table: "ClaimDocuments",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimNumberSequences_OrganisationId_Year",
                table: "ClaimNumberSequences",
                columns: new[] { "OrganisationId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaimParties_ClaimId",
                table: "ClaimParties",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimReserveComponents_ClaimId",
                table: "ClaimReserveComponents",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimReserveComponents_ClaimId_Component",
                table: "ClaimReserveComponents",
                columns: new[] { "ClaimId", "Component" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimRiskObjects_ClaimId",
                table: "ClaimRiskObjects",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_AssignedHandlerId",
                table: "Claims",
                column: "AssignedHandlerId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_OrganisationId_ClaimNumber",
                table: "Claims",
                columns: new[] { "OrganisationId", "ClaimNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Claims_PolicyId",
                table: "Claims",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status",
                table: "Claims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimStatusTransitions_FromStatus_ToStatus",
                table: "ClaimStatusTransitions",
                columns: new[] { "FromStatus", "ToStatus" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_OrganisationId_Key",
                table: "IdempotencyRecords",
                columns: new[] { "OrganisationId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LossEvents_CauseOfLossCode",
                table: "LossEvents",
                column: "CauseOfLossCode");

            migrationBuilder.CreateIndex(
                name: "IX_LossEvents_ClaimId",
                table: "LossEvents",
                column: "ClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_OrganisationId_PolicyNumber",
                table: "Policies",
                columns: new[] { "OrganisationId", "PolicyNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReserveHistory_ClaimId",
                table: "ReserveHistory",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ReserveHistory_IdempotencyKey",
                table: "ReserveHistory",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReserveHistory_ReserveComponentId_ChangeSequence",
                table: "ReserveHistory",
                columns: new[] { "ReserveComponentId", "ChangeSequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CauseOfLossCodes");

            migrationBuilder.DropTable(
                name: "ClaimAuditLog");

            migrationBuilder.DropTable(
                name: "ClaimDocuments");

            migrationBuilder.DropTable(
                name: "ClaimNumberSequences");

            migrationBuilder.DropTable(
                name: "ClaimParties");

            migrationBuilder.DropTable(
                name: "ClaimRiskObjects");

            migrationBuilder.DropTable(
                name: "ClaimStatusTransitions");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropTable(
                name: "LossEvents");

            migrationBuilder.DropTable(
                name: "Policies");

            migrationBuilder.DropTable(
                name: "ReserveHistory");

            migrationBuilder.DropTable(
                name: "ClaimReserveComponents");

            migrationBuilder.DropTable(
                name: "Claims");
        }
    }
}
