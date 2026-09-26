using HomeLibrary.Application;
using HomeLibrary.Infrastructure;
using HomeLibrary.Infrastructure.Persistence.Migrations;
using HomeLibrary.Web.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages()
    .AddMvcOptions(options => options.Filters.Add<NotFoundExceptionPageFilter>());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/status/{0}");
app.UseHttpsRedirection();
app.UseRouting();

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

await app.Services
    .GetRequiredService<IDatabaseMigrator>()
    .Migrate(app.Lifetime.ApplicationStopping);

await app.RunAsync();
