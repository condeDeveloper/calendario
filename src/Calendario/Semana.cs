namespace Conde.Calendario;

/// <summary>
/// A data por semana do ISO 8601 — <c>2026-W40-2</c>.
/// </summary>
/// <remarks>
/// <para>
/// É o calendário que a indústria usa e quase todo software erra. A regra é
/// curta: a <b>semana 1 é a que contém a primeira quinta-feira do ano</b>, e
/// toda semana vai de segunda a domingo. Dito de outro jeito, é a semana que
/// contém o dia 4 de janeiro, ou ainda: a semana que tem a maioria dos seus dias
/// no ano novo.
/// </para>
/// <para>
/// A consequência que pega todo mundo é que <b>o ano da semana não é o ano da
/// data</b>. 1º de janeiro de 2027 é uma sexta-feira, e ela pertence à semana 53
/// de <b>2026</b>. Na outra ponta, 31 de dezembro de 2029 é uma segunda, e cai
/// na semana 1 de <b>2030</b>. Um relatório semanal que agrupe por
/// <c>ano da data</c> + <c>número da semana</c> vai, nesses anos, partir uma
/// semana em duas e somar errado.
/// </para>
/// <para>
/// E há os anos de <b>53 semanas</b>: 2004, 2009, 2015, 2020, 2026, 2032… Um
/// ano ISO tem 52 ou 53 semanas, e supor 52 é um defeito que aparece uma vez a
/// cada cinco ou seis anos, num dia de dezembro, em produção.
/// </para>
/// </remarks>
public readonly struct Semana : IEquatable<Semana>
{
    /// <summary>O ano <b>da semana</b> — que pode não ser o ano da data.</summary>
    public int Ano { get; }

    /// <summary>O número da semana, de 1 a 52 ou 53.</summary>
    public int Numero { get; }

    /// <summary>O dia da semana: segunda é 1 e domingo é 7.</summary>
    public int Dia { get; }

    public Semana(int ano, int numero, int dia)
    {
        if (ano is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(ano), ano, "o ano vai de 1 a 9999");
        }

        if (numero < 1 || numero > QuantasSemanas(ano))
        {
            throw new ArgumentOutOfRangeException(
                nameof(numero), numero, $"{ano} tem {QuantasSemanas(ano)} semanas");
        }

        if (dia is < 1 or > 7)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dia), dia, "de 1 (segunda) a 7 (domingo)");
        }

        Ano = ano;
        Numero = numero;
        Dia = dia;
    }

    /// <summary>
    /// Quantas semanas o ano ISO tem — 52 ou 53.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Um ano tem 53 semanas quando começa numa quinta-feira, ou quando é
    /// bissexto e começa numa quarta. A razão é aritmética: um ano comum tem
    /// 52 semanas e 1 dia, e um bissexto, 52 semanas e 2 dias — esses dias
    /// sobrando vão empurrando o começo da semana 1 até que um ano consiga
    /// abrigar uma quinta-feira a mais.
    /// </para>
    /// <para>
    /// A conta abaixo é a forma fechada disso, e a alternativa seria montar o
    /// dia 28 de dezembro e perguntar em que semana ele cai — 28 de dezembro é
    /// o último dia que <b>sempre</b> pertence à última semana do próprio ano.
    /// </para>
    /// </remarks>
    public static int QuantasSemanas(int ano)
    {
        var primeiro = Data.De(ano, 1, 1).DiaDaSemana();

        return primeiro == 4 || (Data.Bissexto(ano) && primeiro == 3) ? 53 : 52;
    }

    /// <summary>A semana ISO a que a data pertence.</summary>
    /// <remarks>
    /// <para>
    /// O caminho é achar a quinta-feira da semana da data: como a semana 1 é a
    /// que contém a primeira quinta-feira, a quinta-feira de qualquer semana
    /// está sempre <b>dentro</b> do ano ISO a que a semana pertence — e é ela
    /// que decide o ano.
    /// </para>
    /// <para>
    /// Achado o ano, o número é a distância até a primeira quinta-feira dele,
    /// dividida por sete, mais um.
    /// </para>
    /// </remarks>
    public static Semana De(Data data)
    {
        var dia = data.DiaDaSemana();

        // A quinta-feira desta semana. Segunda é 1, quinta é 4.
        var quinta = data.MaisDias(4 - dia);

        var ano = quinta.Ano;

        var primeiraQuinta = PrimeiraQuinta(ano);

        return new Semana(ano, (quinta - primeiraQuinta) / 7 + 1, dia);
    }

    /// <summary>A data que esta semana designa.</summary>
    public Data ParaData() => PrimeiraQuinta(Ano).MaisDias((Numero - 1) * 7 - 4 + Dia);

    /// <summary>A primeira quinta-feira do ano — o pivô de tudo.</summary>
    private static Data PrimeiraQuinta(int ano)
    {
        var quatro = Data.De(ano, 1, 4);

        // 4 de janeiro está SEMPRE na semana 1, por construção da regra: se a
        // semana 1 é a que contém a primeira quinta, ela começa entre 29 de
        // dezembro e 4 de janeiro -- e portanto contém o dia 4.
        return quatro.MaisDias(4 - quatro.DiaDaSemana());
    }

    /// <summary>A segunda-feira que abre esta semana.</summary>
    public Data Segunda() => new Semana(Ano, Numero, 1).ParaData();

    /// <summary>O domingo que a fecha.</summary>
    public Data Domingo() => new Semana(Ano, Numero, 7).ParaData();

    public bool Equals(Semana outra) =>
        Ano == outra.Ano && Numero == outra.Numero && Dia == outra.Dia;

    public override bool Equals(object? outro) => outro is Semana semana && Equals(semana);

    public override int GetHashCode() => (Ano * 100 + Numero) * 10 + Dia;

    /// <summary>O formato do ISO 8601: <c>AAAA-Wnn-D</c>.</summary>
    public override string ToString() => $"{Ano:0000}-W{Numero:00}-{Dia}";
}
