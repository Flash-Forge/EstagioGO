using EstagioGO.Data;
using EstagioGO.Middleware;
using EstagioGO.Models.Identity;
using EstagioGO.Services;
using EstagioGO.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configuração completa do Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Configurações de senha
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;

    // Configurações de bloqueio
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // Configurações de usuário
    options.User.RequireUniqueEmail = true;

    // Configurações de login
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender, EmailSender>();

// Políticas de autorização
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PodeGerenciarEstagiarios", policy =>
        policy.RequireRole("Administrador", "Coordenador"));

    options.AddPolicy("PodeRegistrarFrequencia", policy =>
        policy.RequireRole("Administrador", "Coordenador", "Supervisor"));

    options.AddPolicy("PodeAvaliarEstagiarios", policy =>
        policy.RequireRole("Administrador", "Coordenador", "Supervisor"));

    options.AddPolicy("PodeVerRelatorios", policy =>
        policy.RequireRole("Administrador", "Coordenador"));
});

// Adicione o serviço de claims personalizado
builder.Services.AddScoped<UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>, CustomClaimsPrincipalFactory>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Executar o seed de dados para criar o administrador padrão
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro durante o seed de dados");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // O valor HSTS padrão é de 30 dias. Você pode querer alterar isso para cenários de produção.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Middleware para forçar alteração de senha (substitui o FirstAccessMiddleware)
app.UseForcePasswordChangeMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();

    // Bloquear acesso direto à página de registro
    endpoints.MapGet("/Identity/Account/Register", context =>
    {
        context.Response.Redirect("/Identity/Account/Login");
        return Task.CompletedTask;
    });

    endpoints.MapPost("/Identity/Account/Register", context =>
    {
        context.Response.Redirect("/Identity/Account/Login");
        return Task.CompletedTask;
    });
});

app.Run();