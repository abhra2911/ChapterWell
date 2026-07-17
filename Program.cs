using Lib_Mgmt.Data;
using Lib_Mgmt.Models;
using Microsoft.EntityFrameworkCore;
using Lib_Mgmt.Reporting.Exporters;


var builder = WebApplication.CreateBuilder(args);

// Session needs a backing store; in-memory is fine for a single-node app.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// SQL Server via the official EF Core provider. Connection string lives in
// appsettings.json (Windows Auth / trusted connection, so no secrets there).
builder.Services.AddDbContext<ModelContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LibraryDb")));

// One repository instance per request.
builder.Services.AddScoped<LibraryRepository>();
builder.Services.AddScoped<IReportExporter, CsvReportExporter>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();


// Must come before MapControllerRoute so controllers can read HttpContext.Session.
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
