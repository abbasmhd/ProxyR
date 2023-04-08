using FluentMigrator;

namespace ProxyR.Database.Migrations.Dbo.Tables
{
    [Migration(2023_04_01_07_18_14)]
    public class AddUserTable : Migration
    {
        private const string SchemaName = "dbo";
        private const string TableName = "User";

        public override void Up()
        {
            Execute.Script(Directory.GetCurrentDirectory() + "\\Scripts\\Dbo\\Tables\\User.sql");
        }

        public override void Down()
        {
            Delete.Table(TableName)
                  .InSchema(SchemaName);
        }
    }
}
