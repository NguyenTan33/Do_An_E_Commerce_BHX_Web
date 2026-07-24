namespace Do_An_E_Commerce_BHX.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateProduct : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Products", "URLImage", c => c.String(maxLength: 250));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Products", "URLImage", c => c.String(maxLength: 50));
        }
    }
}
