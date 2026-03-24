using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparationService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Feedbacks]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Feedbacks] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Feedbacks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Feedbacks_Accounts_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Accounts] ([Id]) ON DELETE CASCADE
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Feedbacks_CustomerId'
      AND object_id = OBJECT_ID(N'[dbo].[Feedbacks]')
)
BEGIN
    CREATE INDEX [IX_Feedbacks_CustomerId] ON [dbo].[Feedbacks] ([CustomerId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Feedbacks]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Feedbacks];
END
");
        }
    }
}
