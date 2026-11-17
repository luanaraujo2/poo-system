using GestaoTempo.Domain.Entities;

namespace GestaoTempo.Domain.Services;

// US11 - Regras para distribuir as tarefas abertas nos blocos semanais de disponibilidade.
public static class PlanejadorSemanal
{
    public record JanelaBloco(
        Disponibilidade Disponibilidade,
        DateOnly Data,
        DateTime InicioBloco,
        DateTime FimBloco,
        DateTime InicioEfetivo,
        bool Passado,
        int DuracaoTotalMin,
        int MinutosDisponiveis);

    public record ResultadoGeracao(
        IReadOnlyList<PlanejamentoItem> Itens,
        IReadOnlyList<Tarefa> SemEspaco);

    public static DateOnly InicioDaSemana(DateOnly data)
    {
        int diasDesdeSegunda = ((int)data.DayOfWeek + 6) % 7;
        return data.AddDays(-diasDesdeSegunda);
    }

    public static DateTime ArredondarParaProximoMinuto(DateTime momento)
    {
        var baseMinuto = new DateTime(momento.Year, momento.Month, momento.Day, momento.Hour, momento.Minute, 0, momento.Kind);
        return momento.Second > 0 || momento.Millisecond > 0 ? baseMinuto.AddMinutes(1) : baseMinuto;
    }

    public static JanelaBloco CalcularJanela(DateOnly semanaInicio, Disponibilidade bloco, DateTime agora)
    {
        var data = semanaInicio.AddDays((int)bloco.DiaSemana);
        var inicioBloco = data.ToDateTime(bloco.HoraInicio, DateTimeKind.Local);
        var fimBloco = data.ToDateTime(bloco.HoraFim, DateTimeKind.Local);
        var agoraArredondado = ArredondarParaProximoMinuto(agora);

        var inicioEfetivo = inicioBloco < agoraArredondado ? agoraArredondado : inicioBloco;
        bool passado = fimBloco <= inicioEfetivo;
        int duracaoTotalMin = (int)(bloco.HoraFim - bloco.HoraInicio).TotalMinutes;
        int minutosDisponiveis = passado ? 0 : (int)(fimBloco - inicioEfetivo).TotalMinutes;

        return new JanelaBloco(bloco, data, inicioBloco, fimBloco, inicioEfetivo, passado, duracaoTotalMin, minutosDisponiveis);
    }

    public static ResultadoGeracao Gerar(
        int usuarioId,
        DateOnly semanaInicio,
        DateTime agora,
        IReadOnlyList<Disponibilidade> disponibilidades,
        IReadOnlyList<Tarefa> tarefas)
    {
        var janelas = disponibilidades
            .Select(d => CalcularJanela(semanaInicio, d, agora))
            .OrderBy(j => j.Data)
            .ThenBy(j => j.Disponibilidade.HoraInicio)
            .ToList();

        var cursores = janelas.ToDictionary(j => j.Disponibilidade.Id, j => j.InicioEfetivo);
        var ordens = janelas.ToDictionary(j => j.Disponibilidade.Id, _ => 0);

        // Prioriza quem vence antes; no empate de prazo, usa os critérios da US10.
        var candidatas = tarefas
            .Where(t => t.TarefaPaiId is null && t.EstaAberta())
            .OrderBy(t => t.Prazo.ToUniversalTime())
            .ThenByDescending(PontuarCriterios)
            .ThenBy(t => t.Id)
            .ToList();

        var itens = new List<PlanejamentoItem>();
        var semEspaco = new List<Tarefa>();

        foreach (var tarefa in candidatas)
        {
            var prazoUtc = tarefa.Prazo.ToUniversalTime();
            bool alocada = false;

            foreach (var janela in janelas)
            {
                if (janela.Passado) continue;

                var cursor = cursores[janela.Disponibilidade.Id];
                var fimSlot = cursor.AddMinutes(tarefa.EstimativaMin);

                if (fimSlot <= janela.FimBloco && fimSlot.ToUniversalTime() <= prazoUtc)
                {
                    int ordem = ordens[janela.Disponibilidade.Id]++;
                    itens.Add(new PlanejamentoItem(
                        usuarioId,
                        semanaInicio,
                        tarefa.Id,
                        janela.Disponibilidade.Id,
                        janela.Data,
                        TimeOnly.FromDateTime(cursor),
                        TimeOnly.FromDateTime(fimSlot),
                        ordem));

                    cursores[janela.Disponibilidade.Id] = fimSlot;
                    alocada = true;
                    break;
                }
            }

            if (!alocada)
                semEspaco.Add(tarefa);
        }

        return new ResultadoGeracao(itens, semEspaco);
    }

    public record SlotBloco(PlanejamentoItem Item, Tarefa Tarefa, TimeOnly Inicio, TimeOnly Fim);

    public record ResultadoEncaixe(
        IReadOnlyList<SlotBloco> ExistentesAtualizados,
        TimeOnly? InicioNova,
        TimeOnly? FimNova);

    // Reajusta a sequência de tarefas dentro de um bloco e valida que nenhuma passa do fim do bloco nem do próprio prazo.
    public static ResultadoEncaixe CalcularEncaixeNoBloco(
        JanelaBloco janela,
        IReadOnlyList<(PlanejamentoItem Item, Tarefa Tarefa)> existentes,
        Tarefa? novaTarefa = null)
    {
        if (janela.Passado)
            throw new InvalidOperationException("Não é possível mover uma tarefa para um bloco que já passou.");

        var cursor = janela.InicioEfetivo;
        var atualizados = new List<SlotBloco>();

        foreach (var (item, tarefa) in existentes)
        {
            var fim = cursor.AddMinutes(tarefa.EstimativaMin);
            if (fim > janela.FimBloco)
                throw new InvalidOperationException("Este bloco não tem tempo livre suficiente para esta tarefa.");
            if (fim.ToUniversalTime() > tarefa.Prazo.ToUniversalTime())
                throw new InvalidOperationException("Uma tarefa deste bloco terminaria depois do prazo.");

            atualizados.Add(new SlotBloco(item, tarefa, TimeOnly.FromDateTime(cursor), TimeOnly.FromDateTime(fim)));
            cursor = fim;
        }

        TimeOnly? inicioNova = null;
        TimeOnly? fimNova = null;
        if (novaTarefa is not null)
        {
            var fimSlotNova = cursor.AddMinutes(novaTarefa.EstimativaMin);
            if (fimSlotNova > janela.FimBloco)
                throw new InvalidOperationException("Este bloco não tem tempo livre suficiente para esta tarefa.");
            if (fimSlotNova.ToUniversalTime() > novaTarefa.Prazo.ToUniversalTime())
                throw new InvalidOperationException("Esta tarefa terminaria depois do prazo dela neste bloco.");

            inicioNova = TimeOnly.FromDateTime(cursor);
            fimNova = TimeOnly.FromDateTime(fimSlotNova);
        }

        return new ResultadoEncaixe(atualizados, inicioNova, fimNova);
    }

    public static string MotivoSemEspaco(Tarefa tarefa, IReadOnlyList<JanelaBloco> janelas, DateTime agora)
    {
        var prazoUtc = tarefa.Prazo.ToUniversalTime();
        if (prazoUtc <= agora.ToUniversalTime())
            return "Prazo já venceu";

        if (janelas.Count == 0 || janelas.All(j => j.Passado))
            return "Nenhum bloco disponível nesta semana";

        bool cabeEmAlgumBlocoAntesDoPrazo = janelas.Any(j =>
            !j.Passado
            && j.MinutosDisponiveis >= tarefa.EstimativaMin
            && j.InicioEfetivo.AddMinutes(tarefa.EstimativaMin).ToUniversalTime() <= prazoUtc);

        return cabeEmAlgumBlocoAntesDoPrazo
            ? "Sem espaço livre nos blocos antes do prazo"
            : "Nenhum bloco disponível antes do prazo comporta a duração";
    }

    private static int PontuarCriterios(Tarefa tarefa)
    {
        int pontosCriticidade = tarefa.Criticidade switch
        {
            Criticidade.Critica => 30,
            Criticidade.Alta => 20,
            Criticidade.Media => 10,
            _ => 0,
        };
        int pontosPrioridade = tarefa.Prioridade switch
        {
            Prioridade.Urgente_Importante => 20,
            Prioridade.Importante_NaoUrgente => 15,
            Prioridade.Urgente_NaoImportante => 10,
            _ => 0,
        };
        int pontosComplexidade = tarefa.Complexidade switch
        {
            Complexidade.Alta => 10,
            Complexidade.Media => 5,
            _ => 0,
        };
        return pontosCriticidade + pontosPrioridade + pontosComplexidade;
    }
}
