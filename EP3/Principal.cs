using System.Diagnostics;

namespace EP3;

public class Principal
{
    public static void Main(string[] args)
    {
        string caminhoArquivo

        int numeroRoteadores = 5;
        int[] iteracoes = new int[numeroRoteadores];
        int[] datagramasEnviados = new int[numeroRoteadores];

        for (int i = 1; i <= numeroRoteadores; i++)
        {
            Roteador roteador = new Roteador(i);
            roteador.Inicializar();
        }

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // Teste para uma comunicação/roteador sem perdas
        // Não é necessário implementar, pois a comunicação ocorre normalmente

        // Teste para uma comunicação com perdas
        // Não é necessário implementar, pois a comunicação ocorre normalmente

        // Teste para um link que teve seu valor modificado
        Roteador roteador1 = ObterRoteadorPorId(1);
        roteador1.AlterarLink(1, 2, 10); // Alterando o peso do link entre roteador 1 e 2 para 10

        // Teste para um roteador que não envia nem recebe mais datagramas
        Roteador roteador2 = ObterRoteadorPorId(2);
        roteador2.RemoverRoteador(2); // Removendo o roteador 2

        stopwatch.Stop();

        // Calculando média da quantidade de iterações até chegar à finalização
        double mediaIteracoes = 0;
        foreach (int iteracao in iteracoes)
        {
            mediaIteracoes += iteracao;
        }
        mediaIteracoes /= iteracoes.Length;

        // Calculando média da quantidade de datagramas enviados
        double mediaDatagramas = 0;
        foreach (int datagramas in datagramasEnviados)
        {
            mediaDatagramas += datagramas;
        }
        mediaDatagramas /= datagramasEnviados.Length;

        Console.WriteLine($"Média da quantidade de iterações: {mediaIteracoes}");
        Console.WriteLine($"Média da quantidade de datagramas enviados: {mediaDatagramas}");
        Console.WriteLine($"Tempo total: {stopwatch.Elapsed}");
    }

    public static Roteador ObterRoteadorPorId(int id, List<Roteador> roteadores)
    {
        foreach (var roteador in roteadores)
        {
            if (roteador.id == id)
            {
                return roteador;
            }
        }
        return null;
    }

    private static int[,] CarregarMatriz(string filePath)
    {
        int[,] matriz;

        string[] linhas = File.ReadAllLines(filePath);

        int numeroLinhas = linhas.Length;

        matriz = new int[numeroLinhas, numeroLinhas];

        for (int i = 0; i < numeroLinhas; i++)
        {
            string[] linha = linhas[i].Split(separator: ", ");

            if (numeroLinhas != linha.Length)
            {
                throw new Exception(message: "Matriz não é quadrada. Valores para certos links entre roteadores faltando. Por favor, checar arquivo de texto");
            }

            for (int j = 0; j < linha.Length; j++)
            {
                matriz[i, j] = int.Parse(linha[j]);
            }
        }

        return matriz;
    }
}
