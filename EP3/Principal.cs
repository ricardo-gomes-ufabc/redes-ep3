using System.Threading;

namespace EP3;

public class Principal
{
    //private static List<Roteador> roteadores = new List<Roteador>();
    private static List<Thread> threadsRoteadores = new List<Thread>();

    private static readonly Random _aleatorio = new Random();

    private static object _locker = new object();

    public static void Main(string[] args)
    {
        Console.WriteLine("Programa Iniciado.\n");

        string caminhoArquivo = $"{AppContext.BaseDirectory}/matriz.txt";

        int[,] matriz = CarregarMatriz(caminhoArquivo);

        int idRoteadorSelecionado = 0;
        //int idRoteadorSelecionado = _aleatorio.Next(minValue: 0, maxValue: matriz.GetLength(dimension: 0));

        Console.WriteLine($"Roteador selecionado: {idRoteadorSelecionado}\n");

        for (int i = 0; i < matriz.GetLength(0); i++)
        {
            Thread thread;

            int indice = i;

            if (i != idRoteadorSelecionado)
            {
                thread = new Thread(() =>
                {
                    Roteador roteador = new Roteador(indice, Roteador.GetLinha(matriz, indice), principal: false);

                    roteador.ProcessarTabelaRoteamento();

                    roteador.Fechar(_locker);
                });
            }
            else
            {
                thread = new Thread(() =>
                {
                    Roteador roteador = new Roteador(indice, Roteador.GetLinha(matriz, indice), principal: true);

                    roteador.ProcessarTabelaRoteamento();

                    roteador.Fechar(_locker);
                });
            }

            threadsRoteadores.Add(thread);
        }

        threadsRoteadores.ForEach(t => t.Start());

        threadsRoteadores.ForEach(t => t.Join());

        Console.WriteLine("Programa Encerrado.");
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
