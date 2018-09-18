namespace Xenko.Metrics.ServerApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IpToLocation : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.IpToLocations",
                c => new
                    {
                        IpFrom = c.Long(nullable: false),
                        IpTo = c.Long(nullable: false),
                        CountryCode = c.String(nullable: false, maxLength: 2),
                        CountryName = c.String(nullable: false, maxLength: 64),
                    })
                .PrimaryKey(t => t.IpFrom)
                .Index(t => t.IpFrom, unique: true)
                .Index(t => t.IpTo, unique: true);


            Sql(@"CREATE FUNCTION [dbo].[IPAddressToInteger] (@IP AS varchar(15))
RETURNS bigint
AS
BEGIN
RETURN (CONVERT(bigint, PARSENAME(@IP,1)) +
CONVERT(bigint, PARSENAME(@IP,2)) * 256 +
CONVERT(bigint, PARSENAME(@IP,3)) * 65536 +
CONVERT(bigint, PARSENAME(@IP,4)) * 16777216)
END");
            Sql(@"CREATE FUNCTION [dbo].[IPAddressToCountry] (@IP AS varchar(15))
RETURNS nvarchar(64)
AS
BEGIN
RETURN (SELECT TOP 1 CountryName FROM IpToLocations WHERE dbo.IPAddressToInteger(@IP) BETWEEN IpToLocations.IpFrom AND IpToLocations.IpTo ORDER BY IpToLocations.IpFrom DESC)
END");
        }
        
        public override void Down()
        {
            Sql("DROP FUNCTION [dbo].[IPAddressToCountry]");
            Sql("DROP FUNCTION [dbo].[IPAddressToInteger]");
            DropIndex("dbo.IpToLocations", new[] { "IpFrom" });
            DropIndex("dbo.IpToLocations", new[] { "IpTo" });
            DropTable("dbo.IpToLocations");
        }
    }
}
