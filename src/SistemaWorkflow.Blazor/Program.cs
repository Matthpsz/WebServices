using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SistemaWorkflow.Aplicacao.Interfaces;
using SistemaWorkflow.Infraestrutura.BancoDados;
using SistemaWorkflow.Infraestrutura.Seguranca;
using SistemaWorkflow.Infraestrutura.Servicos;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Banco de Dados (EF Core + SQL Server)
builder.Services.AddDbContext<ContextoWorkflow>(opcoes =>
    opcoes.UseSqlServer(builder.Configuration.GetConnectionString("ConexaoPadrao"),
        b => b.MigrationsAssembly("SistemaWorkflow.Infraestrutura")));

// 2. Registro dos Serviços de Domínio/Segurança
builder.Services.AddScoped<IServicoCriptografia, ServicoCriptografia>();
builder.Services.AddScoped<IServicoTokenJwt, ServicoTokenJwt>();
builder.Services.AddScoped<IServicoFormulario, ServicoFormulario>();
builder.Services.AddScoped<IServicoProcesso, ServicoProcesso>();
builder.Services.AddScoped<IMotorWorkflow, MotorWorkflow>();

// 3. Configuração do ASP.NET Core 8 MVC (Controllers + Views)
builder.Services.AddControllersWithViews();

// 3.1. Configuração do Cookie Authentication
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.LoginPath = "/Usuarios/Login";
        opcoes.LogoutPath = "/Usuarios/Logout";
        opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// 3.2. Execução Automática de Migrações e Semeador de Banco de Dados no Startup
using (var escopo = app.Services.CreateScope())
{
    var provedorServicos = escopo.ServiceProvider;
    try
    {
        var contexto = provedorServicos.GetRequiredService<ContextoWorkflow>();
        var servicoCriptografia = provedorServicos.GetRequiredService<IServicoCriptografia>();
        contexto.Database.Migrate();
        SemeadorBancoDados.Semear(contexto, servicoCriptografia);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Erro] Falha ao inicializar/migrar banco de dados: {ex.Message}");
    }
}

// 4. Middleware HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 5. Mapeamento de Rotas MVC Padrão
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PainelGeral}/{action=Index}/{id?}");

app.Run();
