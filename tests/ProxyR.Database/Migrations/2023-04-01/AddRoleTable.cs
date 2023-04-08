using FluentMigrator;

namespace ProxyR.Database.Migrations.Dbo.Tables
{
    [Migration(2023_04_01_07_18_13)]
    public class AddRoleTable : Migration
    {
        private const string SchemaName = "dbo";
        private const string TableName = "Role";

        public override void Up()
        {
            Execute.Script(Directory.GetCurrentDirectory() + "\\Scripts\\Dbo\\Tables\\Role.sql");
        }

        public override void Down()
        {
            Delete.Table(TableName)
                  .InSchema(SchemaName);
        }
    }
}
