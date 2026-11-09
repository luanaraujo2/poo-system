using GestaoTempo.Api.Data;
using GestaoTempo.Api.Dtos;
using GestaoTempo.Api.Services;
using GestaoTempo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoTempo.Api.Controllers;

[ApiController]
[Route("api/disponibilidades")]
public class DisponibilidadesController : ControllerAutenticado
{
    private readonly AppDbContext _db;

    public DisponibilidadesController(AppDbContext db, UsuarioAtual usuarioAtual) : base(usuarioAtual)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<DisponibilidadeResponse>>> Listar()
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        return await _db.Disponibilidades
            .Where(d => d.UsuarioId == usuarioId)
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.HoraInicio)
            .Select(d => new DisponibilidadeResponse(d.Id, d.DiaSemana, d.HoraInicio, d.HoraFim))
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<DisponibilidadeResponse>> Criar(SalvarDisponibilidadeRequest req)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();
        if (Validar(req) is { } erro)
            return BadRequest(erro);

        var dia = req.DiaSemana!.Value;
        var inicio = req.HoraInicio!.Value;
        var fim = req.HoraFim!.Value;

        // Serializa gravações do mesmo usuário/dia para duas requisições simultâneas
        // não passarem juntas pela verificação de sobreposição.
        await using var transacao = await _db.Database.BeginTransactionAsync();
        await BloquearDiaAsync(usuarioId, dia);
        if (await Sobrepoe(usuarioId, dia, inicio, fim))
            return Conflict(new ErroResponse("Este horário se sobrepõe a outro bloco deste dia."));

        var disponibilidade = new Disponibilidade(usuarioId, dia, inicio, fim);
        _db.Disponibilidades.Add(disponibilidade);
        await _db.SaveChangesAsync();
        await transacao.CommitAsync();
        var resposta = new DisponibilidadeResponse(disponibilidade.Id, dia, inicio, fim);
        return CreatedAtAction(nameof(Listar), resposta);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DisponibilidadeResponse>> Editar(int id, SalvarDisponibilidadeRequest req)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var disponibilidade = await _db.Disponibilidades
            .FirstOrDefaultAsync(d => d.Id == id && d.UsuarioId == usuarioId);
        if (disponibilidade is null)
            return NotFound(new ErroResponse("Bloco de disponibilidade não encontrado."));
        if (Validar(req) is { } erro)
            return BadRequest(erro);

        var dia = req.DiaSemana!.Value;
        var inicio = req.HoraInicio!.Value;
        var fim = req.HoraFim!.Value;

        await using var transacao = await _db.Database.BeginTransactionAsync();
        await BloquearDiaAsync(usuarioId, dia);
        if (await Sobrepoe(usuarioId, dia, inicio, fim, id))
            return Conflict(new ErroResponse("Este horário se sobrepõe a outro bloco deste dia."));

        disponibilidade.Editar(dia, inicio, fim);
        await _db.SaveChangesAsync();
        await transacao.CommitAsync();
        return new DisponibilidadeResponse(id, dia, inicio, fim);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var disponibilidade = await _db.Disponibilidades
            .FirstOrDefaultAsync(d => d.Id == id && d.UsuarioId == usuarioId);
        if (disponibilidade is null)
            return NotFound(new ErroResponse("Bloco de disponibilidade não encontrado."));

        _db.Disponibilidades.Remove(disponibilidade);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ErroResponse? Validar(SalvarDisponibilidadeRequest req)
    {
        if (req.DiaSemana is null || !Enum.IsDefined(req.DiaSemana.Value))
            return new ErroResponse("Selecione um dia da semana válido.");
        if (req.HoraInicio is null || req.HoraFim is null)
            return new ErroResponse("Informe a hora de início e a hora de fim.");
        if (req.HoraFim.Value <= req.HoraInicio.Value)
            return new ErroResponse("A hora de fim deve ser maior que a hora de início.");
        return null;
    }

    private Task<int> BloquearDiaAsync(int usuarioId, GestaoTempo.Domain.Enums.DiaSemana dia)
    {
        var chaveDia = (int)dia;
        return _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({usuarioId}, {chaveDia})");
    }

    // Intervalos adjacentes são permitidos; só há conflito quando a interseção é maior que zero.
    private Task<bool> Sobrepoe(int usuarioId, GestaoTempo.Domain.Enums.DiaSemana dia,
        TimeOnly inicio, TimeOnly fim, int? idIgnorado = null) =>
        _db.Disponibilidades.AnyAsync(d => d.UsuarioId == usuarioId && d.DiaSemana == dia
            && d.Id != idIgnorado && d.HoraInicio < fim && d.HoraFim > inicio);
}
