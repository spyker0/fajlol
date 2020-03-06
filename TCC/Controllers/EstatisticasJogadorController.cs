using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RiotSharp;
using RiotSharp.Misc;
using System;
using System.Threading.Tasks;
using TCC.Models;
using TCC.RegraNegocio;

namespace TCC.Controllers
{
    [Route("[controller]")]
    public class EstatisticasJogadorController : Controller
    {
        private readonly ILogger<EstatisticasJogadorController> _logger;
        private readonly JogadorRegraNegocio _jogadorRegraNegocio;

        public EstatisticasJogadorController(ILogger<EstatisticasJogadorController> logger, JogadorRegraNegocio jogadorRegraNegocio)
        {
            _logger = logger;
            _jogadorRegraNegocio = jogadorRegraNegocio;
        }

        // GET: EstatisticasJogadors
        [Route("{regiao}/{jogador}")]
        public async Task<IActionResult> Index(int regiao, string jogador)
        {
            try
            {
                var estatisticasJogador = await _jogadorRegraNegocio.PersistirDadosJogadorAsync(regiao, jogador);

                if (estatisticasJogador == null)
                {
                    return RedirectToAction("Index", "Home", new { mensagem = $"Não foi possível encontrar o jogador {jogador} na região {regiao}" });
                }

                var retorno = new EstatisticasJogadorViewModel { EstatisticasJogador = estatisticasJogador, LinkIcone = await _jogadorRegraNegocio.RetornarUrlIconeJogadorAsync(estatisticasJogador.IconeId) };

                retorno.Partidas = await _jogadorRegraNegocio.RetornarPartidasJogador(estatisticasJogador.ContaId, estatisticasJogador.Regiao);

                return View(retorno);
            }
            catch (RiotSharpException ex)
            {
                if (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex, "Algo não encontrado");
                }
                else
                    _logger.LogError(ex, "Ocorreu um erro");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro");
            }
            return View(null);
        }

        [Route("Detalhes/{regiao}/{gameId}/{idJogadorPrincipal}")]
        public async Task<PartidaViewModel> ObterPartidaDetalhada(Region regiao, long gameId, string idJogadorPrincipal)
            => await _jogadorRegraNegocio.RetornarPartidaDetalhada(regiao, gameId, idJogadorPrincipal);
    }
}
