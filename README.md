# calendario

Datas do zero em C# e .NET 8 — calendário gregoriano proléptico, semana ISO 8601,
leitor e escritor do 8601, aritmética de meses e a Páscoa.

```
$ dotnet medidor.dll mes 2026 9

     setembro de 2026

seg ter qua qui sex sáb dom
              1   2   3   4   5   6
  7   8   9  10  11  12  13
 14  15  16  17  18  19  20
 21  22  23  24  25  26  27
 28  29  30

  30 dias, de 2026-09-01 a 2026-09-30
  semanas ISO de 36 a 40
```

## O juiz é o domínio inteiro

O domínio de uma data gregoriana entre 0001-01-01 e 9999-12-31 tem **3.652.059**
elementos, e isso cabe num laço de vinte segundos.

Então aqui não há amostra e não há sorteio. Cada dia representável é montado,
convertido nos dois sentidos e comparado com o `DateOnly`, o `DateTime` e o
`System.Globalization.ISOWeek` do .NET — número de dias desde a época, ano, mês,
dia, dia da semana, dia do ano, semana ISO e ano ISO. O que a suíte produz não é
evidência estatística: é **verificação exaustiva**. Se ela passa, não existe uma
data em que a conversão erre, porque todas foram experimentadas.

Isso é uma sorte rara. Quase todo código tem domínio grande demais, e por isso se
testa por amostra, por propriedade ou por exemplo. Quando o domínio **cabe**,
testar por amostra é desperdiçar a única chance de ter certeza.

São 123 testes. Fora o laço exaustivo, há o `DateTime.AddMonths` em 800 mil
somas, a tabela publicada de Páscoas, e 40 entradas que o leitor do 8601 tem de
**recusar**.

## O defeito: duas contas que discordavam sobre o que é um mês

`MaisMeses` gruda no último dia do mês — 31 de janeiro mais um mês é 28 de
fevereiro, como no `DateTime.AddMonths` e no `java.time`. `MesesAte` contava
meses inteiros comparando o dia de destino com o dia de origem **cru**.

As duas passavam nos seus próprios testes. E discordavam:

| | |
|---|---|
| nasceu em | 2000-02-29 |
| `MaisAnos(25)` diz que o aniversário de 25 é em | **2025-02-28** |
| `AnosAte(2025-02-28)` respondia | **24** |

Um sistema que usasse a primeira para agendar e a segunda para conferir cobraria
no dia errado, e o erro é de um dia — o tamanho exato que passa despercebido numa
conferência por cima.

A regra que concilia as duas: `a.MesesAte(b) >= n` **se e somente se**
`a.MaisMeses(n) <= b`. A distância em meses tem de ser definida *pela* soma de
meses, e não ao lado dela. O teste que enuncia isso roda em todo dia de duzentos
anos e sete deslocamentos.

O que vale guardar: **duas operações que falam da mesma unidade precisam
concordar sobre o que ela é**, e nenhuma das duas sozinha revela a discordância.

## A semana ISO, que quase todo software erra

```
$ dotnet medidor.dll semanas

  2004  2009  2015  2020  2026  2032  2037  2043  2048  2054  2060  2065  2071
  2076  2082  2088  2093  2099

  18 em 101 anos — um a cada cinco ou seis.

E os dias em que o ano da semana não é o ano da data:

    2024-12-30   2025-W01-1      2024 -> 2025
    2025-12-31   2026-W01-3      2025 -> 2026
    2027-01-01   2026-W53-5      2027 -> 2026
    2028-01-02   2027-W52-7      2028 -> 2027
    2031-12-31   2032-W01-3      2031 -> 2032
```

São dois defeitos, e os dois aparecem uma vez por ano, em dezembro, em produção:

1. **O ano da semana não é o ano da data.** 1º de janeiro de 2027 é uma
   sexta-feira e pertence à semana 53 de **2026**. Um relatório agrupado por
   (ano da data, número da semana) parte essa semana em duas e mostra metade do
   total em cada.
2. **Nem todo ano tem 52 semanas.** 1.775 dos 9.999 anos têm 53.

A regra é curta: a semana 1 é a que contém a **primeira quinta-feira** do ano, e
toda semana vai de segunda a domingo. Equivalentemente, é a semana que contém o
dia 4 de janeiro.

## A Páscoa, e o que o ciclo de 400 anos não repete

O calendário gregoriano se repete inteiro a cada **400 anos**, e o número que
manda nisso é 146.097: é quantos dias tem o ciclo, e é divisível por 7. Um
calendário de parede de 2026 serve outra vez em 2426, dia da semana e tudo — e é
por isso que a conversão de datas cabe em aritmética inteira, sem tabela nenhuma.

Eu escrevi na ferramenta que a Páscoa também se repetia. A tabela que a própria
ferramenta imprime logo abaixo me desmentiu:

```
$ dotnet medidor.dll ciclo

    ano    1º de janeiro       Páscoa
    --------------------------------------------
    1226   quinta-feira     1226-03-22
    1626   quinta-feira     1626-04-12
    2026   quinta-feira     2026-04-05
    2426   quinta-feira     2426-03-29
    2826   quinta-feira     2826-04-19
```

O 1º de janeiro cai na mesma quinta-feira nos cinco anos. A Páscoa cai em cinco
dias diferentes. Ela não depende só do calendário solar: a **epacta** — a idade
da lua eclesiástica — tem a sua própria correção por século, e o ciclo que fecha
as duas coisas ao mesmo tempo é de **5.700.000 anos**. Dentro do intervalo
representável ele não fecha nenhuma vez.

E a Páscoa não é aleatória dentro da sua janela de 35 dias:

```
$ dotnet medidor.dll pascoa

  22/3  #####                                      55
  23/3  #########                                  92
  25/3  ####################                      191
  31/3  ####################################      344
  19/4  ##########################################372
  25/4  ########                                   77
```

O dia mais comum acontece quase **sete vezes** mais que o mais raro — e o mais
raro é 22 de março, não 25 de abril. Também escrevi ao contrário e o teste
reprovou: para cair em 22 de março são precisas duas coincidências, e para cair
em 25 de abril, uma.

O juiz da Páscoa é de outro tipo, porque o .NET não a calcula: são **49 datas de
tabela publicada** mais as propriedades que a regra de Niceia obriga, conferidas
em todos os 9.999 anos — é sempre domingo, e cai sempre entre 22 de março e 25 de
abril.

## Um número, e não três

A estrutura guarda **um inteiro**: quantos dias desde 1970-01-01. Ano, mês e dia
saem de uma conta — a de Howard Hinnant — e não de tabela nem de laço.

A consequência prática é que **comparar duas datas é comparar dois inteiros** e a
diferença entre elas é uma subtração. As duas operações que mais aparecem em
código real ficam triviais, e o custo se paga na conversão, que só acontece
quando alguém quer ver o ano.

O truque da conta é mudar onde o ano começa: se março for o mês 1, o dia bissexto
cai no **fim** do ano, e o comprimento dos meses vira uma sequência regular sem o
degrau de fevereiro no meio. Feita a troca, o dia do ano é `(153·m + 2) / 5`, e
essa fração reproduz 31, 30, 31, 30, 31, 31, 30, 31, 30, 31, 31, 28 sem tabela.

O dia da semana, por consequência, é só um resto por 7.

```
$ dotnet medidor.dll velocidade

  operação                        meu       .NET     razão
  --------------------------------------------------------
  dias -> ano-mês-dia         153.7 ms     101.1 ms    1.52x
  ano-mês-dia -> dias         112.4 ms      33.3 ms    3.38x
```

Mais lento que o `DateOnly`, e as duas fazem a mesma conta. A diferença é a
validação, que aqui acontece em toda construção.

## O gregoriano proléptico, e por que não há buraco

As regras de 1582 aplicadas para trás até o ano 1, inclusive nos séculos em que
ninguém as usava. É a mesma escolha do ISO 8601 e do `DateTime` do .NET, e troca
fidelidade histórica por uma propriedade que vale mais em software: **não há
buraco**. Entre 1582-10-04 e 1582-10-15 o calendário real pulou dez dias, e uma
biblioteca que os respeitasse teria uma data inexistente no meio da reta.

## O leitor do 8601, e o que ele recusa

Três grafias do mesmo dia, cada uma com forma estendida e compacta:

| | |
|---|---|
| `2026-09-29` / `20260929` | por calendário |
| `2026-272` / `2026272` | ordinal |
| `2026-W40-2` / `2026W402` | por semana |

A metade fácil de um leitor é aceitar o válido, e está testada nos 3,6 milhões de
dias, nas quatro grafias. A difícil é recusar o resto, e são 40 entradas nomeadas:
`2026-02-29`, `2026-0929` (formas misturadas, que a norma proíbe), `2026/09/29`,
`+2026-09-29`, ` 2026-09-29`, `2025-W53-1` (2025 tem 52 semanas), `2026-366`.

O leitor de número é escrito à mão de propósito. O `int.TryParse` aceita, conforme
a cultura, sinal, espaço em volta e separador de milhar: `" +1 "` vira 1. Num
leitor de formato de troca, cada uma dessas gentilezas é uma entrada inválida que
passa — e um leitor generoso demais não avisa que a entrada está errada, ele
**inventa uma data**.

## Feriados

```
$ dotnet medidor.dll feriados 2026

  2026-01-01   quinta-feira     Confraternização Universal
  2026-02-17   terça-feira      Carnaval                        (ponto facultativo)
  2026-04-03   sexta-feira      Sexta-feira Santa
  2026-04-21   terça-feira      Tiradentes
  ...
  9 dos feriados de lei caem de segunda a sexta.
```

Carnaval, Quarta-feira de Cinzas e Corpus Christi **não** são feriados nacionais
por lei federal — são ponto facultativo, e a diferença aparece em folha de
pagamento. O domingo de Páscoa também não: ele não consta de lei nenhuma, e quem
foi declarada feriado religioso nacional pela Lei 9.093/1995 é a Sexta-feira
Santa. A Consciência Negra entra a partir de 2024, pela Lei 14.759/2023.

E um caso que o teste achou: em **1957 a Páscoa caiu em 21 de abril**, que é o dia
de Tiradentes. Dois feriados no mesmo dia acontece de tempos em tempos, e quem
escreve um calculador de dias úteis precisa contar o dia uma vez só.

## Rodar

.NET 8. Zero dependências fora do xUnit, e só nos testes.

```
dotnet test testes/Calendario.Testes/Calendario.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- ano 2026
```

As medidas de tempo moram na ferramenta e **não** na suíte, de propósito: uma
afirmação de relógio falha sem que nada tenha piorado — basta a máquina do CI
estar ocupada — e o efeito é ensinar quem trabalha no projeto a ignorar teste
vermelho. A ferramenta roda no CI, imprime e sai com zero sempre.

## O que ele não faz

Não tem hora, nem fuso, nem horário de verão. É deliberado: uma data sem hora é
um conceito completo e fechado, e quase todo bug de fuso nasce de misturar os
dois. Quem precisa de instante no tempo quer `DateTimeOffset`; quem precisa de
fuso quer o banco IANA, que muda várias vezes por ano e não cabe num repositório
sem dependências.

Não tem calendário juliano, nem hebraico, nem islâmico, nem japonês. Não tem
Páscoa ortodoxa, que usa o juliano e cai em outro dia. Não tem dias úteis com
feriado estadual ou municipal — os estaduais são 27 listas e os municipais são
5.570.

Não vai além de 9999, porque o `DateTime` do .NET não vai, e o juiz exaustivo
deixaria de existir.

## Licença

MIT.
