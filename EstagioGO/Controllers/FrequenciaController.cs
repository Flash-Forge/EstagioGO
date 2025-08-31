using EstagioGO.Data;
using EstagioGO.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EstagioGO.Controllers
{
    [Authorize]
    public class FrequenciaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        // GET: Frequencia
        public async Task<IActionResult> Index()
        {
            // Verificar se existe algum estagiário cadastrado
            bool existemEstagiarios = await _context.Estagiarios.AnyAsync();

            if (!existemEstagiarios)
            {
                ViewBag.MensagemSemRecursos = "Não há estagiários cadastrados. Você precisa cadastrar um estagiário primeiro.";
                ViewBag.RedirecionarPara = Url.Action("Create", "Estagiarios");
                return View("SemEstagiarios"); // Usa a nova view específica
            }

            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                // Para estagiários, buscar apenas seus próprios registros
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario != null)
                {
                    ViewBag.EstagiarioId = estagiario.Id;
                    ViewBag.EstagiarioNome = estagiario.Nome;

                    // Carregar as frequências do estagiário
                    var frequencias = await _context.Frequencias
                        .Include(f => f.Estagiario)
                        .Where(f => f.EstagiarioId == estagiario.Id)
                        .OrderByDescending(f => f.Data)
                        .ThenByDescending(f => f.DataRegistro)
                        .Take(10) // Últimos 10 registros
                        .ToListAsync();

                    return View("IndexEstagiario", frequencias);
                }
                else
                {
                    ViewBag.MensagemSemRecursos = "Estagiário não encontrado para este usuário. Contate o administrador.";
                    ViewBag.RedirecionarPara = Url.Action("Index", "Home");
                    return View("SemEstagiarios");
                }
            }
            else
            {
                // Para coordenadores/supervisores, mostrar todos os estagiários
                ViewBag.Estagiarios = await _context.Estagiarios.OrderBy(e => e.Nome).ToListAsync();
                return View();
            }
        }

        // GET: Frequencia/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var frequencia = await _context.Frequencias
                .Include(f => f.Estagiario)
                .Include(f => f.Justificativa)
                .Include(f => f.RegistradoPor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (frequencia == null)
            {
                return NotFound();
            }

            // Verificar se o usuário tem permissão para ver este registro
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || frequencia.EstagiarioId != estagiario.Id)
                {
                    return Forbid();
                }
            }

            return View(frequencia);
        }

        // GET: Frequencia/Create
        public async Task<IActionResult> Create(int? estagiarioId)
        {
            bool existemEstagiarios = await _context.Estagiarios.AnyAsync();

            if (!existemEstagiarios)
            {
                ViewBag.MensagemSemRecursos = "Não há estagiários cadastrados. Você precisa cadastrar um estagiário primeiro.";
                ViewBag.RedirecionarPara = Url.Action("Create", "Estagiarios");
                return View("SemEstagiarios");
            }

            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");
            var isSupervisor = User.IsInRole("Supervisor");
            var isCoordenador = User.IsInRole("Coordenador");
            var isAdministrador = User.IsInRole("Administrador");

            // Se for estagiário, buscar seu próprio ID
            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null)
                {
                    TempData["ErrorMessage"] = "Estagiário não encontrado para este usuário. Contate o administrador.";
                    return RedirectToAction("Index", "Home");
                }

                estagiarioId = estagiario.Id;
                ViewBag.EstagiarioNome = estagiario.Nome;
            }

            // Carregar Estagiários com nome
            if (isEstagiario)
            {
                ViewData["EstagiarioId"] = new SelectList(
                    _context.Estagiarios.Where(e => e.Id == estagiarioId),
                    "Id", "Nome", estagiarioId);
            }
            else
            {
                // Para supervisores, mostrar apenas seus estagiários
                if (isSupervisor)
                {
                    ViewData["EstagiarioId"] = new SelectList(
                        _context.Estagiarios.Where(e => e.SupervisorId == user.Id),
                        "Id", "Nome", estagiarioId);
                }
                else
                {
                    // Para coordenadores e administradores, mostrar todos os estagiários
                    ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "Nome", estagiarioId);
                }
            }

            // Carregar Justificativas apenas para não-estagiários
            if (!isEstagiario)
            {
                ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Descricao");
            }

            // Se for estagiário, definir valores padrão
            if (isEstagiario)
            {
                var model = new Frequencia
                {
                    EstagiarioId = estagiarioId.Value,
                    Data = DateTime.Today,
                    HoraEntrada = DateTime.Now.TimeOfDay,
                    Presente = true,
                    DataRegistro = DateTime.Now,
                    RegistradoPorId = user.Id
                };

                return View(model);
            }

            ViewBag.CurrentUserId = user.Id;

            return View();
        }

        // POST: Frequencia/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,EstagiarioId,Data,HoraEntrada,HoraSaida,Presente,Observacao,JustificativaId")] Frequencia frequencia)
        {
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");
            var isSupervisor = User.IsInRole("Supervisor");
            var isCoordenador = User.IsInRole("Coordenador");
            var isAdministrador = User.IsInRole("Administrador");

            // Remover validação das propriedades de navegação
            ModelState.Remove("Estagiario");
            ModelState.Remove("RegistradoPor");
            ModelState.Remove("Justificativa");
            ModelState.Remove("RegistradoPorId");

            // Definir o RegistradoPorId ANTES de qualquer validação
            frequencia.RegistradoPorId = user.Id;
            frequencia.DataRegistro = DateTime.Now;

            // Validações
            if (frequencia.Data > DateTime.Today)
            {
                ModelState.AddModelError("Data", "A data não pode ser futura.");
            }

            if (frequencia.HoraEntrada.HasValue && frequencia.HoraSaida.HasValue &&
                frequencia.HoraEntrada.Value > frequencia.HoraSaida.Value)
            {
                ModelState.AddModelError("HoraSaida", "A hora de saída não pode ser anterior à hora de entrada.");
            }

            // Para estagiários, forçar presença = true e remover justificativa
            if (isEstagiario)
            {
                frequencia.Presente = true;
                frequencia.JustificativaId = null;

                // Verificar se o estagiário está tentando registrar para si mesmo
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || frequencia.EstagiarioId != estagiario.Id)
                {
                    return Forbid();
                }
            }
            else
            {
                // Para supervisores, coordenadores e administradores, verificar permissões
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.Id == frequencia.EstagiarioId);

                if (estagiario == null)
                {
                    ModelState.AddModelError("EstagiarioId", "Estagiário não encontrado.");
                }
                else if (isSupervisor && estagiario.SupervisorId != user.Id)
                {
                    // Supervisores só podem registrar frequência para seus próprios estagiários
                    ModelState.AddModelError("", "Você só pode registrar frequência para estagiários que você supervisiona.");
                }
                // Coordenadores e administradores podem registrar para qualquer estagiário
            }

            // Verifica se já existe frequência registrada para o estagiário na mesma data
            bool existeRegistro = await _context.Frequencias
                .AnyAsync(f => f.EstagiarioId == frequencia.EstagiarioId && f.Data.Date == frequencia.Data.Date);

            if (existeRegistro)
            {
                ModelState.AddModelError("", "Já existe um registro de frequência para esse estagiário nesta data.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(frequencia);
                    await _context.SaveChangesAsync();

                    if (isEstagiario)
                    {
                        TempData["SuccessMessage"] = "Ponto registrado com sucesso!";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Frequência registrada com sucesso!";
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao salvar a frequência. Tente novamente.");
                }
            }

            // Recarregar as listas em caso de erro
            if (isEstagiario)
            {
                ViewData["EstagiarioId"] = new SelectList(
                    _context.Estagiarios.Where(e => e.Id == frequencia.EstagiarioId),
                    "Id", "Nome", frequencia.EstagiarioId);
            }
            else
            {
                // Para supervisores, mostrar apenas seus estagiários
                if (isSupervisor)
                {
                    ViewData["EstagiarioId"] = new SelectList(
                        _context.Estagiarios.Where(e => e.SupervisorId == user.Id),
                        "Id", "Nome", frequencia.EstagiarioId);
                }
                else
                {
                    // Para coordenadores e administradores, mostrar todos os estagiários
                    ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "Nome", frequencia.EstagiarioId);
                }

                ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Descricao", frequencia.JustificativaId);
            }

            return View(frequencia);
        }

        // GET: Frequencia/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var frequencia = await _context.Frequencias.FindAsync(id);
            if (frequencia == null)
            {
                return NotFound();
            }

            // Verificar permissões
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || frequencia.EstagiarioId != estagiario.Id)
                {
                    return Forbid();
                }
            }

            ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "Nome", frequencia.EstagiarioId);

            if (!isEstagiario)
            {
                ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Descricao", frequencia.JustificativaId);
            }

            return View(frequencia);
        }

        // POST: Frequencia/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EstagiarioId,Data,HoraEntrada,HoraSaida,Presente,Observacao,JustificativaId,DataRegistro,RegistradoPorId")] Frequencia frequencia)
        {
            if (id != frequencia.Id)
            {
                return NotFound();
            }

            // Verificar permissões
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || frequencia.EstagiarioId != estagiario.Id)
                {
                    return Forbid();
                }

                // Estagiários não podem alterar a presença ou justificativa
                var originalFrequencia = await _context.Frequencias.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (originalFrequencia != null)
                {
                    frequencia.Presente = originalFrequencia.Presente;
                    frequencia.JustificativaId = originalFrequencia.JustificativaId;
                }
            }

            // Validações
            if (frequencia.Data > DateTime.Today)
            {
                ModelState.AddModelError("Data", "A data não pode ser futura.");
            }

            if (frequencia.HoraEntrada.HasValue && frequencia.HoraSaida.HasValue &&
                frequencia.HoraEntrada.Value > frequencia.HoraSaida.Value)
            {
                ModelState.AddModelError("HoraSaida", "A hora de saída não pode ser anterior à hora de entrada.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(frequencia);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Frequência atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FrequenciaExists(frequencia.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "Nome", frequencia.EstagiarioId);

            if (!isEstagiario)
            {
                ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Descricao", frequencia.JustificativaId);
            }

            return View(frequencia);
        }

        // GET: Frequencia/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var frequencia = await _context.Frequencias
                .Include(f => f.Estagiario)
                .Include(f => f.Justificativa)
                .Include(f => f.RegistradoPor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (frequencia == null)
            {
                return NotFound();
            }

            // Verificar permissões (apenas administradores/coordenadores podem excluir)
            var isEstagiario = User.IsInRole("Estagiario");
            var isSupervisor = User.IsInRole("Supervisor");

            if (isEstagiario || isSupervisor)
            {
                return Forbid();
            }

            return View(frequencia);
        }

        // POST: Frequencia/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var frequencia = await _context.Frequencias.FindAsync(id);

            if (frequencia != null)
            {
                _context.Frequencias.Remove(frequencia);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Frequência excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Frequencia/EditList/5
        public async Task<IActionResult> EditList(int estagiarioId)
        {
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            // Se for estagiário, verificar se está tentando acessar seus próprios registros
            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || estagiario.Id != estagiarioId)
                {
                    return Forbid(); // Não permitir que estagiário acesse registros de outros
                }
            }

            var frequencias = await _context.Frequencias
                .Include(f => f.Estagiario)
                .Include(f => f.Justificativa)
                .Where(f => f.EstagiarioId == estagiarioId)
                .OrderByDescending(f => f.Data)
                .ThenByDescending(f => f.DataRegistro)
                .ToListAsync();

            ViewBag.EstagiarioId = estagiarioId;
            ViewBag.EstagiarioNome = await _context.Estagiarios
                .Where(e => e.Id == estagiarioId)
                .Select(e => e.Nome)
                .FirstOrDefaultAsync();

            return View(frequencias);
        }

        [HttpGet]
        public async Task<JsonResult> GetFrequenciasParaCalendario(int estagiarioId)
        {
            // Verificar permissões
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || estagiario.Id != estagiarioId)
                {
                    return Json(new { error = "Acesso não autorizado" });
                }
            }

            var frequencias = await _context.Frequencias
                .Where(f => f.EstagiarioId == estagiarioId)
                .Select(f => new
                {
                    date = f.Data.ToString("yyyy-MM-dd"),  // formato padrão ISO para JS
                    presente = f.Presente
                })
                .ToListAsync();

            return Json(frequencias);
        }

        private bool FrequenciaExists(int id)
        {
            return _context.Frequencias.Any(e => e.Id == id);
        }
    }
}