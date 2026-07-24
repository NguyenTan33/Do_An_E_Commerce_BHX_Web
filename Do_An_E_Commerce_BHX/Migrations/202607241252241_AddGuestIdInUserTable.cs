namespace Do_An_E_Commerce_BHX.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGuestIdInUserTable : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Carts", "GuestId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Carts", "GuestId");
        }
    }
}
