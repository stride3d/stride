namespace Stride.Metrics.ServerApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEventIdMigration : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MetricEvents", "EventId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.MetricEvents", "EventId");
        }
    }
}
