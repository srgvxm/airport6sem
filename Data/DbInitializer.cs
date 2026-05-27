using Airport.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
namespace Airport.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();
            EnsureTicketExtraServiceColumns(context);
            EnsureAuditLogTable(context);
        }

        private static void EnsureTicketExtraServiceColumns(ApplicationDbContext context)
        {
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Ticket' AND COLUMN_NAME = 'HasBaggage'
                )
                BEGIN
                    ALTER TABLE [Ticket] ADD [HasBaggage]   BIT NOT NULL DEFAULT 0;
                    ALTER TABLE [Ticket] ADD [HasMeal]      BIT NOT NULL DEFAULT 0;
                    ALTER TABLE [Ticket] ADD [HasInsurance] BIT NOT NULL DEFAULT 0;
                END
            ");
        }

        private static void EnsureAuditLogTable(ApplicationDbContext context)
        {
            // Если таблица есть, но в старой схеме (нет поля ActionDescription) — дропаем и пересоздаём
            context.Database.ExecuteSqlRaw(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLog'
                ) AND NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'AuditLog' AND COLUMN_NAME = 'ActionDescription'
                )
                BEGIN
                    DROP TABLE [AuditLog];
                END
            ");

            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLog')
                BEGIN
                    CREATE TABLE [AuditLog] (
                        [Id]                INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
                        [UserId]            INT           NULL,
                        [Username]          NVARCHAR(50)  NOT NULL DEFAULT '',
                        [UserRole]          NVARCHAR(20)  NOT NULL DEFAULT '',
                        [ActionDescription] NVARCHAR(200) NOT NULL DEFAULT '',
                        [Controller]        NVARCHAR(100) NOT NULL DEFAULT '',
                        [Action]            NVARCHAR(100) NOT NULL DEFAULT '',
                        [IpAddress]         NVARCHAR(50)  NOT NULL DEFAULT '',
                        [StatusCode]        INT           NOT NULL DEFAULT 0,
                        [Timestamp]         DATETIME2     NOT NULL DEFAULT GETUTCDATE()
                    );
                    CREATE INDEX [IX_AuditLog_Timestamp] ON [AuditLog] ([Timestamp]);
                    CREATE INDEX [IX_AuditLog_UserId]    ON [AuditLog] ([UserId]);
                END
            ");
        }

    }
}
