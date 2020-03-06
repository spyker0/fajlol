using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TCC.Models;

namespace TCC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(string mensagem = null)
        => View(new JogadorViewModel(mensagem));
    }
}
