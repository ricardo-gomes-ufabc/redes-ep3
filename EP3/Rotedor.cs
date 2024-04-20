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

    private const int _timeoutMilissegundos = 30000;
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

        for (int i = 0; vetorDistancias.Length > 0; i++)
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
                if (i == Id)
                {
                    _matrizAdjacencia[i, j] = vetorDistancias[j];
                }
                else
                {
                    _matrizAdjacencia[i, j] = Infinito;
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
                AtualizarMatrizAdjacencias(datagramaInfoRecebido);
            }
        }
    }

    private void AtualizarMatrizAdjacencias(DatagramaInfo datagramaInfo)
    {
        int n = datagramaInfo.VetorDistancias.Length;

        for (int i = 0; i < n; i++)
        {
            _matrizAdjacencia[datagramaInfo.OrigemId, i] = datagramaInfo.VetorDistancias[i];

            int distanciaAntiga = _matrizAdjacencia[Id, i];
            int distanciaNova = datagramaInfo.VetorDistancias[i] + _matrizAdjacencia[datagramaInfo.OrigemId, i];

            if (distanciaNova < distanciaAntiga)
            {
                _matrizAdjacencia[Id, i] = distanciaNova;
                _distanciaAtualizada = true;
            }
        }

        if (_distanciaAtualizada)
        {
            EnviarDatagramaInfo();

            _distanciaAtualizada = false;
        }
    }

    private void EnviarDatagramaInfo()
    {
        int[] vetorDistancias = GetLinha(_matrizAdjacencia, Id);

        DatagramaInfo datagramaInfo = new DatagramaInfo(Id, vetorDistancias);

        foreach (IPEndPoint vizinho in vizinhos)
        {
            _canal.EnviarDatagramaInfo(datagramaInfo, vizinho);
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

    public void Fechar()
    {
        _tockenCancelamentoRecebimento.Dispose();

        _temporizadorRecebimento.Dispose();

        _canal.Fechar();
    }
}