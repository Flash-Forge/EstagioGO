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

        // GET: Frequencia
        public async Task<IActionResult> Index()
        {
            // Verificar se existe algum estagiário cadastrado
            bool existemEstagiarios = await context.Estagiarios.AnyAsync();

            if (!existemEstagiarios)
            {
                ViewBag.MensagemSemRecursos = "Não há estagiários cadastrados. Você precisa cadastrar um estagiário primeiro.";
                ViewBag.RedirecionarPara = Url.Action("Create", "Estagiarios");
                return View("SemEstagiarios"); // Usa a nova view específica
            }

            var user = await userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                // Para estagiários, buscar apenas seus próprios registros
                var estagiario = await context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user!.Id);

                if (estagiario != null)
                {
                    ViewBag.EstagiarioId = estagiario.Id;
                    ViewBag.EstagiarioNome = estagiario.Nome;

                    // Carregar as frequências do estagiário
                    var frequencias = await context.Frequencias
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
                ViewBag.Estagiarios = await context.Estagiarios.OrderBy(e => e.Nome).ToListAsync();
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

            var frequencia = await context.Frequencias
                .Include(f => f.Estagiario)
                .Include(f => f.RegistradoPor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (frequencia == null)
            {
                return NotFound();
            }

            // Verificar se o usuário tem permissão para ver este registro
            var user = await userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                var estagiario = await context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user!.Id);

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
            bool existemEstagiarios = await context.Estagiarios.AnyAsync();

            if (!existemEstagiarios)
            {
                ViewBag.MensagemSemRecursos = "Não há estagiários cadastrados. Você precisa cadastrar um estagiário primeiro.";
                ViewBag.RedirecionarPara = Url.Action("Create", "Estagiarios");
                return View("SemEstagiarios");
            }

            var user = await userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");
            var isSupervisor = User.IsInRole("Supervisor");
            var isCoordenador = User.IsInRole("Coordenador");
            var isAdministrador = User.IsInRole("Administrador");

            // Se for estagiário, buscar seu próprio ID
            if (isEstagiario)
            {
                var estagiario = await context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user!.Id);

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
                    context.Estagiarios.Where(e => e.Id == estagiarioId),
                    "Id", "Nome", estagiarioId);
            }
            else
            {
                // Para supervisores, mostrar apenas seus estagiários
                if (isSupervisor)
                {
                    ViewData["EstagiarioId"] = new SelectList(
                        context.Estagiarios.Where(e => e.SupervisorId == user!.Id),
                        "Id", "Nome", estagiarioId);
                }
                else
                {
                    // Para coordenadores e administradores, mostrar todos os estagiários
                    ViewData["EstagiarioId"] = new SelectList(context.Estagiarios, "Id", "Nome", estagiarioId);
                }
            }

            // Carregar Justificativas apenas para não-estagiários
            if (!isEstagiario)
            {
                ViewData["Motivo"] = new SelectList(context.Frequencias, "Motivo", "Detalhamento");
            }

            // Se for estagiário, definir valores padrão
            if (isEstagiario)
            {
                var model = new Frequencia
                {
                    EstagiarioId = estagiarioId!.Value,
                    Data = DateTime.Today,
                    HoraEntrada = DateTime.Now.TimeOfDay,
                    Presente = true,
                    DataRegistro = DateTime.Now,
                    RegistradoPorId = user!.Id,
                    Motivo = null,
                    Detalhamento = null
                };

                return View(model);
            }

            ViewBag.CurrentUserId = user!.Id;

            return View();
        }

        // POST: Frequencia/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,EstagiarioId,Data,HoraEntrada,HoraSaida,Presente,Observacao,Motivo,Detalhamento")] Frequencia frequencia)
        {
            var user = await userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");
            var isSupervisor = User.IsInRole("Supervisor");
            var isCoordenador = User.IsInRole("Coordenador");
            var isAdministrador = User.IsInRole("Administrador");

            // Remover validação das propriedades de navegação
            ModelState.Remove("Estagiario");
            ModelState.Remove("RegistradoPor");
            ModelState.Remove("Motivo");
            ModelState.Remove("Detalhamento");
            ModelState.Remove("RegistradoPorId");

            // Definir o RegistradoPorId ANTES de qualquer validação
            frequencia.RegistradoPorId = user!.Id;
            frequencia.DataRegistro = DateTime.Now;
            frequencia.Motivo = "";
            frequencia.Detalhamento = "";

            // Buscar informações do estagiário para validação
            var estagiario = await context.Estagiarios
                .FirstOrDefaultAsync(e => e.Id == frequencia.EstagiarioId);

            if (estagiario == null)
            {
                ModelState.AddModelError("EstagiarioId", "Estagiário não encontrado.");
                return View(frequencia);
            }

            // VALIDAÇÃO: Data dentro do período do estágio
            if (frequencia.Data < estagiario.DataInicio)
            {
                ModelState.AddModelError("Data", $"A data não pode ser anterior ao início do estágio ({estagiario.DataInicio:dd/MM/yyyy}).");
            }

            if (estagiario.DataTermino.HasValue && frequencia.Data > estagiario.DataTermino.Value)
            {
                ModelState.AddModelError("Data", $"A data não pode ser posterior ao término do estágio ({estagiario.DataTermino.Value:dd/MM/yyyy}).");
            }

            // Validações de ponto
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
                frequencia.Motivo = null;
                frequencia.Detalhamento = null;

                // Verificar se o estagiário está tentando registrar para si mesmo
                if (estagiario == null || frequencia.EstagiarioId != estagiario.Id)
                {
                    return Forbid();
                }
            }
            else
            {
                // Para supervisores, coordenadores e administradores, verificar permissões
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

                if (frequencia.Motivo == null && !frequencia.Presente)
                {
                    ModelState.AddModelError("", "É preciso justificar a falta do Estagiário");
                }
                else
                {
                    ModelState.Remove("Motivo");
                }
            }

            // Verifica se já existe frequência registrada para o estagiário na mesma data
            bool existeRegistro = await context.Frequencias
                .AnyAsync(f => f.EstagiarioId == frequencia.EstagiarioId && f.Data.Date == frequencia.Data.Date);

            if (existeRegistro)
            {
                ModelState.AddModelError("", "Já existe um registro de frequência para esse estagiário nesta data.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    context.Add(frequencia);
                    await context.SaveChangesAsync();

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
                    context.Estagiarios.Where(e => e.Id == frequencia.EstagiarioId),
                    "Id", "Nome", frequencia.EstagiarioId);
            }
            else
            {
                // Para supervisores, mostrar apenas seus estagiários
                if (isSupervisor)
                {
                    ViewData["EstagiarioId"] = new SelectList(
                        context.Estagiarios.Where(e => e.SupervisorId == user!.Id),
                        "Id", "Nome", frequencia.EstagiarioId);
                }
                else
                {
                    // Para coordenadores e administradores, mostrar todos os estagiários
                    ViewData["EstagiarioId"] = new SelectList(context.Estagiarios, "Id", "Nome", frequencia.EstagiarioId);
                }

                ViewData["Motivo"] = new SelectList(context.Frequencias, "Id", "Detalhamento", frequencia.Motivo);
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

            var frequencia = await context.Frequencias
                .Include(f => f.Estagiario)
                .Include(f => f.RegistradoPor)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (frequencia == null)
            {
                return NotFound();
            }

            // Impedir acesso de estagiários
            var isEstagiario = User.IsInRole("Estagiario");
            if (isEstagiario)
            {
                return Forbid();
            }

            // Verificar permissões para supervisores (só podem editar seus estagiários)
            var user = await userManager.GetUserAsync(User);
            var isSupervisor = User.IsInRole("Supervisor");

            if (isSupervisor)
            {
                var estagiario = await context.Estagiarios
                    .FirstOrDefaultAsync(e => e.Id == frequencia.EstagiarioId);

                if (estagiario == null || estagiario.SupervisorId != user!.Id)
                {
                    return Forbid();
                }
            }

            return View(frequencia);
        }

        // POST: Frequencia/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EstagiarioId,Data,HoraEntrada,HoraSaida,Presente,Observacao,Motivo,Detalhamento,DataRegistro,RegistradoPorId")] Frequencia frequencia)
        {
            if (id != frequencia.Id)
            {
                return NotFound();
            }

            var user = await userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");
            var isSupervisor = User.IsInRole("Supervisor");
            var isCoordenador = User.IsInRole("Coordenador");
            var isAdministrador = User.IsInRole("Administrador");

            // Impedir acesso de estagiários
            if (isEstagiario)
            {
                return Forbid();
            }

            // Remover validação de campos desnecessários
            ModelState.Remove("Estagiario");
            ModelState.Remove("RegistradoPor");
            ModelState.Remove("Motivo");
            ModelState.Remove("Detalhamento");

            // Buscar a frequência original para preservar dados
            var frequenciaOriginal = await context.Frequencias
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (frequenciaOriginal == null)
            {
                return NotFound();
            }

            // Buscar informações do estagiário para validação
            var estagiario = await context.Estagiarios
                .FirstOrDefaultAsync(e => e.Id == frequencia.EstagiarioId);

            if (estagiario == null)
            {
                ModelState.AddModelError("", "Estagiário não encontrado.");
                return View(frequencia);
            }

            // VALIDAÇÃO: Data dentro do período do estágio (apenas se administrador alterou a data)
            if (isAdministrador && frequencia.Data != frequenciaOriginal.Data)
            {
                if (frequencia.Data < estagiario.DataInicio)
                {
                    ModelState.AddModelError("Data", $"A data não pode ser anterior ao início do estágio ({estagiario.DataInicio:dd/MM/yyyy}).");
                }

                if (estagiario.DataTermino.HasValue && frequencia.Data > estagiario.DataTermino.Value)
                {
                    ModelState.AddModelError("Data", $"A data não pode ser posterior ao término do estágio ({estagiario.DataTermino.Value:dd/MM/yyyy}).");
                }
            }

            // Verificar permissões para supervisores
            if (isSupervisor)
            {
                if (estagiario == null || estagiario.SupervisorId != user!.Id)
                {
                    return Forbid();
                }
            }

            // Preenchimentos automáticos
            frequencia.EstagiarioId = frequenciaOriginal.EstagiarioId;
            frequencia.DataRegistro = DateTime.Now;
            frequencia.RegistradoPorId = user!.Id;

            // Apenas administradores podem alterar a data
            if (!isAdministrador)
            {
                frequencia.Data = frequenciaOriginal.Data;
            }

            // VALIDAÇÕES ESPECÍFICAS

            // Validação para presença: horários são obrigatórios
            if (frequencia.Presente)
            {
                if (!frequencia.HoraEntrada.HasValue || !frequencia.HoraSaida.HasValue)
                {
                    ModelState.AddModelError("", "É necessário informar tanto a hora de entrada quanto a hora de saída quando o estagiário está presente.");
                }
                else if (frequencia.HoraEntrada.Value > frequencia.HoraSaida.Value)
                {
                    ModelState.AddModelError("HoraSaida", "A hora de saída não pode ser anterior à hora de entrada.");
                }

                // Limpar campos de ausência quando presente
                frequencia.Motivo = "";
                frequencia.Detalhamento = "";
                ModelState.Remove("Motivo");
                ModelState.Remove("Detalhamento");
            }
            else
            {
                // Validação para ausência: motivo é obrigatório
                if (string.IsNullOrEmpty(frequencia.Motivo))
                {
                    ModelState.AddModelError("Motivo", "É necessário informar o motivo da falta.");
                }

                // Manter os horários originais no banco, mas não exibir na UI
                frequencia.HoraEntrada = frequenciaOriginal.HoraEntrada;
                frequencia.HoraSaida = frequenciaOriginal.HoraSaida;
            }

            // Validar data (apenas se administrador alterou a data)
            if (isAdministrador && frequencia.Data != frequenciaOriginal.Data)
            {
                if (frequencia.Data > DateTime.Today)
                {
                    ModelState.AddModelError("Data", "A data não pode ser futura.");
                }

                // Verificar se já existe frequência para a nova data
                bool existeRegistro = await context.Frequencias
                    .AnyAsync(f => f.EstagiarioId == frequencia.EstagiarioId &&
                                  f.Data.Date == frequencia.Data.Date &&
                                  f.Id != frequencia.Id);

                if (existeRegistro)
                {
                    ModelState.AddModelError("Data", "Já existe um registro de frequência para esse estagiário nesta data.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Se mudou de presente para ausente, preservar os horários originais
                    if (!frequencia.Presente && frequenciaOriginal.Presente)
                    {
                        frequencia.HoraEntrada = frequenciaOriginal.HoraEntrada;
                        frequencia.HoraSaida = frequenciaOriginal.HoraSaida;
                    }

                    context.Update(frequencia);
                    await context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Frequência atualizada com sucesso!";
                    return RedirectToAction("EditList", new { estagiarioId = frequencia.EstagiarioId });
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
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao salvar as alterações: " + ex.Message);
                }
            }

            // Recarregar dados de navegação para a view
            if (estagiario != null)
            {
                frequencia.Estagiario = estagiario;
            }
            else
            {
                ModelState.AddModelError("", "Estagiário não encontrado.");
            }

            var registradoPor = await userManager.FindByIdAsync(frequencia.RegistradoPorId.ToString());
            if (registradoPor != null)
            {
                frequencia.RegistradoPor = registradoPor;
            }
            else
            {
                ModelState.AddModelError("", "Usuário registrador não encontrado.");
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

            var frequencia = await context.Frequencias
                .Include(f => f.Estagiario)
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
            var frequencia = await context.Frequencias.FindAsync(id);

            if (frequencia != null)
            {
                context.Frequencias.Remove(frequencia);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Frequência excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Frequencia/EditList/5
        public async Task<IActionResult> EditList(int estagiarioId)
        {
            var user = await userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            // Se for estagiário, verificar se está tentando acessar seus próprios registros
            if (isEstagiario)
            {
                var estagiario = await context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user!.Id);

                if (estagiario == null || estagiario.Id != estagiarioId)
                {
                    return Forbid(); // Não permitir que estagiário acesse registros de outros
                }
            }

            var frequencias = await context.Frequencias
                .Include(f => f.Estagiario)
                .Where(f => f.EstagiarioId == estagiarioId)
                .OrderByDescending(f => f.Data)
                .ThenByDescending(f => f.DataRegistro)
                .ToListAsync();

            ViewBag.EstagiarioId = estagiarioId;

            var estagiarioNome = await context.Estagiarios
                .Where(e => e.Id == estagiarioId)
                .Select(e => e.Nome)
                .FirstOrDefaultAsync();

            ViewBag.EstagiarioNome = estagiarioNome ?? "Estagiário não encontrado";

            return View(frequencias);
        }

        [HttpGet]
        public async Task<JsonResult> GetFrequenciasParaCalendario(int estagiarioId)
        {
            // Verificar permissões
            var user = await userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                var estagiario = await context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user!.Id);

                if (estagiario == null || estagiario.Id != estagiarioId)
                {
                    return Json(new { error = "Acesso não autorizado" });
                }
            }

            var frequencias = await context.Frequencias
                .Where(f => f.EstagiarioId == estagiarioId)
                .Select(f => new
                {
                    id = f.Id,
                    date = f.Data.ToString("yyyy-MM-dd"),
                    presente = f.Presente
                })
                .ToListAsync();

            return Json(frequencias);
        }

        private bool FrequenciaExists(int id)
        {
            return context.Frequencias.Any(e => e.Id == id);
        }

        // GET: Frequencia/VerificarDataExistente
        [HttpGet]
        public async Task<JsonResult> VerificarDataExistente(int estagiarioId, DateTime data, int frequenciaId = 0)
        {
            try
            {
                // Verificar se existe frequência para o mesmo estagiário na mesma data, excluindo a frequência atual
                bool existe = await context.Frequencias
                    .AnyAsync(f => f.EstagiarioId == estagiarioId &&
                                  f.Data.Date == data.Date &&
                                  f.Id != frequenciaId);

                return Json(new { existe });
            }
            catch (Exception ex)
            {
                return Json(new { existe = false, error = ex.Message });
            }
        }
    }
}