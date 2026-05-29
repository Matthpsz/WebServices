using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWorkflow.Dominio.Enums;
using SistemaWorkflow.Infraestrutura.BancoDados;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaWorkflow.Blazor.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class PainelGeralController : Controller
    {
        private readonly ContextoWorkflow _contexto;

        public PainelGeralController(ContextoWorkflow contexto)
        {
            _contexto = contexto;
        }

        public async Task<IActionResult> Index()
        {
            // Métricas Consolidadas
            ViewBag.InstanciasAtivas = await _contexto.InstanciasProcesso
                .CountAsync(i => i.Status == StatusInstanciaProcesso.Ativa && i.Ativo);

            ViewBag.TarefasPendentes = await _contexto.TarefasProcesso
                .CountAsync(t => t.Status == StatusTarefaProcesso.Pendente && t.Ativo);

            ViewBag.ConcluidosMes = await _contexto.InstanciasProcesso
                .CountAsync(i => i.Status == StatusInstanciaProcesso.Concluida && i.Ativo);

            ViewBag.MediaSla = "94.8%"; // Simulação conforme KPI anterior

            // Logs de Auditoria Recentes
            var logs = await _contexto.LogsAuditoria
                .OrderByDescending(l => l.DataCriacao)
                .Take(8)
                .ToListAsync();

            return View(logs);
        }
    }
}
