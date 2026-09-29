using System.Globalization;

namespace Conde.Calendario;

/// <summary>
/// Ler e escrever datas no ISO 8601 — as três formas, e só as três.
/// </summary>
/// <remarks>
/// <para>
/// A norma admite três maneiras de escrever o mesmo dia, e as três estão aqui:
/// </para>
/// <list type="table">
///   <item><term><c>2026-09-29</c></term><description>por calendário — ano, mês, dia</description></item>
///   <item><term><c>2026-272</c></term><description>ordinal — ano e dia do ano</description></item>
///   <item><term><c>2026-W40-2</c></term><description>por semana — ano ISO, semana, dia da semana</description></item>
/// </list>
/// <para>
/// Cada uma tem também a forma <b>compacta</b>, sem os hifens: <c>20260929</c>,
/// <c>2026272</c>, <c>2026W402</c>. Misturar os dois estilos dentro de uma mesma
/// data — <c>2026-0929</c> — é proibido pela norma, e é recusado aqui.
/// </para>
/// <para>
/// O que este leitor <b>não</b> faz, de propósito: não aceita fuso, não aceita
/// hora, não aceita ano com sinal ou com mais de quatro algarismos (a norma
/// permite, mediante acordo prévio entre as partes, e "mediante acordo prévio"
/// é o contrário do que um formato de troca deve exigir), e não aceita
/// intervalos nem durações.
/// </para>
/// </remarks>
public static class Iso
{
    /// <summary>Lê uma data em qualquer das três formas.</summary>
    public static Data Ler(string texto) => TentarLer(texto, out var data)
        ? data
        : throw new FormatException($"não é uma data ISO 8601: \"{texto}\"");

    /// <summary>Lê uma data, devolvendo falso em vez de lançar.</summary>
    public static bool TentarLer(string? texto, out Data data)
    {
        data = default;

        if (texto is null)
        {
            return false;
        }

        var bruto = texto.AsSpan();

        // O 'W' na quinta posição (com hifens) ou na quinta (sem) marca a forma
        // por semana. É a única das três que tem uma letra, o que torna a
        // triagem barata.
        if (bruto.Length >= 5 && (bruto[4] == 'W' || (bruto.Length >= 6 && bruto[5] == 'W')))
        {
            return LerSemana(bruto, out data);
        }

        return bruto.Length switch
        {
            10 => LerCalendario(bruto, true, out data),
            8 when bruto[4] == '-' => LerOrdinal(bruto, true, out data),
            8 => LerCalendario(bruto, false, out data),
            7 => LerOrdinal(bruto, false, out data),
            _ => false,
        };
    }

    private static bool LerCalendario(ReadOnlySpan<char> bruto, bool comHifen, out Data data)
    {
        data = default;

        if (comHifen && (bruto[4] != '-' || bruto[7] != '-'))
        {
            return false;
        }

        var passoMes = comHifen ? 5 : 4;
        var passoDia = comHifen ? 8 : 6;

        if (!Numero(bruto[..4], out var ano)
            || !Numero(bruto.Slice(passoMes, 2), out var mes)
            || !Numero(bruto.Slice(passoDia, 2), out var dia))
        {
            return false;
        }

        return Data.TentarDe(ano, mes, dia, out data);
    }

    private static bool LerOrdinal(ReadOnlySpan<char> bruto, bool comHifen, out Data data)
    {
        data = default;

        if (comHifen && bruto[4] != '-')
        {
            return false;
        }

        if (!Numero(bruto[..4], out var ano)
            || !Numero(bruto.Slice(comHifen ? 5 : 4, 3), out var diaDoAno))
        {
            return false;
        }

        if (ano is < 1 or > 9999 || diaDoAno < 1 || diaDoAno > Data.DiasNoAno(ano))
        {
            return false;
        }

        data = Data.De(ano, 1, 1).MaisDias(diaDoAno - 1);

        return true;
    }

    private static bool LerSemana(ReadOnlySpan<char> bruto, out Data data)
    {
        data = default;

        var comHifen = bruto.Length == 10;

        if (comHifen)
        {
            if (bruto[4] != '-' || bruto[5] != 'W' || bruto[8] != '-')
            {
                return false;
            }
        }
        else if (bruto.Length != 8 || bruto[4] != 'W')
        {
            return false;
        }

        if (!Numero(bruto[..4], out var ano)
            || !Numero(bruto.Slice(comHifen ? 6 : 5, 2), out var numero)
            || !Numero(bruto.Slice(comHifen ? 9 : 7, 1), out var dia))
        {
            return false;
        }

        if (ano is < 1 or > 9999
            || numero < 1 || numero > Semana.QuantasSemanas(ano)
            || dia is < 1 or > 7)
        {
            return false;
        }

        var achada = new Semana(ano, numero, dia).ParaData();

        // A semana 52 ou 53 de um ano pode designar um dia do ano seguinte, e a
        // semana 1 um dia do anterior -- e nas pontas do intervalo isso sai da
        // faixa representável. Melhor recusar do que estourar.
        if (achada.Dias < Data.Menor.Dias || achada.Dias > Data.Maior.Dias)
        {
            return false;
        }

        data = achada;

        return true;
    }

    /// <summary>
    /// Lê um número de tamanho fixo — sem sinal, sem espaço, sem separador.
    /// </summary>
    /// <remarks>
    /// Escrito à mão de propósito. O <c>int.TryParse</c> aceita, conforme a
    /// cultura, sinal, espaço em volta e separador de milhar: <c>" +1 "</c> vira
    /// 1, e <c>"1,234"</c> pode virar 1234. Num leitor de formato de troca, cada
    /// uma dessas gentilezas é uma entrada inválida que passa.
    /// </remarks>
    private static bool Numero(ReadOnlySpan<char> bruto, out int valor)
    {
        valor = 0;

        foreach (var letra in bruto)
        {
            if (letra is < '0' or > '9')
            {
                return false;
            }

            valor = valor * 10 + (letra - '0');
        }

        return true;
    }

    /// <summary>Escreve <c>AAAA-MM-DD</c>.</summary>
    public static string Escrever(Data data) => data.ToString();

    /// <summary>Escreve <c>AAAAMMDD</c>, a forma compacta.</summary>
    public static string EscreverCompacto(Data data)
    {
        var (ano, mes, dia) = data.Civil();

        return $"{ano:0000}{mes:00}{dia:00}";
    }

    /// <summary>Escreve <c>AAAA-DDD</c>, a forma ordinal.</summary>
    public static string EscreverOrdinal(Data data) => $"{data.Ano:0000}-{data.DiaDoAno():000}";

    /// <summary>Escreve <c>AAAA-Wnn-D</c>, a forma por semana.</summary>
    public static string EscreverSemana(Data data) => Semana.De(data).ToString();

    /// <summary>O nome do mês em português.</summary>
    public static string NomeDoMes(int mes) => mes is >= 1 and <= 12
        ? Meses[mes - 1]
        : throw new ArgumentOutOfRangeException(nameof(mes), mes, "o mês vai de 1 a 12");

    /// <summary>O nome do dia da semana em português — 1 é segunda.</summary>
    public static string NomeDoDia(int diaDaSemana) => diaDaSemana is >= 1 and <= 7
        ? Dias[diaDaSemana - 1]
        : throw new ArgumentOutOfRangeException(
            nameof(diaDaSemana), diaDaSemana, "de 1 (segunda) a 7 (domingo)");

    /// <summary>A data por extenso: <c>29 de setembro de 2026</c>.</summary>
    public static string PorExtenso(Data data)
    {
        var (ano, mes, dia) = data.Civil();

        return string.Create(
            CultureInfo.InvariantCulture, $"{dia} de {Meses[mes - 1]} de {ano}");
    }

    private static readonly string[] Meses =
    [
        "janeiro", "fevereiro", "março", "abril", "maio", "junho",
        "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
    ];

    private static readonly string[] Dias =
    [
        "segunda-feira", "terça-feira", "quarta-feira", "quinta-feira",
        "sexta-feira", "sábado", "domingo",
    ];
}
