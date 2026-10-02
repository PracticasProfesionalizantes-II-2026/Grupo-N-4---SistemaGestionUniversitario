public class PoliticasAcademicasTests
{
    [Fact]
    public void BajaExamen_SePermiteHastaAntesDeLas24Horas()
    {
        var examen = new DateTime(2026, 10, 8, 16, 0, 0);

        Assert.True(PoliticasAcademicas.PuedeDarseDeBajaExamen(
            examen, new DateTime(2026, 10, 7, 15, 59, 59)));
        Assert.False(PoliticasAcademicas.PuedeDarseDeBajaExamen(
            examen, new DateTime(2026, 10, 7, 16, 0, 0)));
    }

    [Fact]
    public void Correlativas_DevuelveSoloLasPendientesSinDuplicados()
    {
        var pendientes = PoliticasAcademicas.CorrelativasPendientes(
            new[] { 1, 2, 2, 3 }, new[] { 1, 3 });

        Assert.Equal(new[] { 2 }, pendientes);
    }

    [Theory]
    [InlineData("Regular")]
    [InlineData("Aprobada")]
    [InlineData("Promocionado")]
    public void EstadosAcademicosHabilitantes_PermitenCorrelativa(string estado) =>
        Assert.True(PoliticasAcademicas.EstadoHabilitaCorrelativa(estado));

    [Fact]
    public void Horarios_AdyacentesNoSeSuperponen()
    {
        Assert.False(PoliticasAcademicas.HorariosSeSuperponen(
            TimeSpan.FromHours(16), TimeSpan.FromHours(17),
            TimeSpan.FromHours(17), TimeSpan.FromHours(18)));
        Assert.True(PoliticasAcademicas.HorariosSeSuperponen(
            TimeSpan.FromHours(16), TimeSpan.FromHours(17),
            TimeSpan.FromHours(16.5), TimeSpan.FromHours(18)));
    }

    [Theory]
    [InlineData(EstadoPago.AlDia, true)]
    [InlineData(EstadoPago.Exentado, true)]
    [InlineData(EstadoPago.Pendiente, false)]
    [InlineData(EstadoPago.Impaga, false)]
    [InlineData(EstadoPago.EnRevision, false)]
    public void Pagos_DefinenLaHabilitacionDeFinales(EstadoPago estado, bool esperado) =>
        Assert.Equal(esperado, PoliticasAcademicas.EstadoPagoHabilitante(estado));

    [Theory]
    [InlineData(30, 29, true)]
    [InlineData(30, 30, false)]
    [InlineData(30, 31, false)]
    [InlineData(0, 0, false)]
    public void Cupos_DefinenSiLaComisionTieneVacante(int cupo, int ocupados, bool esperado) =>
        Assert.Equal(esperado, PoliticasAcademicas.HayVacante(cupo, ocupados));

    [Theory]
    [InlineData(6, 8, false, true)]
    [InlineData(6, 5, false, false)]
    [InlineData(11, null, false, false)]
    [InlineData(6, null, true, true)]
    public void Criterios_ValidanRegularizacionYPromocion(
        int regularizacion, int? promocion, bool esFinal, bool esperado) =>
        Assert.Equal(esperado, PoliticasAcademicas.CriteriosCalificacionValidos(
            regularizacion, promocion, esFinal));

    [Theory]
    [InlineData(9, 6, 8, false, "Promocionó")]
    [InlineData(7, 6, 8, false, "Regularizó")]
    [InlineData(5, 6, 8, false, "Desaprobó")]
    [InlineData(8, 6, 9, true, "Aprobó")]
    public void Notas_DeterminanCondicion(
        int nota, int regularizacion, int? promocion, bool esFinal, string esperado) =>
        Assert.Equal(esperado, PoliticasAcademicas.DeterminarCondicionNota(
            nota, regularizacion, promocion, esFinal));

    [Fact]
    public void Finales_PermitenCargarNotasSoloDuranteLosCatorceDiasPosteriores()
    {
        var examen = new DateTime(2026, 10, 8, 16, 0, 0);

        Assert.False(PoliticasAcademicas.PuedeCargarNotaFinal(
            examen, new DateTime(2026, 10, 8, 23, 59, 59)));
        Assert.True(PoliticasAcademicas.PuedeCargarNotaFinal(
            examen, new DateTime(2026, 10, 9, 0, 0, 0)));
        Assert.True(PoliticasAcademicas.PuedeCargarNotaFinal(
            examen, new DateTime(2026, 10, 22, 23, 59, 59)));
        Assert.False(PoliticasAcademicas.PuedeCargarNotaFinal(
            examen, new DateTime(2026, 10, 23, 0, 0, 0)));
    }

    [Theory]
    [InlineData("Ausente", true)]
    [InlineData("justificada", true)]
    [InlineData("Presente", false)]
    [InlineData("Tardanza", false)]
    public void Inasistencias_SoloPermitenJustificarAusencias(string estado, bool esperado) =>
        Assert.Equal(esperado, PoliticasAcademicas.PuedeJustificarInasistencia(estado));

    [Fact]
    public void Promedio_RedondeaADosDecimales() =>
        Assert.Equal(7.67m, PoliticasAcademicas.CalcularPromedio(new[] { 7m, 8m, 8m }));
}
