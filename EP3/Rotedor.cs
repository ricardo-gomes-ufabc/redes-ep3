using System.Net;
using System.Timers;
using Timer = System.Timers.Timer;


namespace EP3;

public class Roteador
{
    public int Id { get; private set; }
    public bool Principal { get; private set; }

    private bool _roteadorAtivo = true;

    private const int OffsetPorta = 10000;
    private const int Infinito = int.MaxValue;

    private Canal _canal;
    private List<IPEndPoint> vizinhos = new List<IPEndPoint>();

    private int[,] _matrizAdjacencia;

    private bool _distanciaAtualizada;

    private const int _timeoutMilissegundos = 10000;
    private CancellationTokenSource _tockenCancelamentoRecebimento = new CancellationTokenSource();
    private Timer _temporizadorRecebimento = new Timer(_timeoutMilissegundos);
    private ElapsedEventHandler _evento;

    public Roteador(int id, int[] vetorDistancias, bool principal)
    {
        Id = id;
        Principal = principal;

        _canal = new Canal(OffsetPorta + id);

        MapearVizinhos(vetorDistancias);

        InicializarMatriz(vetorDistancias);

        _evento = new ElapsedEventHandler(TemporizadorEncerrado);

        _temporizadorRecebimento.Elapsed += _evento;
        _temporizadorRecebimento.AutoReset = true;
    }

    private void MapearVizinhos(int[] vetorDistancias)
    {
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

        for (int i = 0; i < vetorDistancias.Length; i++)
        {
            if (i != Id && vetorDistancias[i] != Infinito)
            {
                vizinhos.Add(new IPEndPoint(ipAddress, OffsetPorta + i));
            }
        }
    }

    private void InicializarMatriz(int[] vetorDistancias)
    {
        _matrizAdjacencia = new int[vetorDistancias.Length, vetorDistancias.Length];

        for (int i = 0; i < vetorDistancias.Length; i++)
        {
            for (int j = 0; j < vetorDistancias.Length; j++)
            {
                if (i != Id || (i != j && vetorDistancias[j] == 0))
                {
                    _matrizAdjacencia[i, j] = Infinito;
                }
                else
                {
                    _matrizAdjacencia[i, j] = vetorDistancias[j];
                }
            }
        }
    }

    public void ProcessarTabelaRoteamento()
    {
        EnviarDatagramaInfo();

        while (_roteadorAtivo)
        {
            _temporizadorRecebimento.Start();

            DatagramaInfo? datagramaInfoRecebido = _canal.ReceberDatagramaInfo(_tockenCancelamentoRecebimento.Token);

            _temporizadorRecebimento.Stop();

            if (datagramaInfoRecebido != null)
            {
                if (Principal)
                {
                    ImprimirDatagramaInfo(datagramaInfoRecebido);
                }

                AtualizarMatrizAdjacencias(datagramaInfoRecebido);
            }
        }

        if (Principal)
        {
            Console.WriteLine("Nenhuma informação recebida até o timeout. Encerrando o Roteador.\n");
        }
    }

    private void ImprimirDatagramaInfo(DatagramaInfo datagramaInfoRecebido)
    {
        int tamanhoVetor = datagramaInfoRecebido.VetorDistancias.Length;
        string valoresVetor = "- Vetor de distâncias do DatagramaInfo: ";

        for (int i = 0; i < tamanhoVetor - 1; i++)
        {
            if (datagramaInfoRecebido.VetorDistancias[i] == Infinito)
            {
                valoresVetor += "Infinito, ";

            }
            else
            {
                valoresVetor += $"{datagramaInfoRecebido.VetorDistancias[i]}, ";
            }
            
        }

        if (datagramaInfoRecebido.VetorDistancias[tamanhoVetor - 1] == Infinito)
        {
            valoresVetor += "Infinito";

        }
        else
        {
            valoresVetor += $"{datagramaInfoRecebido.VetorDistancias[tamanhoVetor - 1]}";
        }

        Console.WriteLine($"DatagramaInfo enviado pelo Roteador {datagramaInfoRecebido.OrigemId}:");
        Console.WriteLine($"{valoresVetor}\n");
    }

    private void AtualizarMatrizAdjacencias(DatagramaInfo datagramaInfo)
    {
        int n = datagramaInfo.VetorDistancias.Length;

        if (Principal)
        {
            Console.WriteLine($"Vetor de distâncias do Roteador {datagramaInfo.OrigemId} atualizada");
        }

        for (int i = 0; i < n; i++)
        {
            _matrizAdjacencia[datagramaInfo.OrigemId, i] = datagramaInfo.VetorDistancias[i];

            if (i != Id)
            {
                int distanciaAntiga = _matrizAdjacencia[Id, datagramaInfo.OrigemId];

                int distanciaNova;

                int custoAoVizinho = _matrizAdjacencia[Id, i];
                int distânciaVizinhoOrigem = _matrizAdjacencia[datagramaInfo.OrigemId, i];

                if (custoAoVizinho == Infinito || distânciaVizinhoOrigem == Infinito)
                {
                    distanciaNova = Infinito;
                }
                else
                {
                    distanciaNova = custoAoVizinho + distânciaVizinhoOrigem;
                }

                if (distanciaNova < distanciaAntiga)
                {
                    _matrizAdjacencia[Id, datagramaInfo.OrigemId] = distanciaNova;
                    _distanciaAtualizada = true;

                    if (Principal)
                    {
                        Console.WriteLine($"Nova rota de menor custo encontrada entre Roteador {Id} e {datagramaInfo.OrigemId}: {distanciaAntiga} -> {distanciaNova}");
                    }
                }
            }
        }

        if (_distanciaAtualizada)
        {
            EnviarDatagramaInfo();

            _distanciaAtualizada = false;
        }

        if (Principal)
        {
            Console.WriteLine();
        }
    }

    private void EnviarDatagramaInfo()
    {
        int[] vetorDistancias = GetLinha(_matrizAdjacencia, Id);

        DatagramaInfo datagramaInfo = new DatagramaInfo(Id, vetorDistancias);

        foreach (IPEndPoint vizinho in vizinhos)
        {
            _canal.ProcessarMensagem(datagramaInfo, vizinho);
        }
    }

    public static int[] GetLinha(int[,] matrix, int linha)
    {
        return Enumerable.Range(start: 0, matrix.GetLength(dimension: 1))
                                                .Select(x => matrix[linha, x])
                                                .ToArray();
    }

    private void TemporizadorEncerrado(object? sender, ElapsedEventArgs e)
    {
        _temporizadorRecebimento.Stop();
        _roteadorAtivo = false;
        _tockenCancelamentoRecebimento.Cancel();
    }

    public void Fechar(object locker)
    {
        lock (locker)
        {
            ImprimirTabela();

            _tockenCancelamentoRecebimento.Dispose();

            _temporizadorRecebimento.Dispose();

            _canal.Fechar(Principal);
        }
    }

    public void ImprimirTabela()
    {
        Console.WriteLine($"Tabela de Roteamento do Roteador {Id}:");

        int n = _matrizAdjacencia.GetLength(dimension: 0);

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (_matrizAdjacencia[i, j] == Infinito)
                {
                    Console.Write("Infinito, ");
                }
                else
                {
                    Console.Write($"{_matrizAdjacencia[i, j]}, ");
                }
            }

            Console.WriteLine();
        }

        
    }
}