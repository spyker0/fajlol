using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TCC.RegraNegocio;

namespace TCC.Controllers
{
    [Route("[controller]")]
    public class ItensCampeaoController : Controller
    {
        private readonly ItensRegraNegocio _itensRegraNegocio;
        private readonly ILogger<ItensCampeaoController> _logger;

        public ItensCampeaoController(ItensRegraNegocio itensRegraNegocio, ILogger<ItensCampeaoController> logger)
        {
            _itensRegraNegocio = itensRegraNegocio;
            _logger = logger;
        }

        [Route("CalcularItens")]
        public async Task<IActionResult> CalcularItens()
        {
            try
            {
                await _itensRegraNegocio.CalculaItensCampeoes();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular itens dos campeões");
                return new StatusCodeResult(500);
            }
        }
    }
}