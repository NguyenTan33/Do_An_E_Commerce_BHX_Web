namespace Do_An_E_Commerce_BHX.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateModel1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "FullName", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "FullName");
        }
    }
}
