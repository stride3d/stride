using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Stride.Metrics.ServerApp.Data;

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


var app = builder.Build();

// Apply migrations and seed data
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    app.UseSwagger();
    app.UseSwaggerUI();
    //MetricDbContext.Initialize(scope.ServiceProvider);
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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
