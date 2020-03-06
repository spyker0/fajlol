using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RiotSharp.Misc;
using System;
using System.Linq;
using System.Threading.Tasks;
using TCC.RegraNegocio;

namespace TCC.Controllers
{
    [Route("[controller]")]
    public class PartidaAtualController : Controller
    {
        private readonly ILogger<PartidaAtualController> _logger;
        private readonly JogadorRegraNegocio _jogadorRegraNegocio;

        public PartidaAtualController(ILogger<PartidaAtualController> logger, JogadorRegraNegocio jogadorRegraNegocio)
        {
            _logger = logger;
            _jogadorRegraNegocio = jogadorRegraNegocio;
        }

        // GET: EstatisticasJogadors
        [Route("{regiao}/{jogador}")]
        public async Task<IActionResult> Index(int regiao, string jogador)
        {
            var retorno = await _jogadorRegraNegocio.PersistirDadosPartidaAtualAsync(regiao, jogador);
            return View(retorno);
        }

        // GET: PartidaAleatoria
        [Route("PartidaAleatoria")]
        public async Task<IActionResult> PartidaAleatoria()
        {
            var partidaAleatoria = await _jogadorRegraNegocio.RetornarPartidaAleatoriaAsync();

            var regiao = (int)Region.Br;
            var jogador = partidaAleatoria.Participants.First().SummonerName;

            var retorno = await _jogadorRegraNegocio.PersistirDadosPartidaAtualAsync(regiao, jogador);

            return View("Index", retorno);
        }


        // GET: RetornarVariasPartidas
        [Route("PreverPartidas")]
        public async Task<IActionResult> PreverPartidas()
        {
            while (true)
            {

                try
                {
                    _logger.LogInformation("Buscando partida aleatória");
                    //gerando dados de teste
                    var partidaAleatoria = await _jogadorRegraNegocio.RetornarPartidaAleatoriaAsync();

                    var regiao = (int)Region.Br;
                    var jogador = partidaAleatoria.Participants.First().SummonerName;

                    //Final dados de teste

                    var retorno = await _jogadorRegraNegocio.PersistirDadosPartidaAtualAsync(regiao, jogador);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao obter partida atual");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(120));
                }
            }
        }

        // GET: RetornarVariasPartidas
        [Route("GravarResultadoPrevisaoPartidas")]
        public async Task<IActionResult> GravarResultadoPrevisaoPartidas()
        {
            while (true)
            {
                try
                {
                    var todasGravadas = await _jogadorRegraNegocio.GravarResultadoPrevisaoPartidasAsync();
                    if (todasGravadas)
                        break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao gravar resultado das partidas");
                }
                await Task.Delay(TimeSpan.FromSeconds(120));
            }
            return Content("");
        }
    }
}
