namespace Conde.Calendario;

/// <summary>
/// Uma data do calendário gregoriano — ano, mês e dia, sem hora e sem fuso.
/// </summary>
/// <remarks>
/// <para>
/// Toda a estrutura guarda <b>um número</b>: quantos dias se passaram desde
/// 1970-01-01. Ano, mês e dia são calculados a partir dele, e o caminho de volta
/// também é uma conta — não há tabela de meses acumulados, nem laço sobre anos,
/// nem nada que dependa do tamanho do intervalo.
/// </para>
/// <para>
/// Guardar um número em vez de três tem uma consequência que vale enunciar:
/// <b>comparar duas datas é comparar dois inteiros</b>, e a diferença entre elas
/// é uma subtração. As duas operações que mais aparecem em código real ficam
/// triviais, e o custo se paga na conversão, que acontece só quando alguém quer
/// ver o ano.
/// </para>
/// <para>
/// O calendário é o <b>gregoriano proléptico</b>: as regras de 1582 aplicadas
/// para trás até o ano 1, inclusive nos séculos em que ninguém as usava. É a
/// mesma escolha do ISO 8601 e do <c>DateTime</c> do .NET, e ela troca fidelidade
/// histórica por uma propriedade que vale mais em software: <b>não há buraco</b>.
/// Entre 1582-10-04 e 1582-10-15 o calendário real pulou dez dias, e uma
/// biblioteca que os respeitasse teria uma data que não existe no meio da reta.
/// </para>
/// </remarks>
public readonly struct Data : IEquatable<Data>, IComparable<Data>
{
    /// <summary>Quantos dias desde 1970-01-01 — o único campo.</summary>
    public int Dias { get; }

    /// <summary>A primeira data representável: 0001-01-01.</summary>
    public static Data Menor => new(-719_162);

    /// <summary>A última data representável: 9999-12-31.</summary>
    public static Data Maior => new(2_932_896);

    private Data(int dias) => Dias = dias;

    /// <summary>Uma data a partir do número de dias desde 1970-01-01.</summary>
    public static Data DeDias(int dias)
    {
        if (dias < Menor.Dias || dias > Maior.Dias)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dias), dias, "a data cairia fora de 0001-01-01 .. 9999-12-31");
        }

        return new Data(dias);
    }

    /// <summary>Uma data a partir de ano, mês e dia.</summary>
    public static Data De(int ano, int mes, int dia)
    {
        if (ano is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(ano), ano, "o ano vai de 1 a 9999");
        }

        if (mes is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(mes), mes, "o mês vai de 1 a 12");
        }

        var quantos = DiasNoMes(ano, mes);

        if (dia < 1 || dia > quantos)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dia), dia, $"{ano}-{mes:00} tem {quantos} dias");
        }

        return new Data(DiasDoCivil(ano, mes, dia));
    }

    /// <summary>Tenta montar a data; devolve falso em vez de lançar.</summary>
    public static bool TentarDe(int ano, int mes, int dia, out Data data)
    {
        if (ano is < 1 or > 9999 || mes is < 1 or > 12
            || dia < 1 || dia > DiasNoMes(ano, mes))
        {
            data = default;

            return false;
        }

        data = new Data(DiasDoCivil(ano, mes, dia));

        return true;
    }

    /// <summary>
    /// De ano-mês-dia para dias desde 1970-01-01 — a conta de Howard Hinnant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// O truque é mudar onde o ano começa. Se março for o mês 1 e fevereiro o
    /// mês 12, o dia bissexto cai no <b>fim</b> do ano em vez do meio — e o
    /// comprimento dos meses vira uma sequência regular, sem o degrau de
    /// fevereiro no caminho.
    /// </para>
    /// <para>
    /// Feita a troca, o dia do ano é <c>(153·m + 2) / 5</c>, e essa fração
    /// reproduz a sequência 31, 30, 31, 30, 31, 31, 30, 31, 30, 31, 31, 28 —
    /// o padrão de meses de 30 e 31 dias é <b>periódico</b>, e a divisão inteira
    /// o alcança sem tabela nenhuma.
    /// </para>
    /// <para>
    /// O resto é o ciclo de <b>400 anos</b>, que tem exatamente 146.097 dias.
    /// Esse número é a razão de o calendário gregoriano ser o que é: 400 anos
    /// têm 146.097 dias e 146.097 é divisível por 7, então o calendário
    /// <b>se repete inteiro</b> a cada 400 anos, dia da semana e tudo. Um
    /// calendário de 2026 serve outra vez em 2426.
    /// </para>
    /// </remarks>
    private static int DiasDoCivil(int ano, int mes, int dia)
    {
        // Março vira o mês 1. Janeiro e fevereiro passam a pertencer ao ano
        // anterior, e é isso que joga o dia bissexto para o fim.
        var y = ano - (mes <= 2 ? 1 : 0);

        var era = (y >= 0 ? y : y - 399) / 400;

        var anoDaEra = y - era * 400;                 // 0 .. 399

        var m = mes + (mes > 2 ? -3 : 9);             // março = 0 .. fevereiro = 11

        var diaDoAno = (153 * m + 2) / 5 + dia - 1;   // 0 .. 365

        var diaDaEra = anoDaEra * 365 + anoDaEra / 4 - anoDaEra / 100 + diaDoAno;

        // 719.468 é a distância entre 0000-03-01 (o começo de uma era) e
        // 1970-01-01.
        return era * 146_097 + diaDaEra - 719_468;
    }

    /// <summary>O ano.</summary>
    public int Ano => Civil().Ano;

    /// <summary>O mês, de 1 a 12.</summary>
    public int Mes => Civil().Mes;

    /// <summary>O dia do mês.</summary>
    public int Dia => Civil().Dia;

    /// <summary>
    /// De dias desde 1970-01-01 para ano-mês-dia — o caminho de volta.
    /// </summary>
    /// <remarks>
    /// A volta desfaz a ida na ordem inversa: acha a era, o ano dentro dela, o
    /// dia dentro do ano, e enfim o mês pela fração <c>(5·d + 2) / 153</c>, que
    /// é a inversa de <c>(153·m + 2) / 5</c>.
    /// </remarks>
    public (int Ano, int Mes, int Dia) Civil()
    {
        var z = Dias + 719_468;

        var era = (z >= 0 ? z : z - 146_096) / 146_097;

        var diaDaEra = z - era * 146_097;             // 0 .. 146.096

        var anoDaEra =
            (diaDaEra - diaDaEra / 1_460 + diaDaEra / 36_524 - diaDaEra / 146_096) / 365;

        var y = anoDaEra + era * 400;

        var diaDoAno = diaDaEra - (365 * anoDaEra + anoDaEra / 4 - anoDaEra / 100);

        var m = (5 * diaDoAno + 2) / 153;             // 0 .. 11, com março = 0

        var dia = diaDoAno - (153 * m + 2) / 5 + 1;

        var mes = m + (m < 10 ? 3 : -9);

        return (y + (mes <= 2 ? 1 : 0), mes, dia);
    }

    /// <summary>O dia da semana — segunda é 1 e domingo é 7, como manda o ISO 8601.</summary>
    /// <remarks>
    /// <para>
    /// Não há conta de Zeller aqui, nem tabela: o dia da semana de uma data
    /// guardada como número de dias é o <b>resto por 7</b>, e mais nada.
    /// </para>
    /// <para>
    /// 1970-01-01 foi uma quinta-feira, o que põe o resto 0 na quinta. O
    /// deslocamento de 3 traz a segunda para o 1. E o <c>+ 7) % 7</c> existe
    /// porque em C# o resto de um número negativo é negativo — para datas
    /// anteriores a 1970 a conta ingênua devolveria dia da semana negativo.
    /// </para>
    /// </remarks>
    public int DiaDaSemana() => (Dias + 3) % 7 < 0
        ? (Dias + 3) % 7 + 7 + 1
        : (Dias + 3) % 7 + 1;

    /// <summary>O dia dentro do ano, de 1 a 366.</summary>
    public int DiaDoAno() => Dias - DiasDoCivil(Ano, 1, 1) + 1;

    /// <summary>O ano é bissexto?</summary>
    /// <remarks>
    /// <para>
    /// A regra tem três degraus, e o terceiro é o que quase todo mundo esquece:
    /// de quatro em quatro anos, <b>exceto</b> de cem em cem, <b>exceto</b> de
    /// quatrocentos em quatrocentos. 1900 não foi bissexto; 2000 foi.
    /// </para>
    /// <para>
    /// O motivo de existir o terceiro degrau é que o ano trópico tem 365,2422
    /// dias. A regra dos quatro dá 365,25 e erra 11 minutos por ano; tirar os
    /// séculos dá 365,24 e passa do ponto; devolver os múltiplos de 400 dá
    /// 365,2425, e o erro que sobra é de <b>um dia a cada 3.200 anos</b>.
    /// </para>
    /// </remarks>
    public static bool Bissexto(int ano) => (ano % 4 == 0 && ano % 100 != 0) || ano % 400 == 0;

    /// <summary>Quantos dias tem o mês.</summary>
    public static int DiasNoMes(int ano, int mes) => mes switch
    {
        2 => Bissexto(ano) ? 29 : 28,
        4 or 6 or 9 or 11 => 30,
        _ => 31,
    };

    /// <summary>Quantos dias tem o ano.</summary>
    public static int DiasNoAno(int ano) => Bissexto(ano) ? 366 : 365;

    /// <summary>Soma dias.</summary>
    public Data MaisDias(int quantos) => DeDias(Dias + quantos);

    /// <summary>
    /// Soma meses — com o ajuste que é a única decisão de projeto aqui.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 31 de janeiro mais um mês é o quê? Não existe 31 de fevereiro, e a
    /// pergunta não tem resposta certa — tem convenções. A daqui é a mesma do
    /// <c>DateTime.AddMonths</c>, do <c>java.time</c> e da maioria: <b>grudar no
    /// último dia do mês</b>, e 31 de janeiro mais um mês é 28 ou 29 de
    /// fevereiro.
    /// </para>
    /// <para>
    /// A consequência de escolher isso é que a soma de meses <b>deixa de ser
    /// reversível</b>: 31 de janeiro mais um mês menos um mês dá 28 de janeiro,
    /// e não 31. Não é defeito — é o preço de fevereiro. Quem precisa de
    /// reversibilidade soma dias, que é a única unidade que sempre volta.
    /// </para>
    /// </remarks>
    public Data MaisMeses(int quantos)
    {
        var (ano, mes, dia) = Civil();

        var total = ano * 12 + (mes - 1) + quantos;

        var novoAno = total / 12;
        var novoMes = total % 12;

        if (novoMes < 0)
        {
            novoMes += 12;
            novoAno--;
        }

        novoMes++;

        if (novoAno is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantos), quantos, "a data cairia fora de 0001-01-01 .. 9999-12-31");
        }

        // Aqui está o grude: o dia vira o menor entre o que era e o último do
        // mês de destino.
        return De(novoAno, novoMes, Math.Min(dia, DiasNoMes(novoAno, novoMes)));
    }

    /// <summary>Soma anos — com o mesmo ajuste, que só aparece em 29 de fevereiro.</summary>
    public Data MaisAnos(int quantos) => MaisMeses(quantos * 12);

    /// <summary>Quantos dias de uma data até a outra.</summary>
    public int Ate(Data outra) => outra.Dias - Dias;

    /// <summary>
    /// Quantos meses <b>inteiros</b> de uma data até a outra.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Inteiros" é a palavra que faz a diferença: de 15 de janeiro a 14 de
    /// fevereiro não se passou um mês inteiro, e a resposta é zero. É a conta
    /// que uma assinatura mensal precisa, e ela não é a subtração dos campos.
    /// </para>
    /// <para>
    /// A comparação é contra o dia <b>grudado</b>, e não contra o dia original —
    /// e isto é uma correção que o teste exigiu. A versão ingênua comparava o dia
    /// de destino com o de origem cru, e com isso quem nasceu em 29 de fevereiro
    /// de 2000 tinha 24 anos em 28 de fevereiro de 2025, enquanto
    /// <see cref="MaisAnos"/> dizia que o aniversário de 25 anos era naquele
    /// mesmo dia. As duas contas discordavam sobre o mesmo fato.
    /// </para>
    /// <para>
    /// A regra que as concilia é: <c>a.MesesAte(b) &gt;= n</c> se e somente se
    /// <c>a.MaisMeses(n) &lt;= b</c>. Ou seja, a distância em meses é definida
    /// <b>pela soma de meses</b>, e não ao lado dela. Duas operações que falam de
    /// meses precisam concordar sobre o que é um mês, ou o sistema que usa as
    /// duas cobra no dia errado.
    /// </para>
    /// </remarks>
    public int MesesAte(Data outra)
    {
        var (ano, mes, dia) = Civil();
        var (outroAno, outroMes, outroDia) = outra.Civil();

        var meses = (outroAno - ano) * 12 + (outroMes - mes);

        // O dia de origem, grudado no último dia do mês de destino -- exatamente
        // o que o MaisMeses faria.
        var grudado = Math.Min(dia, DiasNoMes(outroAno, outroMes));

        if (meses > 0 && outroDia < grudado)
        {
            meses--;
        }
        else if (meses < 0 && outroDia > grudado)
        {
            meses++;
        }

        return meses;
    }

    /// <summary>Quantos anos inteiros de uma data até a outra — a idade de alguém.</summary>
    public int AnosAte(Data outra) => MesesAte(outra) / 12;

    /// <summary>A primeira data do mês.</summary>
    public Data PrimeiroDoMes()
    {
        var (ano, mes, _) = Civil();

        return De(ano, mes, 1);
    }

    /// <summary>A última data do mês.</summary>
    public Data UltimoDoMes()
    {
        var (ano, mes, _) = Civil();

        return De(ano, mes, DiasNoMes(ano, mes));
    }

    /// <summary>
    /// O próximo dia da semana pedido, contando a partir do dia seguinte.
    /// </summary>
    /// <remarks>
    /// A conta com resto evita o laço: a distância até o próximo é
    /// <c>((alvo − hoje + 6) mod 7) + 1</c>, e ela dá 7 quando o alvo é o
    /// próprio dia de hoje — que é o que "próxima segunda", dito numa segunda,
    /// quer dizer.
    /// </remarks>
    public Data Proximo(int diaDaSemana)
    {
        if (diaDaSemana is < 1 or > 7)
        {
            throw new ArgumentOutOfRangeException(
                nameof(diaDaSemana), diaDaSemana, "de 1 (segunda) a 7 (domingo)");
        }

        return MaisDias((diaDaSemana - DiaDaSemana() + 6) % 7 + 1);
    }

    /// <summary>O dia da semana anterior mais próximo, contando a partir do dia anterior.</summary>
    public Data Anterior(int diaDaSemana)
    {
        if (diaDaSemana is < 1 or > 7)
        {
            throw new ArgumentOutOfRangeException(
                nameof(diaDaSemana), diaDaSemana, "de 1 (segunda) a 7 (domingo)");
        }

        return MaisDias(-((DiaDaSemana() - diaDaSemana + 6) % 7 + 1));
    }

    public bool Equals(Data outra) => Dias == outra.Dias;

    public override bool Equals(object? outro) => outro is Data data && Equals(data);

    public override int GetHashCode() => Dias;

    public int CompareTo(Data outra) => Dias.CompareTo(outra.Dias);

    public static bool operator ==(Data um, Data outro) => um.Dias == outro.Dias;

    public static bool operator !=(Data um, Data outro) => um.Dias != outro.Dias;

    public static bool operator <(Data um, Data outro) => um.Dias < outro.Dias;

    public static bool operator >(Data um, Data outro) => um.Dias > outro.Dias;

    public static bool operator <=(Data um, Data outro) => um.Dias <= outro.Dias;

    public static bool operator >=(Data um, Data outro) => um.Dias >= outro.Dias;

    public static int operator -(Data um, Data outro) => um.Dias - outro.Dias;

    /// <summary>O formato do ISO 8601: <c>AAAA-MM-DD</c>.</summary>
    public override string ToString()
    {
        var (ano, mes, dia) = Civil();

        return $"{ano:0000}-{mes:00}-{dia:00}";
    }
}
