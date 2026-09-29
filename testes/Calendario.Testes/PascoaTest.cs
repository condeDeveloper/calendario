using Xunit;

namespace Conde.Calendario.Testes;

/// <summary>
/// A Páscoa — contra uma tabela publicada, e contra a própria regra.
/// </summary>
/// <remarks>
/// <para>
/// Aqui não há implementação do .NET para comparar: a plataforma não calcula
/// Páscoa. Então o juiz é de outro tipo — a <b>tabela publicada</b> de datas,
/// que é o que as igrejas e os almanaques imprimem, mais as propriedades que a
/// regra de Niceia <b>obriga</b> e que dá para conferir em todos os 9.999 anos.
/// </para>
/// <para>
/// A segunda metade é a mais forte. Uma tabela de quarenta anos é uma amostra;
/// "a Páscoa é sempre um domingo" e "cai sempre entre 22 de março e 25 de abril"
/// são deduções da regra, e valem para todo ano. Se o algoritmo produzisse uma
/// quarta-feira em algum ano de quatro algarismos, esses testes o achariam.
/// </para>
/// </remarks>
public class PascoaTest
{
    /// <summary>
    /// A tabela — quarenta e um anos seguidos, mais oito âncoras históricas.
    /// </summary>
    /// <remarks>
    /// Os anos de 2000 a 2040 estão em qualquer almanaque ou calendário
    /// litúrgico; as âncoras foram escolhidas por serem extremos ou anos que
    /// aparecem em tabela: 1886 e 2038 caem no dia <b>mais tarde</b> possível,
    /// 25 de abril, e 1818 e 2285 no <b>mais cedo</b>, 22 de março.
    /// </remarks>
    public static TheoryData<int, int, int> ATabela() => new()
    {
        { 2000, 4, 23 }, { 2001, 4, 15 }, { 2002, 3, 31 }, { 2003, 4, 20 },
        { 2004, 4, 11 }, { 2005, 3, 27 }, { 2006, 4, 16 }, { 2007, 4, 8 },
        { 2008, 3, 23 }, { 2009, 4, 12 }, { 2010, 4, 4 }, { 2011, 4, 24 },
        { 2012, 4, 8 }, { 2013, 3, 31 }, { 2014, 4, 20 }, { 2015, 4, 5 },
        { 2016, 3, 27 }, { 2017, 4, 16 }, { 2018, 4, 1 }, { 2019, 4, 21 },
        { 2020, 4, 12 }, { 2021, 4, 4 }, { 2022, 4, 17 }, { 2023, 4, 9 },
        { 2024, 3, 31 }, { 2025, 4, 20 }, { 2026, 4, 5 }, { 2027, 3, 28 },
        { 2028, 4, 16 }, { 2029, 4, 1 }, { 2030, 4, 21 }, { 2031, 4, 13 },
        { 2032, 3, 28 }, { 2033, 4, 17 }, { 2034, 4, 9 }, { 2035, 3, 25 },
        { 2036, 4, 13 }, { 2037, 4, 5 }, { 2038, 4, 25 }, { 2039, 4, 10 },
        { 2040, 4, 1 },

        { 1818, 3, 22 },   // o mais cedo possível
        { 1886, 4, 25 },   // o mais tarde possível
        { 1900, 4, 15 },
        { 1918, 3, 31 },
        { 1943, 4, 25 },
        { 1954, 4, 18 },
        { 1981, 4, 19 },
        { 2285, 3, 22 },   // o mais cedo outra vez
    };

    [Theory(DisplayName = "a tabela publicada")]
    [MemberData(nameof(ATabela))]
    public void ATabelaBate(int ano, int mes, int dia)
    {
        Assert.Equal(Data.De(ano, mes, dia), Pascoa.De(ano));
    }

    [Fact(DisplayName = "em TODOS os 9.999 anos: é domingo, e cai entre 22/3 e 25/4")]
    public void AsPropriedadesDaRegra()
    {
        // As duas propriedades são consequências da regra de Niceia, e não
        // observações: "o primeiro domingo depois da lua cheia pascal" é domingo
        // por definição, e a lua eclesiástica cai entre 21 de março e 18 de
        // abril -- o domingo seguinte, portanto, entre 22 de março e 25 de
        // abril.
        var maisCedo = Data.De(9999, 12, 31);
        var maisTarde = Data.De(1, 1, 1);

        for (var ano = 1; ano <= 9999; ano++)
        {
            var pascoa = Pascoa.De(ano);

            Assert.Equal(7, pascoa.DiaDaSemana());

            var (_, mes, dia) = pascoa.Civil();

            Assert.True(
                (mes == 3 && dia >= 22) || (mes == 4 && dia <= 25),
                $"a Páscoa de {ano} caiu em {pascoa}, fora de 22/3 .. 25/4");

            var doAno = Data.De(2000, mes, dia);

            if (doAno < maisCedo)
            {
                maisCedo = doAno;
            }

            if (doAno > maisTarde)
            {
                maisTarde = doAno;
            }
        }

        // E os dois extremos são de fato alcançados: a janela de 35 dias não
        // sobra em nenhuma ponta.
        Assert.Equal(Data.De(2000, 3, 22), maisCedo);
        Assert.Equal(Data.De(2000, 4, 25), maisTarde);
    }

    [Fact(DisplayName = "as datas móveis pendem da Páscoa, na distância certa")]
    public void AsMoveis()
    {
        for (var ano = 1600; ano <= 2400; ano++)
        {
            var pascoa = Pascoa.De(ano);

            // A Sexta-feira Santa é a sexta anterior: dois dias antes.
            Assert.Equal(5, Pascoa.SextaSanta(ano).DiaDaSemana());
            Assert.Equal(2, Pascoa.SextaSanta(ano).Ate(pascoa));

            // O carnaval é a terça anterior à quarta de cinzas, 47 dias antes.
            Assert.Equal(2, Pascoa.Carnaval(ano).DiaDaSemana());
            Assert.Equal(47, Pascoa.Carnaval(ano).Ate(pascoa));

            // A quarta-feira de cinzas abre a quaresma: 40 dias de jejum mais os
            // seis domingos, que não contam -- 46 dias antes.
            Assert.Equal(3, Pascoa.Cinzas(ano).DiaDaSemana());
            Assert.Equal(46, Pascoa.Cinzas(ano).Ate(pascoa));

            // Corpus Christi é sempre numa quinta, 60 dias depois.
            Assert.Equal(4, Pascoa.CorpusChristi(ano).DiaDaSemana());
            Assert.Equal(60, pascoa.Ate(Pascoa.CorpusChristi(ano)));
        }
    }

    [Fact(DisplayName = "os feriados do Brasil saem em ordem e sem repetição")]
    public void OsFeriados()
    {
        for (var ano = 1950; ano <= 2100; ano++)
        {
            var feriados = Pascoa.FeriadosDoBrasil(ano);

            Assert.Equal(ano >= 2024 ? 14 : 13, feriados.Count);

            // Em ordem -- mas NÃO estritamente, e isto o teste me ensinou. Eu
            // tinha escrito `<` e ele reprovou em 1957: a Páscoa caiu em 21 de
            // abril, que é o dia de Tiradentes. Dois feriados no mesmo dia é um
            // caso real, acontece de tempos em tempos, e quem escreve um
            // calculador de dias úteis precisa contar o dia uma vez só.
            for (var i = 1; i < feriados.Count; i++)
            {
                Assert.True(feriados[i - 1].Quando <= feriados[i].Quando,
                    $"{ano}: {feriados[i - 1].Nome} e {feriados[i].Nome} fora de ordem");
            }

            // Todos caem no próprio ano -- incluindo o carnaval, que em anos de
            // Páscoa muito cedo vai para o começo de fevereiro.
            foreach (var feriado in feriados)
            {
                Assert.Equal(ano, feriado.Quando.Ano);
            }

            // Carnaval e Corpus Christi são ponto facultativo, e não feriado
            // nacional por lei federal. A diferença aparece em folha.
            Assert.False(feriados.Single(f => f.Nome == "Carnaval").DeLei);
            Assert.False(feriados.Single(f => f.Nome == "Corpus Christi").DeLei);
            Assert.True(feriados.Single(f => f.Nome == "Natal").DeLei);
        }

        // A consciência negra virou feriado nacional pela Lei 14.759, de 2023.
        Assert.DoesNotContain(
            Pascoa.FeriadosDoBrasil(2023), f => f.Nome == "Consciência Negra");

        Assert.Contains(
            Pascoa.FeriadosDoBrasil(2024), f => f.Nome == "Consciência Negra");

        // E o caso de 1957, nomeado: a Páscoa em cima do Tiradentes.
        Assert.Equal(Data.De(1957, 4, 21), Pascoa.De(1957));

        var em1957 = Pascoa.FeriadosDoBrasil(1957);

        Assert.Equal(2, em1957.Count(f => f.Quando == Data.De(1957, 4, 21)));

        // São dias distintos do ano, e são 13 feriados; mas o número de DIAS
        // que eles ocupam é 12.
        Assert.Equal(12, em1957.Select(f => f.Quando).Distinct().Count());
    }

    [Fact(DisplayName = "a distribuição é torta, e o mais raro é o dia mais CEDO")]
    public void ADistribuicao()
    {
        // As 35 datas possíveis não são igualmente prováveis, e a diferença é
        // grande: as do meio da janela acontecem umas quatro vezes mais que as
        // das pontas. É consequência de a lua eclesiástica ser uma tabela e não
        // um sorteio.
        var quantas = new Dictionary<string, int>();

        for (var ano = 1; ano <= 9999; ano++)
        {
            var (_, mes, dia) = Pascoa.De(ano).Civil();

            var chave = $"{mes:00}-{dia:00}";

            quantas[chave] = quantas.GetValueOrDefault(chave) + 1;
        }

        Assert.Equal(35, quantas.Count);

        var maisRaro = quantas.MinBy(par => par.Value);
        var maisComum = quantas.MaxBy(par => par.Value);

        // O mais raro é 22 de MARÇO, e eu tinha escrito 25 de abril aqui. O
        // teste reprovou e o erro era meu: as duas pontas da janela são raras,
        // mas não igualmente. Para a Páscoa cair em 22 de março é preciso que a
        // lua eclesiástica caia exatamente em 21 de março E que 21 de março seja
        // um sábado -- duas condições. Para cair em 25 de abril basta que a lua
        // caia em 18 de abril e o domingo seguinte seja o dia 25, o que a
        // aritmética da epacta permite mais vezes.
        //
        // Medido em 9.999 anos: 22 de março sai 55 vezes, 25 de abril 77, e o
        // mais comum -- 19 de abril -- sai 372. Quase sete vezes mais que o mais
        // raro.
        Assert.Equal("03-22", maisRaro.Key);

        Assert.True(maisComum.Value > maisRaro.Value * 3,
            $"o mais comum ({maisComum.Key}, {maisComum.Value}) devia ser bem mais "
            + $"frequente que o mais raro ({maisRaro.Key}, {maisRaro.Value})");
    }
}
