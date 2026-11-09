using GestaoTempo.Domain.Enums;

namespace GestaoTempo.Api.Dtos;

public record SalvarDisponibilidadeRequest(DiaSemana? DiaSemana, TimeOnly? HoraInicio, TimeOnly? HoraFim);

public record DisponibilidadeResponse(int Id, DiaSemana DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFim);
