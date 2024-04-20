using System.Timers;
using Timer = System.Timers.Timer;


namespace EP3;

public class Roteador
{
    public int Id { get; private set; }

    private bool _roteadorAtivo = true;

    private Canal _canal;

    private int[,] _matrizAdjacencia;

    private const int OffsetPorta = 10000;
    private const int Infinito = int.MaxValue;

    private const int _timeoutMilissegundos = 30000;
    private CancellationTokenSource _tockenCancelamentoRecebimento = new CancellationTokenSource();
    private Timer _temporizadorRecebimento = new Timer(_timeoutMilissegundos);
    private ElapsedEventHandler _evento;
    
    public Roteador(int id, int[] vetorDistancias)
    {
        this.Id = id;

        _canal = new Canal(OffsetPorta + id);

        InicializarMatriz(vetorDistancias);

        _evento = new ElapsedEventHandler(TemporizadorEncerrado);

        _temporizadorRecebimento.Elapsed += _evento;
        _temporizadorRecebimento.AutoReset = true;
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
        for (int i = 0; i < datagramaInfo.VetorDistancias.Length; i++)
        {
            _matrizAdjacencia[datagramaInfo.Origem, i] = datagramaInfo.VetorDistancias[i];
        }

        int n = _matrizAdjacencia.GetLength(dimension: 0); // Número de roteadores na rede

        

        // Para cada roteador na rede
        for (int i = 0; i < n; i++)
        {
            // Para cada destino
            for (int j = 0; j < n; j++)
            {
                // Se o destino não for o mesmo roteador
                if (i != j)
                {
                    // Atualiza a entrada na matriz de adjacências
                    matrizAdjacencias[i, j] = Math.Min(matrizAdjacencias[i, j], vetorDistancias[i] + matrizAdjacencias[i, j]);
                }
            }
        }
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