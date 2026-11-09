using GestaoTempo.Domain.Enums;

namespace GestaoTempo.Domain.Entities;

public class Disponibilidade
{
    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public DiaSemana DiaSemana { get; private set; }
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFim { get; private set; }

    public Disponibilidade(int usuarioId, DiaSemana diaSemana, TimeOnly horaInicio, TimeOnly horaFim)
    {
        if (usuarioId <= 0)
            throw new ArgumentException("Usuário inválido.");
        ValidarHorario(horaInicio, horaFim);
        UsuarioId = usuarioId;
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        HoraFim = horaFim;
    }

#pragma warning disable CS8618
    private Disponibilidade() { }
#pragma warning restore CS8618

    public void Editar(DiaSemana diaSemana, TimeOnly horaInicio, TimeOnly horaFim)
    {
        ValidarHorario(horaInicio, horaFim);
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        HoraFim = horaFim;
    }

    private static void ValidarHorario(TimeOnly horaInicio, TimeOnly horaFim)
    {
        if (horaFim <= horaInicio)
            throw new ArgumentException("A hora de fim deve ser maior que a hora de início.");
    }
}
