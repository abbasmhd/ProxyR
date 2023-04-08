using FluentMigrator;

namespace ProxyR.Database.Migrations.Dbo.Tables
{
    [Migration(2023_04_01_07_18_15)]
    public class AddUserRoleTable : Migration
    {
        private const string SchemaName = "dbo";
        private const string TableName = "UserRole";

        public override void Up()
        {
            Execute.Script(Directory.GetCurrentDirectory() + "\\Scripts\\Dbo\\Tables\\UserRole.sql");
        }

        public override void Down()
        {
            Delete.Table(TableName)
                  .InSchema(SchemaName);
        }
    }
}
