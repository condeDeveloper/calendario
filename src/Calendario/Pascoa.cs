namespace Conde.Calendario;

/// <summary>
/// A Páscoa — e, com ela, as datas móveis do ano.
/// </summary>
/// <remarks>
/// <para>
/// A regra é de 325, do Concílio de Niceia: a Páscoa é o <b>primeiro domingo
/// depois da primeira lua cheia que cai em 21 de março ou depois</b>. Só que a
/// "lua cheia" da regra não é a do céu — é a <b>lua eclesiástica</b>, uma lua de
/// tabela, e é por isso que a conta é possível e não depende de observação.
/// </para>
/// <para>
/// A diferença importa: em alguns anos a lua da tabela e a do céu não coincidem,
/// e a Páscoa cai numa semana que a astronomia não escolheria. Isso não é
/// defeito — a tabela <b>é</b> a definição. Um programa que calculasse a fase
/// real da lua daria a resposta errada.
/// </para>
/// <para>
/// O algoritmo abaixo é o "anônimo gregoriano", publicado sem autor na revista
/// <i>Nature</i> em 1876. Ele cabe em dez linhas de divisão inteira e não tem
/// nenhuma constante mágica: cada uma é um pedaço datado da regra, e estão
/// nomeadas.
/// </para>
/// <para>
/// A Páscoa cai entre <b>22 de março e 25 de abril</b>, e o ciclo inteiro se
/// repete a cada 5.700.000 anos. Dentro de um intervalo humano ela parece
/// aleatória, e é a razão de tanta coisa no calendário comercial brasileiro —
/// carnaval, Sexta-feira Santa, Corpus Christi — andar de ano para ano.
/// </para>
/// </remarks>
public static class Pascoa
{
    /// <summary>O domingo de Páscoa do ano.</summary>
    public static Data De(int ano)
    {
        if (ano is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(ano), ano, "o ano vai de 1 a 9999");
        }

        // O ciclo de Méton: 19 anos solares são quase exatamente 235 lunações,
        // e o resto por 19 diz onde o ano está nesse ciclo. É o número de ouro,
        // menos um.
        var douro = ano % 19;

        var seculo = ano / 100;
        var doSeculo = ano % 100;

        // A correção solar: quantos dias bissextos os anos seculares tiraram.
        var solar = seculo / 4;
        var restoSolar = seculo % 4;

        // A correção lunar: o ciclo de Méton erra cerca de um dia a cada 308
        // anos, e esta é a emenda gregoriana para isso.
        var lunar = (seculo + 8) / 25;
        var correcaoLunar = (seculo - lunar + 1) / 3;

        // A epacta: a idade da lua eclesiástica em 1º de janeiro.
        var epacta = (19 * douro + seculo - solar - correcaoLunar + 15) % 30;

        var quartoDoAno = doSeculo / 4;
        var restoDoAno = doSeculo % 4;

        // O dia da semana, pela mesma aritmética.
        var semana = (32 + 2 * restoSolar + 2 * quartoDoAno - epacta - restoDoAno) % 7;

        // A correção das duas exceções da tabela lunar -- os dois casos em que a
        // lua eclesiástica precisaria cair num dia que a tabela não tem.
        var emenda = (douro + 11 * epacta + 22 * semana) / 451;

        var total = epacta + semana - 7 * emenda + 114;

        var mes = total / 31;      // 3 = março, 4 = abril
        var dia = total % 31 + 1;

        return Data.De(ano, mes, dia);
    }

    /// <summary>A Sexta-feira Santa — dois dias antes.</summary>
    public static Data SextaSanta(int ano) => De(ano).MaisDias(-2);

    /// <summary>A terça-feira de carnaval — 47 dias antes.</summary>
    public static Data Carnaval(int ano) => De(ano).MaisDias(-47);

    /// <summary>A quarta-feira de cinzas — 46 dias antes, e o começo da quaresma.</summary>
    public static Data Cinzas(int ano) => De(ano).MaisDias(-46);

    /// <summary>Corpus Christi — 60 dias depois.</summary>
    public static Data CorpusChristi(int ano) => De(ano).MaisDias(60);

    /// <summary>
    /// Os feriados nacionais brasileiros do ano, em ordem.
    /// </summary>
    /// <remarks>
    /// <para>
    /// São os da Lei 662/1949 com as que vieram depois, mais os móveis que saem
    /// da Páscoa. Carnaval, Quarta-feira de Cinzas e Corpus Christi <b>não</b>
    /// são feriados nacionais por lei federal — são ponto facultativo — e estão
    /// marcados como tal, porque a diferença aparece em folha de pagamento.
    /// </para>
    /// <para>
    /// O domingo de Páscoa também não é: ele não consta de nenhuma lei federal,
    /// e a Sexta-feira Santa é que foi declarada feriado religioso nacional pela
    /// Lei 9.093/1995. A Páscoa está na lista porque é o <b>pivô</b> — as outras
    /// quatro datas móveis são todas contadas a partir dela — e não porque seja
    /// folga. Como cai sempre num domingo, a distinção nunca muda uma folha; mas
    /// marcá-la como feriado de lei seria escrever um erro no código.
    /// </para>
    /// <para>
    /// A consciência negra, em 20 de novembro, virou feriado nacional pela Lei
    /// 14.759 de <b>2023</b>, e antes disso era estadual em alguns lugares. Por
    /// isso ele entra na lista só a partir de 2024.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<Feriado> FeriadosDoBrasil(int ano)
    {
        var lista = new List<Feriado>
        {
            new(Data.De(ano, 1, 1), "Confraternização Universal", true),
            new(Carnaval(ano), "Carnaval", false),
            new(Cinzas(ano), "Quarta-feira de Cinzas", false),
            new(SextaSanta(ano), "Sexta-feira Santa", true),
            new(De(ano), "Páscoa", false),
            new(Data.De(ano, 4, 21), "Tiradentes", true),
            new(Data.De(ano, 5, 1), "Dia do Trabalho", true),
            new(CorpusChristi(ano), "Corpus Christi", false),
            new(Data.De(ano, 9, 7), "Independência", true),
            new(Data.De(ano, 10, 12), "Nossa Senhora Aparecida", true),
            new(Data.De(ano, 11, 2), "Finados", true),
            new(Data.De(ano, 11, 15), "Proclamação da República", true),
            new(Data.De(ano, 12, 25), "Natal", true),
        };

        if (ano >= 2024)
        {
            lista.Add(new Feriado(
                Data.De(ano, 11, 20), "Consciência Negra", true));
        }

        lista.Sort((um, outro) => um.Quando.CompareTo(outro.Quando));

        return lista;
    }

    /// <summary>Um feriado: quando, qual, e se é feriado de lei ou ponto facultativo.</summary>
    /// <param name="Quando">O dia.</param>
    /// <param name="Nome">O nome.</param>
    /// <param name="DeLei">Verdadeiro se for feriado nacional por lei federal.</param>
    public readonly record struct Feriado(Data Quando, string Nome, bool DeLei);
}
