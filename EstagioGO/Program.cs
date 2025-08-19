using EstagioGO.Data;
using EstagioGO.Models.Identity;
using Microsoft.AspNetCore.Identity;
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
builder.Services.AddRazorPages(); // 👈 ESSA LINHA FOI ADICIONADA (CORREÇÃO)

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Agora funcionará porque os serviços foram registrados

app.Run();