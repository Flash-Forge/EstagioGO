using EstagioGO.Constants;
using EstagioGO.Data;
using EstagioGO.Filters;
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
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender, EmailSender>();

// Políticas de autorização
builder.Services.AddAuthorizationBuilder()
                               // Políticas de autorização
                               .AddPolicy("PodeGerenciarEstagiarios", policy =>
        policy.RequireRole("Administrador", "Coordenador"))
                               // Políticas de autorização
                               .AddPolicy("PodeRegistrarFrequencia", policy =>
        policy.RequireRole("Administrador", "Coordenador", "Supervisor"))
                               // Políticas de autorização
                               .AddPolicy("PodeAvaliarEstagiarios", policy =>
        policy.RequireRole("Administrador", "Coordenador", "Supervisor"))
                               // Políticas de autorização
                               .AddPolicy("PodeVerRelatorios", policy =>
        policy.RequireRole("Administrador", "Coordenador"));

// Adicione o filtro de primeiro acesso
builder.Services.AddScoped<FirstAccessFilter>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<FirstAccessFilter>(); // Adicione o filtro globalmente
});

builder.Services.AddRazorPages();

var app = builder.Build();

// Executar o seed de dados para criar o administrador padrão
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("SeedData");

    try
    {
        logger.LogInformation("Iniciando o seed de dados...");
        await SeedData.InitializeAsync(services);
        logger.LogInformation("Seed de dados concluído com sucesso.");

        // Verificação pós-seed
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync(AppConstants.DefaultAdminEmail);

        if (adminUser != null)
        {
            logger.LogInformation("Administrador padrão encontrado com ID: {UserId}", adminUser.Id);
            logger.LogInformation("Email confirmado: {Confirmed}", adminUser.EmailConfirmed);
            logger.LogInformation("Primeiro acesso concluído: {Completed}", adminUser.PrimeiroAcessoConcluido);

            // Verifique se a senha está correta
            var passwordCheck = await userManager.CheckPasswordAsync(adminUser, "Admin@123");
            logger.LogInformation("Senha padrão funciona: {PasswordWorks}", passwordCheck);
        }
        else
        {
            logger.LogError("ERRO CRÍTICO: Administrador padrão NÃO foi criado!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocorreu um erro durante o seed de dados");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=UserManagement}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "estagiarios",
    pattern: "Estagiarios/{action=Index}/{id?}",
    defaults: new { controller = "Estagiarios" });

app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

// No .NET 8, NÃO use app.UseEndpoints() - isso está obsoleto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Bloquear acesso direto à página de registro
app.MapGet("/Identity/Account/Register", context =>
{
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

app.MapPost("/Identity/Account/Register", context =>
{
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

app.Run();