using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrescriptionRefill.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Bilingual reference data (spec 005 FR-019, FR-020, FR-024; research R7). Existing names and
    /// descriptions become the English values, with data and keys kept by renaming. Arabic values
    /// for the seeded rows are literals in this file, so the migration never changes after it is
    /// committed; it does not read <c>SeedNames</c>. Any other row gets a copy of its English value,
    /// so the migration never fails. Later wording fixes ship as a new migration or a documented
    /// reseed, never as an edit here.
    /// </summary>
    public partial class BilingualReferenceData : Migration
    {
        private static readonly (string En, string Ar)[] SeededMedicationNames =
        [
            ("Avelorin", "أفيلورين"),
            ("Brimexa", "بريميكسا"),
            ("Corvantil", "كورفانتيل"),
            ("Dexolune", "ديكسولون"),
            ("Elvadine", "إلفادين"),
            ("Fenquira", "فينكيرا"),
            ("Glimora", "غليمورا"),
            ("Halvexin", "هالفيكسين"),
            ("Jurvane", "جورفان"),
            ("Kestrolin", "كيسترولين"),
            ("Lumavex", "لومافيكس"),
        ];

        private const string SeededDescriptionEn = "Synthetic demo interaction (fictional, not clinical guidance).";
        private const string SeededDescriptionAr = "تداخل تجريبي مُصطنع (خيالي، وليس إرشادًا سريريًا).";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1–2. Keep the existing values as English.
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Medications",
                newName: "NameEn");

            migrationBuilder.RenameIndex(
                name: "IX_Medications_Name",
                table: "Medications",
                newName: "IX_Medications_NameEn");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "DrugInteractions",
                newName: "DescriptionEn");

            // 3. Add the Arabic columns as nullable and fill them.
            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Medications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "DrugInteractions",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            foreach (var (en, ar) in SeededMedicationNames)
            {
                migrationBuilder.Sql($"UPDATE [Medications] SET [NameAr] = {Literal(ar)} WHERE [NameEn] = {Literal(en)};");
            }

            migrationBuilder.Sql(
                $"UPDATE [DrugInteractions] SET [DescriptionAr] = {Literal(SeededDescriptionAr)} WHERE [DescriptionEn] = {Literal(SeededDescriptionEn)};");
            migrationBuilder.Sql("UPDATE [Medications] SET [NameAr] = [NameEn] WHERE [NameAr] IS NULL;");
            migrationBuilder.Sql("UPDATE [DrugInteractions] SET [DescriptionAr] = [DescriptionEn] WHERE [DescriptionAr] IS NULL;");

            // 4. Required, then unique per language.
            migrationBuilder.AlterColumn<string>(
                name: "NameAr",
                table: "Medications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DescriptionAr",
                table: "DrugInteractions",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medications_NameAr",
                table: "Medications",
                column: "NameAr",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Medications_NameAr",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "DrugInteractions");

            migrationBuilder.RenameColumn(
                name: "NameEn",
                table: "Medications",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_Medications_NameEn",
                table: "Medications",
                newName: "IX_Medications_Name");

            migrationBuilder.RenameColumn(
                name: "DescriptionEn",
                table: "DrugInteractions",
                newName: "Description");
        }

        /// <summary>An N'…' literal with embedded quotes doubled; the values above are constants.</summary>
        private static string Literal(string value) => "N'" + value.Replace("'", "''") + "'";
    }
}
