using FluentMigrator;

namespace ProxyR.Database.Migrations.Dbo.Tables
{
    [Migration(2023_04_01_07_18_25)]
    public class AddTableData : Migration
    {

        public override void Up()
        {
            Execute.Script(Directory.GetCurrentDirectory() + "\\Scripts\\Data\\RolesData.sql");
            Execute.Script(Directory.GetCurrentDirectory() + "\\Scripts\\Data\\UsersData.sql");
            Execute.Script(Directory.GetCurrentDirectory() + "\\Scripts\\Data\\UserRoleData.sql");
        }

        public override void Down()
        {
        }
    }
}
