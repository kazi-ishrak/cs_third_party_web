using cs_third_party_web;
using cs_third_party_web.Data;
using cs_third_party_web.Handler;
using cs_third_party_web.Job;
using cs_third_party_web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
IConfiguration configuration = builder.Configuration;
builder.Services.AddHostedService<Worker>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddWindowsService();
builder.Services.AddRazorPages(); //add razor pages
builder.Services.AddScoped<DbHandler>();
builder.Services.AddSingleton<AttendanceLogPullJob>();
builder.Services.AddSingleton<AttendanceLogPushJob>();
builder.Services.AddSingleton<EmployeeListPullJob>();
builder.Services.AddSingleton<ApiHandler>();
builder.Services.AddSingleton<PrismService>();
builder.Services.AddSingleton<PihrService>();
builder.Services.AddSingleton<ZohoService>();
builder.Services.AddSingleton<CsService>();
builder.Services.AddSingleton<CommonService>();
builder.Services.AddSingleton<ApiHandler>();
builder.Services.AddSingleton<ApiControllerHelper>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Central Server API", Version = "v1" });
});

#region DB Connection
//Local
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (configuration.GetSection("Database").Get<string>() == "mysql")
    {
        options.UseMySql(configuration.GetConnectionString("mysqlConnection"), new MySqlServerVersion(new Version(8, 0, 26)));
    }
    else if (configuration.GetSection("Database").Get<string>() == "mssql")
    {
        options.UseSqlServer(configuration.GetConnectionString("mssqlConnection"), sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        });
    }
}, ServiceLifetime.Scoped);

//HRM
//services.AddDbContext<HrmDbContext>(options =>
//{
//    options.UseMySql(configuration.GetConnectionString("hrmConnection"), new MySqlServerVersion(new Version(8, 0, 26)));
//}, ServiceLifetime.Scoped);

//CS
//services.AddDbContext<CsDbContext>(options =>
//{
//    options.UseMySql(configuration.GetConnectionString("csConnection"), new MySqlServerVersion(new Version(8, 0, 26)));
//}, ServiceLifetime.Scoped);
#endregion

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inovace Central Server");
    });
}

app.UseRouting();
app.MapRazorPages();
app.UseStaticFiles();
app.MapControllers();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

//Loads the AttendanceLogPage By default
app.MapGet("/", context =>
{
    context.Response.Redirect("/WelcomePage");
    return Task.CompletedTask;
});


await app.RunAsync();
