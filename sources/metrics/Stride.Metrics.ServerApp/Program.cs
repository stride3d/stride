using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Stride.Metrics.ServerApp.Data;
using Stride.Metrics.ServerApp.Extensions;
using Stride.Metrics.ServerApp.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add SQL Server database 
builder.Services.AddDbContext<MetricDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    options => options.CommandTimeout(999999999));
});
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    c.IncludeXmlComments(xmlPath);
});
builder.Services.AddDatabaseSeeder();

var app = builder.Build();

// Apply migrations and seed data
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    if(EnvironmentHelpers.IsSeedingEnabled())
        app.UseDatabaseSeeder();
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
