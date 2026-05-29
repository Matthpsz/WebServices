using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaWorkflow.Aplicacao.Interfaces;
using SistemaWorkflow.Infraestrutura.BancoDados;
using SistemaWorkflow.Infraestrutura.Seguranca;
using SistemaWorkflow.Infraestrutura.Servicos;

var construtor = WebApplication.CreateBuilder(args);

// 1. Configuração do Banco de Dados (EF Core + SQL Server LocalDB)
construtor.Services.AddDbContext<ContextoWorkflow>(opcoes =>
    opcoes.UseSqlServer(construtor.Configuration.GetConnectionString("ConexaoPadrao"),
        b => b.MigrationsAssembly("SistemaWorkflow.Infraestrutura")));

// 2. Registro de Serviços de Negócio/Segurança
construtor.Services.AddScoped<IServicoCriptografia, ServicoCriptografia>();
construtor.Services.AddScoped<IServicoTokenJwt, ServicoTokenJwt>();
construtor.Services.AddScoped<IServicoFormulario, ServicoFormulario>();
construtor.Services.AddScoped<IServicoProcesso, ServicoProcesso>();
construtor.Services.AddScoped<IMotorWorkflow, MotorWorkflow>();

// 3. Configuração dos Controladores
construtor.Services.AddControllers();

// 4. Configuração de Autenticação JWT
var chaveSecreta = construtor.Configuration["ConfiguracoesJwt:ChaveSecreta"]
    ?? throw new InvalidOperationException("Chave secreta do JWT não configurada no appsettings.json.");
var emissor = construtor.Configuration["ConfiguracoesJwt:Emissor"];
var audiencia = construtor.Configuration["ConfiguracoesJwt:Audiencia"];

construtor.Services.AddAuthentication(opcoes =>
{
    opcoes.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opcoes.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opcoes =>
{
    opcoes.RequireHttpsMetadata = false;
    opcoes.SaveToken = true;
    opcoes.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveSecreta)),
        ValidateIssuer = !string.IsNullOrWhiteSpace(emissor),
        ValidIssuer = emissor,
        ValidateAudience = !string.IsNullOrWhiteSpace(audiencia),
        ValidAudience = audiencia,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 5. Configuração do Swagger com suporte a JWT
construtor.Services.AddEndpointsApiExplorer();
construtor.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Sistema de Workflow e BPM Corporativo - API", 
        Version = "v1",
        Description = "API do sistema corporativo para gerenciamento dinâmico de processos e formulários."
    });

    opcoes.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticação baseada em token JWT. Exemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    opcoes.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = construtor.Build();

// 6. Execução Automática de Migrações e Semeador de Banco de Dados no Startup
using (var escopo = app.Services.CreateScope())
{
    var provedorServicos = escopo.ServiceProvider;
    try
    {
        var contexto = provedorServicos.GetRequiredService<ContextoWorkflow>();
        var servicoCriptografia = provedorServicos.GetRequiredService<IServicoCriptografia>();
        
        // Aplica automaticamente quaisquer migrações pendentes no SQL Server
        contexto.Database.Migrate();
        
        // Insere as sementes de perfis, permissões e o administrador inicial
        SemeadorBancoDados.Semear(contexto, servicoCriptografia);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Erro Grave] Falha ao inicializar/migrar banco de dados: {ex.Message}");
    }
}

// 7. Pipeline de Requisições HTTP (Middlewares)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
