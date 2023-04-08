using FluentMigrator;

namespace ProxyR.Database.Migrations.Dbo.Tables
{
    [Migration(2023_04_01_07_18_12)]
    public class AddLogTable : Migration
    {
        public override void Up()
        {
            Create.Table("Log")
                  .InSchema("dbo")
                  .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                  .WithColumn("Text").AsString();
        }

        public override void Down()
        {
            Delete.Table("Log")
                  .InSchema("dbo");
        }
    }
}
