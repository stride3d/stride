namespace Stride.Metrics.ServerApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialVersion : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MetricApps",
                c => new
                    {
                        AppId = c.Int(nullable: false, identity: true),
                        AppGuid = c.Guid(nullable: false),
                        AppName = c.String(nullable: false, maxLength: 128),
                        Created = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.AppId)
                .Index(t => t.AppGuid, unique: true)
                .Index(t => t.AppName, unique: true);
            
            CreateTable(
                "dbo.MetricInstalls",
                c => new
                    {
                        InstallId = c.Int(nullable: false, identity: true),
                        InstallGuid = c.Guid(nullable: false),
                        Created = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.InstallId)
                .Index(t => t.InstallGuid, unique: true);
            
            CreateTable(
                "dbo.MetricMarkerGroups",
                c => new
                    {
                        MarkerGroupId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Description = c.String(),
                        Created = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.MarkerGroupId);
            
            CreateTable(
                "dbo.MetricMarkers",
                c => new
                    {
                        MarkerId = c.Int(nullable: false, identity: true),
                        MarkerGroupId = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 100),
                        Date = c.DateTime(nullable: false),
                        Created = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.MarkerId)
                .ForeignKey("dbo.MetricMarkerGroups", t => t.MarkerGroupId, cascadeDelete: true)
                .Index(t => t.MarkerGroupId);
            
            CreateTable(
                "dbo.MetricEventDefinitions",
                c => new
                    {
                        MetricId = c.Int(nullable: false, identity: true),
                        MetricGuid = c.Guid(nullable: false),
                        MetricName = c.String(nullable: false, maxLength: 128),
                        Description = c.String(maxLength: 512),
                        Created = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.MetricId)
                .Index(t => t.MetricGuid, unique: true)
                .Index(t => t.MetricName, unique: true);
            
            CreateTable(
                "dbo.MetricEvents",
                c => new
                    {
                        Timestamp = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        AppId = c.Int(nullable: false),
                        InstallId = c.Int(nullable: false),
                        SessionId = c.Int(nullable: false),
                        MetricId = c.Int(nullable: false),
                        IPAddress = c.String(maxLength: 20),
                        MetricValue = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => new { t.Timestamp, t.AppId, t.InstallId, t.SessionId, t.MetricId })
                .ForeignKey("dbo.MetricApps", t => t.AppId, cascadeDelete: true)
                .ForeignKey("dbo.MetricInstalls", t => t.InstallId, cascadeDelete: true)
                .ForeignKey("dbo.MetricEventDefinitions", t => t.MetricId, cascadeDelete: true)
                .Index(t => t.AppId)
                .Index(t => t.InstallId)
                .Index(t => t.MetricId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MetricEvents", "MetricId", "dbo.MetricEventDefinitions");
            DropForeignKey("dbo.MetricEvents", "InstallId", "dbo.MetricInstalls");
            DropForeignKey("dbo.MetricEvents", "AppId", "dbo.MetricApps");
            DropForeignKey("dbo.MetricMarkers", "MarkerGroupId", "dbo.MetricMarkerGroups");
            DropIndex("dbo.MetricEvents", new[] { "MetricId" });
            DropIndex("dbo.MetricEvents", new[] { "InstallId" });
            DropIndex("dbo.MetricEvents", new[] { "AppId" });
            DropIndex("dbo.MetricEventDefinitions", new[] { "MetricName" });
            DropIndex("dbo.MetricEventDefinitions", new[] { "MetricGuid" });
            DropIndex("dbo.MetricMarkers", new[] { "MarkerGroupId" });
            DropIndex("dbo.MetricInstalls", new[] { "InstallGuid" });
            DropIndex("dbo.MetricApps", new[] { "AppName" });
            DropIndex("dbo.MetricApps", new[] { "AppGuid" });
            DropTable("dbo.MetricEvents");
            DropTable("dbo.MetricEventDefinitions");
            DropTable("dbo.MetricMarkers");
            DropTable("dbo.MetricMarkerGroups");
            DropTable("dbo.MetricInstalls");
            DropTable("dbo.MetricApps");
        }
    }
}
