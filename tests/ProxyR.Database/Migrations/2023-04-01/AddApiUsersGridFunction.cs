using FluentMigrator;

namespace ProxyR.Database.Migrations.ProxyR.Functions.Api_Users_Grid
{
    [Migration(2023_04_01_07_18_18)]
    public class AddApiUsersGridFunction : Migration
    {
        public override void Up()
        {
            Execute.Script(Directory.GetCurrentDirectory() + "\\Scripts\\ProxyR\\Functions\\Api_Users_Grid.sql");
        }

        public override void Down()
        {
            Execute.Sql("DROP FUNCTION [ProxyR].[Api_Users_Grid]");
        }
    }
}
