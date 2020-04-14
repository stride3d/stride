using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Xenko.Metrics.ServerApp.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Xenko.Metrics.ServerApp.Models.MetricDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(Xenko.Metrics.ServerApp.Models.MetricDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //

            if (context.Database.SqlQuery<int>("SELECT COUNT(*) FROM dbo.IpToLocations").First() != IpToLocationHelper.EntryCount)
            {
                context.Database.ExecuteSqlCommand("delete from IpToLocations");

                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = IpToLocationHelper.ResourceName;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    var values = new List<string>();
                    while (true)
                    {
                        var value = reader.ReadLine();

                        if (value != null)
                            values.Add(value.Replace("\'", "\'\'").Replace("\"", "\'"));

                        if (values.Count >= 1000 || (values.Count > 0 && value == null))
                        {
                            // https://stackoverflow.com/a/17208015
                            // input.Substring(1, input.Length - 2) removes the first and last " from the string
                            //var tokens = System.Text.RegularExpressions.Regex.Split(input.Substring(1, input.Length - 2), @"""\s*,\s*""");
                            //
                            //context.IpToLocations.Add(new Models.IpToLocation { IpFrom = long.Parse(tokens[0]), IpTo = long.Parse(tokens[1]), CountryCode = tokens[2], CountryName = tokens[3] });
                            context.Database.ExecuteSqlCommand($@"INSERT INTO [dbo].[IpToLocations]
           ([IpFrom]
           ,[IpTo]
           ,[CountryCode]
           ,[CountryName])
     VALUES({string.Join("),(", values)})");

                            values.Clear();
                        }

                        if (value == null)
                            break;
                    }
                }
            }
        }
    }
}
