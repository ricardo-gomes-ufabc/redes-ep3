namespace EP3;

public class Principal
{
    //private static List<Roteador> roteadores = new List<Roteador>();
    private static List<Threads> roteadores = new List<Threads>();

    private static readonly Random _aleatorio = new Random();

    public static void Main(string[] args)
    {
        string caminhoArquivo = $"{AppContext.BaseDirectory}/matriz.txt";

        int[,] matriz = CarregarMatriz(caminhoArquivo);

        for (int i = 0; i < matriz.GetLength(0); i++)
        {
            //roteadores.Add(new Roteador(i, GetLinha(matriz, i)));
            roteadores.Add(new Threads(i, GetLinha(matriz, i)));
        }

        int idRoteadorSelecionado = 0;
        //int idRoteadorSelecionado = _aleatorio.Next(minValue: 0, maxValue: matriz.GetLength(dimension: 0));


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

    public static int[] GetLinha(int[,] matrix, int linha)
    {
        return Enumerable.Range(start: 0, matrix.GetLength(dimension: 1))
                         .Select(x => matrix[linha, x])
                         .ToArray();
    }
}
