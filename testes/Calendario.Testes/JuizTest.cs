using System.Globalization;
using Xunit;

namespace Conde.Calendario.Testes;

/// <summary>
/// O juiz: <b>todos</b> os dias, um a um.
/// </summary>
/// <remarks>
/// <para>
/// Este projeto tem o juiz mais completo que dá para ter, e vale dizer por quê.
/// O domínio inteiro de uma data gregoriana entre 0001-01-01 e 9999-12-31 tem
/// <b>3.652.059</b> elementos. Isso cabe num laço de dois segundos.
/// </para>
/// <para>
/// Então não há amostra aqui, e não há sorteio: cada dia representável é montado,
/// convertido nos dois sentidos e comparado com o <c>DateOnly</c> do .NET. O que
/// os testes deste arquivo produzem não é evidência estatística — é
/// <b>verificação exaustiva</b>. Se eles passam, não existe uma data em que a
/// conversão erre, porque todas foram experimentadas.
/// </para>
/// <para>
/// Essa é uma sorte rara. Quase todo código tem um domínio grande demais para
/// isso, e por isso se testa por amostra, por propriedade ou por exemplo. Quando
/// o domínio <b>cabe</b>, testar por amostra é desperdiçar a única chance de ter
/// certeza.
/// </para>
/// </remarks>
public class JuizTest
{
    private static readonly DateOnly Epoca = new(1970, 1, 1);

    [Fact(DisplayName = "TODOS os 3.652.059 dias: ida e volta contra o DateOnly")]
    public void TodosOsDias()
    {
        var doNet = new DateOnly(1, 1, 1);
        var ultimo = new DateOnly(9999, 12, 31);

        var quantos = 0;

        while (true)
        {
            var minha = Data.De(doNet.Year, doNet.Month, doNet.Day);

            // 1. o número de dias desde a época bate
            Assert.Equal(doNet.DayNumber - Epoca.DayNumber, minha.Dias);

            // 2. a volta devolve exatamente o mesmo ano, mês e dia
            var (ano, mes, dia) = minha.Civil();

            Assert.Equal(doNet.Year, ano);
            Assert.Equal(doNet.Month, mes);
            Assert.Equal(doNet.Day, dia);

            // 3. o dia da semana bate -- com o deslocamento do ISO, em que
            //    segunda é 1 e o .NET põe domingo em 0
            var esperado = doNet.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)doNet.DayOfWeek;

            Assert.Equal(esperado, minha.DiaDaSemana());

            // 4. o dia do ano bate
            Assert.Equal(doNet.DayOfYear, minha.DiaDoAno());

            quantos++;

            if (doNet == ultimo)
            {
                break;
            }

            doNet = doNet.AddDays(1);
        }

        // O número é a prova de que o laço percorreu o domínio inteiro, e não um
        // pedaço dele: 9999 anos de 365 dias mais 2.424 dias bissextos.
        Assert.Equal(3_652_059, quantos);
    }

    [Fact(DisplayName = "todos os dias: o ISO 8601 ida e volta, nas três formas")]
    public void TodosOsDiasNoIso()
    {
        var doNet = new DateOnly(1, 1, 1);
        var ultimo = new DateOnly(9999, 12, 31);

        while (true)
        {
            var data = Data.De(doNet.Year, doNet.Month, doNet.Day);

            // A forma por calendário tem de bater com o "o" do .NET, que é o
            // formato ISO 8601 de data pura -- e ele é uma norma, não uma
            // escolha de estilo.
            Assert.Equal(doNet.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                data.ToString());

            // E as três formas, lidas de volta, têm de dar a MESMA data. É a
            // propriedade que importa: são três grafias de um dia só.
            Assert.Equal(data, Iso.Ler(Iso.Escrever(data)));
            Assert.Equal(data, Iso.Ler(Iso.EscreverCompacto(data)));
            Assert.Equal(data, Iso.Ler(Iso.EscreverOrdinal(data)));
            Assert.Equal(data, Iso.Ler(Iso.EscreverSemana(data)));

            if (doNet == ultimo)
            {
                break;
            }

            doNet = doNet.AddDays(1);
        }
    }

    [Fact(DisplayName = "todos os dias: a semana ISO contra o System.Globalization.ISOWeek")]
    public void TodasAsSemanas()
    {
        // O ISOWeek do .NET é a implementação de referência da norma 8601 na
        // plataforma, e é um juiz melhor do que uma tabela de exemplos: ele
        // responde para QUALQUER data, e é aqui que se descobre que o ano da
        // semana não é o ano da data.
        var doNet = new DateTime(1, 1, 1);
        var ultimo = new DateTime(9999, 12, 31);

        var vezesQueOAnoDivergiu = 0;

        while (true)
        {
            var data = Data.De(doNet.Year, doNet.Month, doNet.Day);

            var semana = Semana.De(data);

            Assert.Equal(ISOWeek.GetYear(doNet), semana.Ano);
            Assert.Equal(ISOWeek.GetWeekOfYear(doNet), semana.Numero);

            // E o caminho de volta: a semana designa a data de onde veio.
            Assert.Equal(data, semana.ParaData());

            if (semana.Ano != doNet.Year)
            {
                vezesQueOAnoDivergiu++;
            }

            if (doNet == ultimo)
            {
                break;
            }

            doNet = doNet.AddDays(1);
        }

        // Isto não é decoração. O ano da semana difere do ano da data em alguns
        // dias de quase todo dezembro e janeiro, e é o defeito mais comum em
        // relatório semanal: agrupar por (ano da data, número da semana) parte
        // uma semana em duas, uma vez por ano, em produção.
        Assert.True(vezesQueOAnoDivergiu > 10_000,
            $"o ano da semana divergiu do ano da data em {vezesQueOAnoDivergiu} dias");
    }

    [Fact(DisplayName = "todos os anos: quantas semanas o ano ISO tem")]
    public void QuantasSemanasPorAno()
    {
        var comCinquentaETres = 0;

        for (var ano = 1; ano <= 9999; ano++)
        {
            var esperado = ISOWeek.GetWeeksInYear(ano);

            Assert.Equal(esperado, Semana.QuantasSemanas(ano));

            if (esperado == 53)
            {
                comCinquentaETres++;
            }
        }

        // 71 anos em cada 400 têm 53 semanas: 400 anos são 146.097 dias, que dão
        // 20.871 semanas exatas, e 400 × 52 são 20.800 -- sobram 71.
        //
        // De 1 a 9999 são 1.775, e não 24 × 71 + algo redondo, porque 9999 não é
        // um número inteiro de ciclos de 400. Escrevi 1.774 aqui de cabeça e o
        // teste reprovou; o erro era meu, não do código -- a comparação ano a ano
        // com o ISOWeek passou nos 9.999.
        Assert.Equal(1_775, comCinquentaETres);
    }

    [Fact(DisplayName = "todos os anos: bissexto")]
    public void TodosOsAnosBissextos()
    {
        var quantos = 0;

        for (var ano = 1; ano <= 9999; ano++)
        {
            Assert.Equal(DateTime.IsLeapYear(ano), Data.Bissexto(ano));

            if (Data.Bissexto(ano))
            {
                quantos++;
            }

            for (var mes = 1; mes <= 12; mes++)
            {
                Assert.Equal(DateTime.DaysInMonth(ano, mes), Data.DiasNoMes(ano, mes));
            }
        }

        // 97 bissextos a cada 400 anos. De 1 a 9999 são 2.424 -- e não 2.425,
        // pelo mesmo motivo do teste acima: o intervalo termina em 9999 e o ano
        // 10000, que é bissexto, fica de fora. Outro número que eu errei de
        // cabeça e o laço corrigiu.
        Assert.Equal(2_424, quantos);

        // Os três degraus da regra, nomeados.
        Assert.True(Data.Bissexto(2024));    // de quatro em quatro
        Assert.False(Data.Bissexto(1900));   // exceto de cem em cem
        Assert.True(Data.Bissexto(2000));    // exceto de quatrocentos em quatrocentos
    }
}
