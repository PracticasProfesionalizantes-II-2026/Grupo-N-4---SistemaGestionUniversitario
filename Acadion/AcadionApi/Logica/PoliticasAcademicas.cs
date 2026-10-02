public static class PoliticasAcademicas
{
    private static readonly HashSet<string> EstadosCorrelativaHabilitantes = new(
        new[] { "Regular", "Regularizada", "Regularizado", "Aprobada", "Aprobado", "Promocionada", "Promocionado" },
        StringComparer.OrdinalIgnoreCase);

    public static DateTime CalcularLimiteBajaExamen(DateTime fechaExamen) => fechaExamen.AddHours(-24);

    public static bool PuedeDarseDeBajaExamen(DateTime fechaExamen, DateTime ahora) =>
        ahora < CalcularLimiteBajaExamen(fechaExamen);

    public static bool EstadoHabilitaCorrelativa(string? estado) =>
        !string.IsNullOrWhiteSpace(estado) && EstadosCorrelativaHabilitantes.Contains(estado);

    public static IReadOnlyCollection<int> CorrelativasPendientes(
        IEnumerable<int> correlativasRequeridas, IEnumerable<int> materiasHabilitadas)
    {
        var aprobadas = materiasHabilitadas.ToHashSet();
        return correlativasRequeridas.Where(id => !aprobadas.Contains(id)).Distinct().ToArray();
    }

    public static bool HorariosSeSuperponen(TimeSpan inicioA, TimeSpan finA,
        TimeSpan inicioB, TimeSpan finB) => inicioA < finB && inicioB < finA;

    public static bool EstadoPagoHabilitante(EstadoPago estado) =>
        estado is EstadoPago.AlDia or EstadoPago.Exentado;

    public static bool HayVacante(int cupo, int ocupados) =>
        cupo > 0 && ocupados >= 0 && ocupados < cupo;

    public static bool CriteriosCalificacionValidos(
        decimal notaMinimaRegularizacion, decimal? notaMinimaPromocion, bool esFinal = false) =>
        notaMinimaRegularizacion is >= 0 and <= 10 &&
        (esFinal || !notaMinimaPromocion.HasValue ||
            notaMinimaPromocion.Value is >= 0 and <= 10 &&
            notaMinimaPromocion.Value >= notaMinimaRegularizacion);

    public static string DeterminarCondicionNota(
        decimal nota, decimal notaMinimaRegularizacion, decimal? notaMinimaPromocion, bool esFinal)
    {
        if (esFinal)
            return nota >= notaMinimaRegularizacion ? "Aprobó" : "Desaprobó";
        if (notaMinimaPromocion.HasValue && nota >= notaMinimaPromocion.Value)
            return "Promocionó";
        return nota >= notaMinimaRegularizacion ? "Regularizó" : "Desaprobó";
    }

    public static DateTime InicioCargaNotaFinal(DateTime fechaExamen) =>
        fechaExamen.Date.AddDays(1);

    public static DateTime LimiteCargaNotaFinal(DateTime fechaExamen) =>
        fechaExamen.Date.AddDays(15).AddTicks(-1);

    public static bool PuedeCargarNotaFinal(DateTime fechaExamen, DateTime ahora) =>
        ahora >= InicioCargaNotaFinal(fechaExamen) && ahora <= LimiteCargaNotaFinal(fechaExamen);

    public static bool PuedeJustificarInasistencia(string? estado) =>
        string.Equals(estado?.Trim(), "Ausente", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(estado?.Trim(), "Justificada", StringComparison.OrdinalIgnoreCase);

    public static decimal? CalcularPromedio(IEnumerable<decimal> notas)
    {
        var valores = notas.ToArray();
        return valores.Length == 0 ? null : Math.Round(valores.Average(), 2);
    }
}
