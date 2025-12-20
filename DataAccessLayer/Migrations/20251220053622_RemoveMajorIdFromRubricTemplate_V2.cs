using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMajorIdFromRubricTemplate_V2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- BẮT ĐẦU ĐOẠN CODE THÊM THỦ CÔNG ---

            // 1. Xóa khóa ngoại (nếu database còn)
            // Dùng try-catch hoặc sql thuần để tránh lỗi nếu khóa ngoại đã mất
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_RubricTemplates_Majors_MajorId') ALTER TABLE [RubricTemplates] DROP CONSTRAINT [FK_RubricTemplates_Majors_MajorId]");

            // 2. Xóa Index (nếu có)
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RubricTemplates_MajorId' AND object_id = OBJECT_ID('RubricTemplates')) DROP INDEX [IX_RubricTemplates_MajorId] ON [RubricTemplates]");

            // 3. Xóa cột MajorId
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.columns WHERE name = 'MajorId' AND object_id = OBJECT_ID('RubricTemplates')) ALTER TABLE [RubricTemplates] DROP COLUMN [MajorId]");

            // --- KẾT THÚC ĐOẠN CODE THÊM THỦ CÔNG ---


            // Các lệnh update data cũ của bạn (giữ nguyên)
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "3b25375c-904a-48fd-846f-7045f88f394c", new DateTime(2025, 12, 20, 5, 36, 22, 70, DateTimeKind.Utc).AddTicks(7561), "3d63626b-6266-41ce-8c64-f2680a516964" });

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 100,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 36, 22, 70, DateTimeKind.Utc).AddTicks(7607));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 101,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 36, 22, 70, DateTimeKind.Utc).AddTicks(7608));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 102,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 36, 22, 70, DateTimeKind.Utc).AddTicks(7609));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 103,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 36, 22, 70, DateTimeKind.Utc).AddTicks(7610));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 104,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 36, 22, 70, DateTimeKind.Utc).AddTicks(7610));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 105,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 36, 22, 70, DateTimeKind.Utc).AddTicks(7611));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 106,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 36, 22, 70, DateTimeKind.Utc).AddTicks(7612));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "15eca881-669c-4e82-9be1-9ac4c24a7039", new DateTime(2025, 12, 20, 5, 3, 20, 355, DateTimeKind.Utc).AddTicks(4712), "e1a10644-c1b1-4c7c-88fa-1e87031b797e" });

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 100,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 3, 20, 355, DateTimeKind.Utc).AddTicks(4770));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 101,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 3, 20, 355, DateTimeKind.Utc).AddTicks(4771));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 102,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 3, 20, 355, DateTimeKind.Utc).AddTicks(4772));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 103,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 3, 20, 355, DateTimeKind.Utc).AddTicks(4774));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 104,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 3, 20, 355, DateTimeKind.Utc).AddTicks(4774));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 105,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 3, 20, 355, DateTimeKind.Utc).AddTicks(4775));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "ConfigId",
                keyValue: 106,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 20, 5, 3, 20, 355, DateTimeKind.Utc).AddTicks(4776));
        }
    }
}
