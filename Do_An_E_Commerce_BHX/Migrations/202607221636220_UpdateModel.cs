namespace Do_An_E_Commerce_BHX.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "Cart_Id", c => c.Int());
            AddColumn("dbo.Promotions", "percentDiscount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Products", "Price", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Promotions", "DiscountValue", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            CreateIndex("dbo.AspNetUsers", "Cart_Id");
            AddForeignKey("dbo.AspNetUsers", "Cart_Id", "dbo.Carts", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUsers", "Cart_Id", "dbo.Carts");
            DropIndex("dbo.AspNetUsers", new[] { "Cart_Id" });
            AlterColumn("dbo.Promotions", "DiscountValue", c => c.Double(nullable: false));
            AlterColumn("dbo.Products", "Price", c => c.Double(nullable: false));
            DropColumn("dbo.Promotions", "percentDiscount");
            DropColumn("dbo.AspNetUsers", "Cart_Id");
        }
    }
}
