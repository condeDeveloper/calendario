using System.Diagnostics;
using System.Text;
using Conde.Calendario;

namespace Conde.Calendario.Ferramentas.Medidor;

/// <summary>
/// A ferramenta — que mostra o calendário e registra os números.
/// </summary>
/// <remarks>
/// Tudo que depende de relógio mora aqui, e não na suíte de testes. Uma
/// afirmação de tempo falha sem que nada tenha piorado — basta a máquina do CI
/// estar ocupada — e o efeito disso é ensinar quem trabalha no projeto a ignorar
/// teste vermelho. Esta ferramenta imprime e sai com zero, sempre.
/// </remarks>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        // Procura o comando entre TODOS os argumentos. O `dotnet run` nem sempre
        // entrega só o que vem depois do `--`, e um `argumentos[0]` cru já
        // derrubou o CI de outro projeto daqui com o código todo funcionando.
        var comandos = new[] { "mes", "ano", "feriados", "pascoa", "semanas", "ciclo", "velocidade" };

        var onde = Array.FindIndex(argumentos, a => comandos.Contains(a));

        if (onde < 0)
        {
            Console.WriteLine(Ajuda);

            return 0;
        }

        var resto = argumentos[onde..];

        var numeros = resto.Skip(1)
            .Select(a => int.TryParse(a, out var n) ? n : (int?)null)
            .Where(n => n is not null)
            .Select(n => n!.Value)
            .ToArray();

        switch (resto[0])
        {
            case "mes":
                Mes(numeros.Length > 0 ? numeros[0] : 2026,
                    numeros.Length > 1 ? numeros[1] : 9);
                break;

            case "ano":
                Ano(numeros.Length > 0 ? numeros[0] : 2026);
                break;

            case "feriados":
                Feriados(numeros.Length > 0 ? numeros[0] : 2026);
                break;

            case "pascoa":
                APascoa();
                break;

            case "semanas":
                Semanas();
                break;

            case "ciclo":
                Ciclo();
                break;

            default:
                Velocidade();
                break;
        }

        return 0;
    }

    /// <summary>Um mês, desenhado — a prova visual de que as contas fecham.</summary>
    private static void Mes(int ano, int mes)
    {
        var primeiro = Data.De(ano, mes, 1);

        var titulo = $"{Iso.NomeDoMes(mes)} de {ano}";

        Console.WriteLine(titulo.PadLeft((20 + titulo.Length) / 2));
        Console.WriteLine();
        Console.WriteLine("seg ter qua qui sex sáb dom");

        var linha = new StringBuilder();

        // O primeiro dia da semana do mês decide o recuo.
        linha.Append(new string(' ', (primeiro.DiaDaSemana() - 1) * 4));

        for (var dia = 1; dia <= Data.DiasNoMes(ano, mes); dia++)
        {
            var data = Data.De(ano, mes, dia);

            linha.Append($"{dia,3} ");

            if (data.DiaDaSemana() == 7)
            {
                Console.WriteLine(linha.ToString().TrimEnd());

                linha.Clear();
            }
        }

        if (linha.Length > 0)
        {
            Console.WriteLine(linha.ToString().TrimEnd());
        }

        Console.WriteLine();
        Console.WriteLine($"  {Data.DiasNoMes(ano, mes)} dias, de {primeiro} a {primeiro.UltimoDoMes()}");
        Console.WriteLine($"  semanas ISO de {Semana.De(primeiro).Numero} a "
            + $"{Semana.De(primeiro.UltimoDoMes()).Numero}");
    }

    /// <summary>O ano inteiro, resumido.</summary>
    private static void Ano(int ano)
    {
        Console.WriteLine($"{ano} — {(Data.Bissexto(ano) ? "bissexto" : "comum")}, "
            + $"{Data.DiasNoAno(ano)} dias, {Semana.QuantasSemanas(ano)} semanas ISO");
        Console.WriteLine();
        Console.WriteLine("  mês          dias   começa em      termina em     semanas ISO");
        Console.WriteLine("  " + new string('-', 64));

        for (var mes = 1; mes <= 12; mes++)
        {
            var primeiro = Data.De(ano, mes, 1);
            var ultimo = primeiro.UltimoDoMes();

            Console.WriteLine(
                $"  {Iso.NomeDoMes(mes),-12} {Data.DiasNoMes(ano, mes),4}   "
                + $"{Iso.NomeDoDia(primeiro.DiaDaSemana()),-14} "
                + $"{Iso.NomeDoDia(ultimo.DiaDaSemana()),-14} "
                + $"{Semana.De(primeiro).Numero,2} a {Semana.De(ultimo).Numero,2}");
        }

        Console.WriteLine();

        var pascoa = Pascoa.De(ano);

        Console.WriteLine($"  Páscoa em {pascoa} ({Iso.PorExtenso(pascoa)})");
    }

    /// <summary>Os feriados do ano.</summary>
    private static void Feriados(int ano)
    {
        Console.WriteLine($"Feriados nacionais de {ano}");
        Console.WriteLine();
        Console.WriteLine("  data         dia da semana    feriado                       ");
        Console.WriteLine("  " + new string('-', 68));

        var emDiaUtil = 0;

        foreach (var feriado in Pascoa.FeriadosDoBrasil(ano))
        {
            var dia = feriado.Quando.DiaDaSemana();

            Console.WriteLine(
                $"  {feriado.Quando}   {Iso.NomeDoDia(dia),-16} {feriado.Nome,-30}"
                + (feriado.DeLei ? string.Empty : "  (ponto facultativo)"));

            if (dia <= 5 && feriado.DeLei)
            {
                emDiaUtil++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"  {emDiaUtil} dos feriados de lei caem de segunda a sexta.");
        Console.WriteLine("  O resto cai no fim de semana e não vira folga -- é o número que");
        Console.WriteLine("  mais varia de ano para ano, e o que uma empresa quer saber.");

        Console.WriteLine();
        Console.WriteLine("  Nos próximos vinte anos:");
        Console.WriteLine();
        Console.WriteLine("     ano   feriados de lei em dia útil");
        Console.WriteLine("     " + new string('-', 36));

        for (var outro = ano; outro < ano + 20; outro++)
        {
            var quantos = Pascoa.FeriadosDoBrasil(outro)
                .Count(f => f.DeLei && f.Quando.DiaDaSemana() <= 5);

            Console.WriteLine($"    {outro}   {new string('#', quantos)} {quantos}");
        }
    }

    /// <summary>A distribuição das datas da Páscoa, em 9.999 anos.</summary>
    private static void APascoa()
    {
        Console.WriteLine("Em que dia cai a Páscoa — todos os 9.999 anos representáveis");
        Console.WriteLine();

        var quantas = new Dictionary<(int Mes, int Dia), int>();

        for (var ano = 1; ano <= 9999; ano++)
        {
            var (_, mes, dia) = Pascoa.De(ano).Civil();

            quantas[(mes, dia)] = quantas.GetValueOrDefault((mes, dia)) + 1;
        }

        var maior = quantas.Values.Max();

        foreach (var par in quantas.OrderBy(p => p.Key.Mes).ThenBy(p => p.Key.Dia))
        {
            var barra = new string('#', par.Value * 40 / maior);

            Console.WriteLine($"  {par.Key.Dia,2}/{par.Key.Mes}  {barra,-40} {par.Value,4}");
        }

        Console.WriteLine();
        Console.WriteLine($"  São {quantas.Count} datas possíveis, de 22/3 a 25/4 — e elas NÃO são");
        Console.WriteLine("  igualmente prováveis. O mais comum acontece quase sete vezes mais");
        Console.WriteLine("  que o mais raro, porque a lua da regra é uma tabela e não um sorteio.");
    }

    /// <summary>Os anos de 53 semanas.</summary>
    private static void Semanas()
    {
        Console.WriteLine("Os anos de 53 semanas — os que quebram quem supõe 52");
        Console.WriteLine();

        var comCinquentaETres = new List<int>();

        for (var ano = 2000; ano <= 2100; ano++)
        {
            if (Semana.QuantasSemanas(ano) == 53)
            {
                comCinquentaETres.Add(ano);
            }
        }

        Console.WriteLine("  " + string.Join("  ", comCinquentaETres));
        Console.WriteLine();
        Console.WriteLine($"  {comCinquentaETres.Count} em 101 anos — um a cada cinco ou seis.");
        Console.WriteLine();

        Console.WriteLine("E os dias em que o ano da semana não é o ano da data:");
        Console.WriteLine();
        Console.WriteLine("    data         semana ISO     o ano difere");
        Console.WriteLine("    " + new string('-', 48));

        for (var ano = 2024; ano <= 2032; ano++)
        {
            foreach (var data in (Data[])
                     [
                         Data.De(ano, 1, 1), Data.De(ano, 1, 2), Data.De(ano, 1, 3),
                         Data.De(ano, 12, 29), Data.De(ano, 12, 30), Data.De(ano, 12, 31),
                     ])
            {
                var semana = Semana.De(data);

                if (semana.Ano != data.Ano)
                {
                    Console.WriteLine(
                        $"    {data}   {semana}      {data.Ano} -> {semana.Ano}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("  Agrupar um relatório semanal por (ano da data, número da semana)");
        Console.WriteLine("  parte essas semanas em duas, e o total de uma delas sai pela metade.");
    }

    /// <summary>O ciclo de 400 anos, verificado.</summary>
    private static void Ciclo()
    {
        Console.WriteLine("O calendário se repete inteiro a cada 400 anos");
        Console.WriteLine();
        Console.WriteLine("  400 anos têm 146.097 dias, e 146.097 é divisível por 7:");
        Console.WriteLine($"    146.097 / 7 = {146_097 / 7:N0} semanas exatas");
        Console.WriteLine();

        var iguais = 0;
        var comparados = 0;

        var data = Data.De(1600, 1, 1);

        while (data < Data.De(2000, 1, 1))
        {
            var quatrocentosDepois = data.MaisDias(146_097);

            comparados++;

            if (data.Mes == quatrocentosDepois.Mes
                && data.Dia == quatrocentosDepois.Dia
                && data.DiaDaSemana() == quatrocentosDepois.DiaDaSemana()
                && quatrocentosDepois.Ano == data.Ano + 400)
            {
                iguais++;
            }

            data = data.MaisDias(1);
        }

        Console.WriteLine($"  {iguais:N0} de {comparados:N0} dias entre 1600 e 2000 caem no mesmo");
        Console.WriteLine("  dia do mês E no mesmo dia da semana 400 anos depois.");
        Console.WriteLine();
        Console.WriteLine("  Na prática: um calendário de parede de 2026 serve outra vez em 2426.");
        Console.WriteLine("  É por isso que a conversão de datas cabe em aritmética inteira, sem");
        Console.WriteLine("  tabela nenhuma.");
        Console.WriteLine();

        Console.WriteLine("    ano    1º de janeiro       Páscoa");
        Console.WriteLine("    " + new string('-', 44));

        foreach (var ano in (int[])[1226, 1626, 2026, 2426, 2826])
        {
            Console.WriteLine(
                $"    {ano}   {Iso.NomeDoDia(Data.De(ano, 1, 1).DiaDaSemana()),-16} "
                + $"{Pascoa.De(ano)}");
        }

        Console.WriteLine();
        Console.WriteLine("  A coluna da direita é o que a tabela veio dizer, e ela contradiz o");
        Console.WriteLine("  que quase todo mundo supõe: a PÁSCOA NÃO SE REPETE com o ciclo de");
        Console.WriteLine("  400 anos. O 1º de janeiro cai na mesma quinta-feira nos cinco anos,");
        Console.WriteLine("  e a Páscoa cai em cinco dias diferentes.");
        Console.WriteLine();
        Console.WriteLine("  O motivo é que a Páscoa não depende só do calendário solar. A epacta");
        Console.WriteLine("  -- a idade da lua eclesiástica -- tem a sua própria correção por");
        Console.WriteLine("  século, e o ciclo que fecha as duas coisas ao mesmo tempo é de");
        Console.WriteLine("  5.700.000 anos. Dentro do intervalo representável, ele não fecha");
        Console.WriteLine("  nenhuma vez.");
    }

    /// <summary>A velocidade da conversão, contra o <c>DateOnly</c>.</summary>
    private static void Velocidade()
    {
        Console.WriteLine("A conversão, contra o DateOnly do .NET");
        Console.WriteLine();

        const int quantos = 5_000_000;

        var sorteio = new Random(29);

        var dias = new int[quantos];

        for (var i = 0; i < quantos; i++)
        {
            dias[i] = sorteio.Next(-700_000, 2_900_000);
        }

        var relogio = Stopwatch.StartNew();

        var soma = 0L;

        foreach (var d in dias)
        {
            var (ano, mes, dia) = Data.DeDias(d).Civil();

            soma += ano + mes + dia;
        }

        var meu = relogio.Elapsed.TotalMilliseconds;

        relogio.Restart();

        foreach (var d in dias)
        {
            var data = DateOnly.FromDayNumber(d + 719_162);

            soma += data.Year + data.Month + data.Day;
        }

        var doNet = relogio.Elapsed.TotalMilliseconds;

        Console.WriteLine("  operação                        meu       .NET     razão");
        Console.WriteLine("  " + new string('-', 56));
        Console.WriteLine($"  dias -> ano-mês-dia       {meu,9:F1} ms {doNet,9:F1} ms {meu / doNet,7:F2}x");

        var datas = dias.Select(d => Data.DeDias(d).Civil()).ToArray();

        relogio.Restart();

        foreach (var (ano, mes, dia) in datas)
        {
            soma += Data.De(ano, mes, dia).Dias;
        }

        var meuIda = relogio.Elapsed.TotalMilliseconds;

        relogio.Restart();

        foreach (var (ano, mes, dia) in datas)
        {
            soma += new DateOnly(ano, mes, dia).DayNumber;
        }

        var doNetIda = relogio.Elapsed.TotalMilliseconds;

        Console.WriteLine($"  ano-mês-dia -> dias       {meuIda,9:F1} ms {doNetIda,9:F1} ms "
            + $"{meuIda / doNetIda,7:F2}x");

        Console.WriteLine();
        Console.WriteLine($"  ({quantos:N0} conversões de cada lado; soma de controle {soma})");
        Console.WriteLine();
        Console.WriteLine("  As duas implementações fazem a mesma conta — a de Howard Hinnant —");
        Console.WriteLine("  e o que resta de diferença é o custo da validação, que aqui acontece");
        Console.WriteLine("  em toda construção.");
    }

    private const string Ajuda = """
        calendario -- datas do zero, com o gregoriano proléptico

          medidor mes AAAA MM     um mês desenhado
          medidor ano AAAA        o ano inteiro, mês a mês
          medidor feriados AAAA   os feriados nacionais, e quantos caem em dia útil
          medidor pascoa          em que dia cai a Páscoa, em 9.999 anos
          medidor semanas         os anos de 53 semanas, e o ano ISO que diverge
          medidor ciclo           a repetição de 400 anos, conferida
          medidor velocidade      a conversão, contra o DateOnly
        """;
}
