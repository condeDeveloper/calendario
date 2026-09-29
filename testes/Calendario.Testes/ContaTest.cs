using Xunit;

namespace Conde.Calendario.Testes;

/// <summary>
/// A aritmética de datas — contra o <c>DateTime</c>, e contra si mesma.
/// </summary>
/// <remarks>
/// <para>
/// Somar dias é trivial: é somar um inteiro. Somar <b>meses</b> não é, e é onde
/// mora a única decisão de projeto do repositório inteiro — o que fazer com 31
/// de janeiro mais um mês.
/// </para>
/// </remarks>
public class ContaTest
{
    [Fact(DisplayName = "somar meses, contra o DateTime.AddMonths, em 800 mil casos")]
    public void SomarMeses()
    {
        // Duzentos anos de datas vezes onze deslocamentos escolhidos: os que
        // atravessam o fim de mês, o fim de ano e o século.
        int[] deslocamentos = [-1200, -13, -12, -11, -1, 0, 1, 11, 12, 13, 1200];

        var doNet = new DateTime(1900, 1, 1);
        var ultimo = new DateTime(2100, 12, 31);

        var quantos = 0;

        while (doNet <= ultimo)
        {
            var minha = Data.De(doNet.Year, doNet.Month, doNet.Day);

            foreach (var quantosMeses in deslocamentos)
            {
                var esperado = doNet.AddMonths(quantosMeses);
                var achado = minha.MaisMeses(quantosMeses);

                Assert.Equal(esperado.Year, achado.Ano);
                Assert.Equal(esperado.Month, achado.Mes);
                Assert.Equal(esperado.Day, achado.Dia);

                quantos++;
            }

            doNet = doNet.AddDays(1);
        }

        Assert.Equal(73_414 * deslocamentos.Length, quantos);
    }

    [Fact(DisplayName = "somar anos, contra o DateTime.AddYears — e o 29 de fevereiro")]
    public void SomarAnos()
    {
        var doNet = new DateTime(1800, 1, 1);
        var ultimo = new DateTime(2200, 12, 31);

        while (doNet <= ultimo)
        {
            var minha = Data.De(doNet.Year, doNet.Month, doNet.Day);

            foreach (var anos in (int[])[-100, -4, -1, 0, 1, 4, 100])
            {
                var esperado = doNet.AddYears(anos);
                var achado = minha.MaisAnos(anos);

                Assert.Equal(esperado.Year, achado.Ano);
                Assert.Equal(esperado.Month, achado.Mes);
                Assert.Equal(esperado.Day, achado.Dia);
            }

            doNet = doNet.AddDays(1);
        }

        // O único dia em que somar um ano não devolve o mesmo dia do mês.
        Assert.Equal(Data.De(2025, 2, 28), Data.De(2024, 2, 29).MaisAnos(1));
        Assert.Equal(Data.De(2028, 2, 29), Data.De(2024, 2, 29).MaisAnos(4));

        // E 1900 não foi bissexto -- o degrau dos cem anos aparece aqui.
        Assert.Equal(Data.De(1900, 2, 28), Data.De(1896, 2, 29).MaisAnos(4));
    }

    [Fact(DisplayName = "somar meses NÃO é reversível, e isso é o preço de fevereiro")]
    public void SomarMesesNaoEhReversivel()
    {
        // É a consequência de grudar no último dia do mês, e vale estar num
        // teste em vez de numa nota de rodapé: quem some meses para frente e
        // para trás esperando voltar ao mesmo lugar vai errar, e o erro é de um
        // a três dias, que é exatamente o tamanho que passa despercebido numa
        // conferência por cima.
        var janeiro31 = Data.De(2026, 1, 31);

        Assert.Equal(Data.De(2026, 2, 28), janeiro31.MaisMeses(1));
        Assert.Equal(Data.De(2026, 1, 28), janeiro31.MaisMeses(1).MaisMeses(-1));

        Assert.NotEqual(janeiro31, janeiro31.MaisMeses(1).MaisMeses(-1));

        // Somar 12 meses de uma vez não é o mesmo que somar 1 doze vezes.
        var deUmaVez = janeiro31.MaisMeses(12);

        var deUmEmUm = janeiro31;

        for (var i = 0; i < 12; i++)
        {
            deUmEmUm = deUmEmUm.MaisMeses(1);
        }

        Assert.Equal(Data.De(2027, 1, 31), deUmaVez);
        Assert.Equal(Data.De(2027, 1, 28), deUmEmUm);

        // Somar DIAS, por outro lado, sempre volta. É a única unidade em que a
        // aritmética de datas é um grupo de verdade.
        var sorteio = new Random(29);

        for (var i = 0; i < 100_000; i++)
        {
            var data = Data.DeDias(sorteio.Next(-500_000, 500_000));
            var quantos = sorteio.Next(-100_000, 100_000);

            Assert.Equal(data, data.MaisDias(quantos).MaisDias(-quantos));
        }
    }

    [Fact(DisplayName = "quantos meses inteiros — a conta que uma assinatura precisa")]
    public void MesesInteiros()
    {
        // "Um mês inteiro" não é a subtração dos campos de mês: de 15 de janeiro
        // a 14 de fevereiro não se passou um mês, e a resposta é zero.
        Assert.Equal(1, Data.De(2026, 1, 15).MesesAte(Data.De(2026, 2, 15)));
        Assert.Equal(0, Data.De(2026, 1, 15).MesesAte(Data.De(2026, 2, 14)));
        Assert.Equal(1, Data.De(2026, 1, 31).MesesAte(Data.De(2026, 3, 1)));

        // 31 de janeiro a 28 de fevereiro dá UM mês, e não zero. A razão está no
        // teste de coerência logo abaixo: 31 de janeiro mais um mês É 28 de
        // fevereiro, então dizer que não se passou um mês faria as duas contas
        // discordarem sobre o mesmo dia.
        Assert.Equal(1, Data.De(2026, 1, 31).MesesAte(Data.De(2026, 2, 28)));
        Assert.Equal(0, Data.De(2026, 1, 31).MesesAte(Data.De(2026, 2, 27)));

        // Para trás, o sinal se inverte e a regra também.
        Assert.Equal(-1, Data.De(2026, 3, 1).MesesAte(Data.De(2026, 1, 31)));
        Assert.Equal(0, Data.De(2026, 2, 28).MesesAte(Data.De(2026, 1, 31)));

        // A idade de alguém, que é a mesma conta em anos.
        var nascimento = Data.De(1990, 6, 15);

        Assert.Equal(36, nascimento.AnosAte(Data.De(2026, 6, 15)));
        Assert.Equal(35, nascimento.AnosAte(Data.De(2026, 6, 14)));

        // E o caso que todo sistema de cadastro erra: quem nasceu em 29 de
        // fevereiro faz aniversário em 28 de fevereiro nos anos comuns.
        var bissexto = Data.De(2000, 2, 29);

        Assert.Equal(25, bissexto.AnosAte(Data.De(2025, 3, 1)));
        Assert.Equal(25, bissexto.AnosAte(Data.De(2025, 2, 28)));
        Assert.Equal(24, bissexto.AnosAte(Data.De(2025, 2, 27)));
    }

    [Fact(DisplayName = "somar meses e contar meses TÊM de concordar")]
    public void ASomaEAContagemConcordam()
    {
        // Este teste é o que corrigiu a contagem de meses, e o defeito que ele
        // achou é do tipo que só aparece em fatura.
        //
        // A versão ingênua de MesesAte comparava o dia de destino com o dia de
        // origem cru. Com isso, quem nasceu em 29 de fevereiro de 2000 tinha 24
        // anos em 28 de fevereiro de 2025 -- enquanto MaisAnos(25) devolvia
        // exatamente aquele dia como o aniversário de 25. As duas contas
        // discordavam, e um sistema que usasse as duas cobraria no dia errado.
        //
        // A regra que as concilia: a.MesesAte(b) >= n  <=>  a.MaisMeses(n) <= b.
        // Ela vale aqui para todo dia de duzentos anos e sete deslocamentos --
        // meio milhão de pares.
        var data = Data.De(1900, 1, 1);

        while (data < Data.De(2100, 1, 1))
        {
            foreach (var n in (int[])[0, 1, 2, 11, 12, 13, 24])
            {
                var depois = data.MaisMeses(n);

                Assert.True(data.MesesAte(depois) >= n,
                    $"{data} mais {n} meses dá {depois}, e a contagem de volta "
                    + $"deu {data.MesesAte(depois)}");

                // E um dia antes do alvo, ainda NÃO se passaram n meses.
                if (n > 0)
                {
                    Assert.True(data.MesesAte(depois.MaisDias(-1)) < n,
                        $"de {data} até {depois.MaisDias(-1)} não podem ter "
                        + $"passado {n} meses");
                }
            }

            data = data.MaisDias(1);
        }
    }

    [Fact(DisplayName = "o próximo e o anterior dia da semana")]
    public void ProximoEAnterior()
    {
        // 2026-09-29 é uma terça-feira.
        var terca = Data.De(2026, 9, 29);

        Assert.Equal(2, terca.DiaDaSemana());

        Assert.Equal(Data.De(2026, 9, 30), terca.Proximo(3));   // a quarta seguinte
        Assert.Equal(Data.De(2026, 10, 5), terca.Proximo(1));   // a segunda seguinte

        // "A próxima terça", dita numa terça, é daqui a SETE dias -- e não hoje.
        // É a convenção que a língua usa, e a que um agendamento precisa.
        Assert.Equal(Data.De(2026, 10, 6), terca.Proximo(2));

        Assert.Equal(Data.De(2026, 9, 28), terca.Anterior(1));
        Assert.Equal(Data.De(2026, 9, 22), terca.Anterior(2));

        // A propriedade, em todo dia de vinte anos: o próximo alvo está entre 1
        // e 7 dias à frente e tem o dia da semana pedido.
        var data = Data.De(2000, 1, 1);

        while (data < Data.De(2020, 1, 1))
        {
            for (var alvo = 1; alvo <= 7; alvo++)
            {
                var proximo = data.Proximo(alvo);
                var anterior = data.Anterior(alvo);

                Assert.Equal(alvo, proximo.DiaDaSemana());
                Assert.Equal(alvo, anterior.DiaDaSemana());

                Assert.InRange(data.Ate(proximo), 1, 7);
                Assert.InRange(anterior.Ate(data), 1, 7);
            }

            data = data.MaisDias(1);
        }
    }

    [Fact(DisplayName = "o primeiro e o último dia do mês")]
    public void PontasDoMes()
    {
        var data = Data.De(1900, 1, 1);

        while (data < Data.De(2100, 1, 1))
        {
            var primeiro = data.PrimeiroDoMes();
            var ultimo = data.UltimoDoMes();

            Assert.Equal(1, primeiro.Dia);
            Assert.Equal(data.Mes, primeiro.Mes);
            Assert.Equal(data.Ano, primeiro.Ano);

            Assert.Equal(Data.DiasNoMes(data.Ano, data.Mes), ultimo.Dia);

            // O dia seguinte ao último é o primeiro do mês seguinte -- que é a
            // definição, dita de outro jeito.
            Assert.Equal(1, ultimo.MaisDias(1).Dia);

            data = data.MaisDias(1);
        }
    }

    [Fact(DisplayName = "comparar e subtrair são as duas operações baratas")]
    public void CompararESubtrair()
    {
        var sorteio = new Random(7);

        for (var i = 0; i < 200_000; i++)
        {
            var um = Data.DeDias(sorteio.Next(-700_000, 2_900_000));
            var outro = Data.DeDias(sorteio.Next(-700_000, 2_900_000));

            var doNetUm = new DateOnly(um.Ano, um.Mes, um.Dia);
            var doNetOutro = new DateOnly(outro.Ano, outro.Mes, outro.Dia);

            Assert.Equal(doNetUm.CompareTo(doNetOutro), Math.Sign(um.CompareTo(outro)));
            Assert.Equal(doNetUm == doNetOutro, um == outro);
            Assert.Equal(doNetUm < doNetOutro, um < outro);
            Assert.Equal(doNetOutro.DayNumber - doNetUm.DayNumber, um.Ate(outro));
            Assert.Equal(um.Ate(outro), outro - um);
        }
    }

    [Fact(DisplayName = "as bordas do intervalo representável")]
    public void AsBordas()
    {
        Assert.Equal(Data.De(1, 1, 1), Data.Menor);
        Assert.Equal(Data.De(9999, 12, 31), Data.Maior);

        Assert.Equal("0001-01-01", Data.Menor.ToString());
        Assert.Equal("9999-12-31", Data.Maior.ToString());

        // 0001-01-01 foi uma segunda-feira no gregoriano proléptico. Não é um
        // fato histórico -- é uma consequência de estender a regra para trás, e
        // o .NET concorda porque faz a mesma extensão.
        Assert.Equal(1, Data.Menor.DiaDaSemana());
        Assert.Equal((int)DayOfWeek.Monday, (int)new DateOnly(1, 1, 1).DayOfWeek);

        Assert.Throws<ArgumentOutOfRangeException>(() => Data.Menor.MaisDias(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Data.Maior.MaisDias(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Data.Menor.MaisMeses(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Data.Maior.MaisAnos(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => Data.De(0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Data.De(10_000, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Data.De(2026, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Data.De(2026, 13, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Data.De(2026, 2, 29));
        Assert.Throws<ArgumentOutOfRangeException>(() => Data.De(2026, 4, 31));

        Assert.True(Data.TentarDe(2024, 2, 29, out _));
        Assert.False(Data.TentarDe(2026, 2, 29, out _));

        // O total de dias representáveis, que é o tamanho do domínio inteiro.
        Assert.Equal(3_652_058, Data.Menor.Ate(Data.Maior));
    }
}
