using FluentMigrator;

namespace ProxyR.Database.Migrations._2023_04_01
{
    [Migration(2023_04_01_07_18_10)]
    public class AddPermissions : Migration
    {
        public override void Up()
        {
            Execute.Script(Directory.GetCurrentDirectory() + "\\Scripts\\Security\\ProxyR.sql");
            Execute.Script(Directory.GetCurrentDirectory() + "\\Scripts\\Security\\Permissions.sql");
        }

        public override void Down()
        {
        }
    }
}
