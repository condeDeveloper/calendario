using Xunit;

namespace Conde.Calendario.Testes;

/// <summary>
/// O leitor do ISO 8601 — e, sobretudo, o que ele <b>recusa</b>.
/// </summary>
/// <remarks>
/// <para>
/// A metade fácil de um leitor de formato é aceitar o que é válido, e ela está
/// testada exaustivamente no <c>JuizTest</c>: todos os 3,6 milhões de dias, nas
/// quatro grafias, lidos de volta.
/// </para>
/// <para>
/// A metade difícil é recusar o resto, e é onde os leitores caseiros falham. Um
/// leitor generoso demais não avisa que a entrada está errada — ele <b>inventa
/// uma data</b>, e ela segue adiante como se fosse verdade.
/// </para>
/// </remarks>
public class IsoTest
{
    [Theory(DisplayName = "o que tem de ser aceito")]
    [InlineData("2026-09-29", 2026, 9, 29)]
    [InlineData("20260929", 2026, 9, 29)]
    [InlineData("2026-272", 2026, 9, 29)]
    [InlineData("2026272", 2026, 9, 29)]
    [InlineData("2026-W40-2", 2026, 9, 29)]
    [InlineData("2026W402", 2026, 9, 29)]
    [InlineData("0001-01-01", 1, 1, 1)]
    [InlineData("9999-12-31", 9999, 12, 31)]
    [InlineData("2024-02-29", 2024, 2, 29)]
    [InlineData("2000-02-29", 2000, 2, 29)]
    // O ano da semana não é o ano da data, e o leitor tem de honrar isso: a
    // semana 1 de 2027 começa em 4 de janeiro, então 2026-W53-5 é 1º de janeiro
    // de 2027.
    [InlineData("2026-W53-5", 2027, 1, 1)]
    [InlineData("2030-W01-1", 2029, 12, 31)]
    public void OQueEhAceito(string texto, int ano, int mes, int dia)
    {
        Assert.Equal(Data.De(ano, mes, dia), Iso.Ler(texto));
    }

    [Theory(DisplayName = "o que tem de ser RECUSADO")]
    // Dias que não existem no mês.
    [InlineData("2026-02-29")]
    [InlineData("2026-04-31")]
    [InlineData("1900-02-29")]
    [InlineData("2026-00-10")]
    [InlineData("2026-13-01")]
    [InlineData("2026-01-00")]
    [InlineData("2026-01-32")]
    // Fora do intervalo representável.
    [InlineData("0000-12-31")]
    // Formas misturadas: a norma proíbe o meio-termo entre estendido e compacto.
    [InlineData("2026-0929")]
    [InlineData("202609-29")]
    [InlineData("2026-W402")]
    [InlineData("2026W40-2")]
    // Separadores errados.
    [InlineData("2026/09/29")]
    [InlineData("2026.09.29")]
    [InlineData("2026 09 29")]
    // Tamanhos errados.
    [InlineData("26-09-29")]
    [InlineData("2026-9-29")]
    [InlineData("2026-09-9")]
    [InlineData("2026-09-290")]
    [InlineData("")]
    [InlineData("   ")]
    // Sinal e espaço, que o int.TryParse aceitaria de bom grado.
    [InlineData("+2026-09-29")]
    [InlineData("-2026-09-29")]
    [InlineData(" 2026-09-29")]
    [InlineData("2026-09-29 ")]
    [InlineData("2026-+9-29")]
    [InlineData("2026-0 -29")]
    // Dia do ano impossível.
    [InlineData("2026-366")]
    [InlineData("2026-000")]
    [InlineData("2026-400")]
    // Semana e dia da semana impossíveis.
    [InlineData("2026-W54-1")]
    [InlineData("2026-W00-1")]
    [InlineData("2026-W40-0")]
    [InlineData("2026-W40-8")]
    // 2025 tem 52 semanas, e a 53 não existe nele.
    [InlineData("2025-W53-1")]
    // Hora, fuso e duração: fora do que esta biblioteca promete.
    [InlineData("2026-09-29T00:00:00")]
    [InlineData("2026-09-29Z")]
    [InlineData("P1Y2M3D")]
    // Letras onde deviam ir algarismos.
    [InlineData("ABCD-09-29")]
    [InlineData("2026-AB-29")]
    public void OQueEhRecusado(string texto)
    {
        Assert.False(Iso.TentarLer(texto, out _), $"aceitou \"{texto}\"");

        Assert.Throws<FormatException>(() => Iso.Ler(texto));
    }

    [Fact(DisplayName = "nulo é recusado sem estourar")]
    public void Nulo()
    {
        Assert.False(Iso.TentarLer(null, out _));
    }

    [Fact(DisplayName = "366 dias só num ano bissexto")]
    public void ODiaTrezentosESessentaESeis()
    {
        Assert.Equal(Data.De(2024, 12, 31), Iso.Ler("2024-366"));

        Assert.False(Iso.TentarLer("2025-366", out _));

        // E 2100 não é bissexto, apesar de divisível por 4.
        Assert.False(Iso.TentarLer("2100-366", out _));
        Assert.Equal(Data.De(2000, 12, 31), Iso.Ler("2000-366"));
    }

    [Fact(DisplayName = "o texto por extenso e os nomes")]
    public void PorExtenso()
    {
        Assert.Equal("29 de setembro de 2026", Iso.PorExtenso(Data.De(2026, 9, 29)));
        Assert.Equal("1 de janeiro de 2000", Iso.PorExtenso(Data.De(2000, 1, 1)));

        Assert.Equal("março", Iso.NomeDoMes(3));
        Assert.Equal("dezembro", Iso.NomeDoMes(12));

        Assert.Equal("segunda-feira", Iso.NomeDoDia(1));
        Assert.Equal("domingo", Iso.NomeDoDia(7));

        // 29 de setembro de 2026 é uma terça-feira.
        Assert.Equal("terça-feira", Iso.NomeDoDia(Data.De(2026, 9, 29).DiaDaSemana()));

        Assert.Throws<ArgumentOutOfRangeException>(() => Iso.NomeDoMes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Iso.NomeDoMes(13));
        Assert.Throws<ArgumentOutOfRangeException>(() => Iso.NomeDoDia(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Iso.NomeDoDia(8));
    }

    [Fact(DisplayName = "a semana ISO, nos casos que todo mundo erra")]
    public void ASemanaNosCasosDifíceis()
    {
        // 1º de janeiro de 2027 é uma sexta-feira, e pertence à semana 53 de
        // 2026. Um relatório que agrupe por (ano da data, semana) põe esse dia
        // numa "semana 53 de 2027" que não existe.
        Assert.Equal("2026-W53-5", Iso.EscreverSemana(Data.De(2027, 1, 1)));

        // 31 de dezembro de 2029 é uma segunda, e abre a semana 1 de 2030.
        Assert.Equal("2030-W01-1", Iso.EscreverSemana(Data.De(2029, 12, 31)));

        // 1º de janeiro de 2026 é uma quinta-feira, e portanto a semana 1 de
        // 2026 começa em 29 de dezembro de 2025.
        Assert.Equal("2026-W01-4", Iso.EscreverSemana(Data.De(2026, 1, 1)));
        Assert.Equal(Data.De(2025, 12, 29), new Semana(2026, 1, 1).ParaData());

        // Os anos de 53 semanas, que são os que quebram o pressuposto de 52.
        Assert.Equal(53, Semana.QuantasSemanas(2026));
        Assert.Equal(53, Semana.QuantasSemanas(2020));
        Assert.Equal(53, Semana.QuantasSemanas(2015));
        Assert.Equal(52, Semana.QuantasSemanas(2025));

        // A segunda e o domingo que cercam um dia.
        var semana = Semana.De(Data.De(2026, 9, 29));

        Assert.Equal(Data.De(2026, 9, 28), semana.Segunda());
        Assert.Equal(Data.De(2026, 10, 4), semana.Domingo());
        Assert.Equal(6, semana.Segunda().Ate(semana.Domingo()));
    }
}
