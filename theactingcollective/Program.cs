using theactingcollective.Components;
using theactingcollective.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Temporary diagnostic deployment: leave all routes public.
// Microsoft Identity registration and staff authorization policy are intentionally disabled.

// Add services to the container.

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<IRegistrationStore, AzureTableRegistrationStore>();
builder.Services.AddSingleton<IEmailSender, AcsEmailSender>();
builder.Services.AddSingleton<IEventScheduleService, EventScheduleService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

