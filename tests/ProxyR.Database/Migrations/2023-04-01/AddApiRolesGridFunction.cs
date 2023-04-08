using FluentMigrator;

namespace ProxyR.Database.Migrations.ProxyR.Functions.Api_Roles_Grid
{
    [Migration(2023_04_01_07_18_17)]
    public class AddApiRolesGridFunction : Migration
    {
        public override void Up()
        {
            Execute.Script(Directory.GetCurrentDirectory() + "\\Scripts\\ProxyR\\Functions\\Api_Roles_Grid.sql");
        }

        public override void Down()
        {
            Execute.Sql("DROP FUNCTION [ProxyR].[Api_Roles_Grid]");
        }
    }
}
