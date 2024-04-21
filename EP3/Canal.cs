using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;


namespace EP3;

public class Canal
{
    private readonly Random _aleatorio = new Random();
    private readonly object _trava = new object();

    #region Socket

    private IPEndPoint _pontoConexaoLocal;

    private readonly UdpClient _socket = new UdpClient();

    #endregion

    #region Configs

    private int _probabilidadeEliminacao;
    private int _delayMilissegundos;
    private int _probabilidadeDuplicacao;
    private int _probabilidadeCorrupcao;
    private int _tamanhoMaximoBytes;

    #endregion

    #region Consolidação

    private uint _totalMensagensEnviadas;
    private uint _totalMensagensRecebidas;
    private uint _totalMensagensEliminadas;
    private uint _totalMensagensAtrasadas;
    private uint _totalMensagensDuplicadas;
    private uint _totalMensagensCorrompidas;
    private uint _totalMensagensCortadas;

    #endregion

    #region Criação Canal

    public Canal(int porta)
    {
        CarregarConfigs();

        _pontoConexaoLocal = new IPEndPoint(IPAddress.Any, porta); ;

        _socket.Client.Bind(_pontoConexaoLocal);
    }

    private void CarregarConfigs()
    {
        string json = File.ReadAllText(path: $@"{AppContext.BaseDirectory}/appsettings.json");

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;

            int porcentagemTaxaEliminacao = root.GetProperty("ProbabilidadeEliminacao").GetInt32();
            int delayMilissegundos = root.GetProperty("DelayMilissegundos").GetInt32();
            int porcentagemTaxaDuplicacao = root.GetProperty("ProbabilidadeDuplicacao").GetInt32();
            int porcentagemTaxaCorrupcao = root.GetProperty("ProbabilidadeCorrupcao").GetInt32();
            int tamanhoMaximoBytes = root.GetProperty("TamanhoMaximoBytes").GetInt32();

            _probabilidadeEliminacao = porcentagemTaxaEliminacao;
            _delayMilissegundos = delayMilissegundos;
            _probabilidadeDuplicacao = porcentagemTaxaDuplicacao;
            _probabilidadeCorrupcao = porcentagemTaxaCorrupcao;
            _tamanhoMaximoBytes = tamanhoMaximoBytes;
        }
    }

    #endregion

    #region Envio e Recebimento

    private byte[] DatagramaInfoParaByteArray(DatagramaInfo? datagramaInfo)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(datagramaInfo));
    }

    private DatagramaInfo? ByteArrayParaDatagramaInfo(byte[] byteArray)
    {
        return JsonSerializer.Deserialize<DatagramaInfo>(Encoding.UTF8.GetString(byteArray));
    }

    private void EnviarDatagramaInfo(byte[]? bytesDatagramaInfo, IPEndPoint pontoConexaoRemoto)
    {
        try
        {
            lock (_trava)
            {
                _totalMensagensEnviadas++;
            }

            if (bytesDatagramaInfo != null)
            {
                _socket.SendAsync(bytesDatagramaInfo, pontoConexaoRemoto);
            }
        }
        catch { }
    }

    public DatagramaInfo? ReceberDatagramaInfo(CancellationToken tokenCancelamento)
    {
        try
        {
            ValueTask<UdpReceiveResult> taskRecebimento = _socket.ReceiveAsync(tokenCancelamento);

            byte[] bytesDatagramaInfoRecebido = taskRecebimento.Result.Buffer;

            lock (_trava)
            {
                _totalMensagensRecebidas++;
            }

            return ByteArrayParaDatagramaInfo(bytesDatagramaInfoRecebido);
        }
        catch (JsonException)
        {
            Console.WriteLine("Mensagem corrompida recebida descartada");

            return null;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Aplicação de Propiedades

    public void ProcessarMensagem(DatagramaInfo datagramaInfo, IPEndPoint pontoConexaoRemoto)
    {
        if (DeveriaAplicarPropriedade(_probabilidadeEliminacao))
        {
            _totalMensagensEliminadas++;

            EnviarDatagramaInfo(null, pontoConexaoRemoto);

            Console.WriteLine($"Mensagem eliminada.");

            return;
        }

        byte[] bytesDatagramaInfo = DatagramaInfoParaByteArray(datagramaInfo);

        if (DeveriaAplicarPropriedade(_probabilidadeDuplicacao))
        {
            _totalMensagensDuplicadas++;

            EnviarDatagramaInfo(bytesDatagramaInfo, pontoConexaoRemoto);

            Console.WriteLine($"Mensagem duplicada.");
        }

        if (DeveriaAplicarPropriedade(_probabilidadeCorrupcao))
        {
            CorromperSegmento(ref bytesDatagramaInfo);
            _totalMensagensCorrompidas++;
            Console.WriteLine($"Mensagem corrompida.");
        }

        if (bytesDatagramaInfo.Length > _tamanhoMaximoBytes)
        {
            CortarSegmento(ref bytesDatagramaInfo);
            _totalMensagensCortadas++;
            Console.WriteLine($"Mensagem cortada.");
        }

        if (_delayMilissegundos != 0)
        {
            Thread.Sleep(_delayMilissegundos);
            _totalMensagensAtrasadas++;
            Console.WriteLine($"Mensagem atrasada.");
        }

        EnviarDatagramaInfo(bytesDatagramaInfo, pontoConexaoRemoto);
    }

    private bool DeveriaAplicarPropriedade(int probabilidade)
    {
        return _aleatorio.Next(minValue: 1, maxValue: 101) <= probabilidade;
    }

    private void CorromperSegmento(ref byte[] segmento)
    {
        int indice = _aleatorio.Next(minValue: 0, maxValue: segmento.Length);

        segmento[indice] = (byte)(~segmento[indice]); ;
    }

    private void CortarSegmento(ref byte[] segmento)
    {
        Array.Resize(ref segmento, _tamanhoMaximoBytes);
    }

    #endregion

    #region Finalização

    public void Fechar()
    {
        //ConsolidarResultados();

        _socket.Close();

        _socket.Dispose();
    }

    private void ConsolidarResultados()
    {
        Console.WriteLine(value: $"\n" +
                                 $"\nTotal de mensagens enviadas: {_totalMensagensEnviadas}" +
                                 $"\nTotal de mensagens recebidas: {_totalMensagensRecebidas}" +
                                 $"\nTotal de mensagens eliminadas: {_totalMensagensEliminadas}" +
                                 $"\nTotal de mensagens atrasadas: {_totalMensagensAtrasadas}" +
                                 $"\nTotal de mensagens duplicadas: {_totalMensagensDuplicadas}" +
                                 $"\nTotal de mensagens corrompidas: {_totalMensagensCorrompidas}" +
                                 $"\nTotal de mensagens cortadas: {_totalMensagensCortadas}");
    }

    #endregion
}

#region Classe Threads

//public class Threads
//{
//    private Canal _canal;

//    private CancellationTokenSource _tockenCancelamentoRecebimento = new CancellationTokenSource();

//    private static int _timeoutMilissegundos = 500;
//    private static Timer _temporizadorRecebimento = new Timer(_timeoutMilissegundos);

//    public Threads(IPEndPoint pontoConexaoLocal, IPEndPoint pontoConexaoRemoto)
//    {
//        _canal = new Canal(pontoConexaoLocal, pontoConexaoRemoto);
//    }

//    public Threads(IPEndPoint pontoConexaoLocal)
//    {
//        _canal = new Canal(pontoConexaoLocal);
//    }

//    #region Envio e Recebimento

//    public void EnviarSegmento(SegmentoConfiavel segmento)
//    {
//        _canal.ProcessarMensagem(segmento);
//    }

//    public SegmentoConfiavel? ReceberSegmento()
//    {
//        SegmentoConfiavel? segmentoRecebido;

//        segmentoRecebido = _canal.ReceberSegmento(_tockenCancelamentoRecebimento.Token);

//        if (_tockenCancelamentoRecebimento.Token.IsCancellationRequested)
//        {
//            _tockenCancelamentoRecebimento = new CancellationTokenSource();
//        }

//        return segmentoRecebido;
//    }

//    public void CancelarRecebimento()
//    {
//        _tockenCancelamentoRecebimento.Cancel();
//    }

//    #endregion

//    #region Timeout

//    public void ConfigurarTemporizador(ElapsedEventHandler evento)
//    {
//        _temporizadorRecebimento.Elapsed += evento;
//        _temporizadorRecebimento.AutoReset = true;
//    }

//    public void IniciarTemporizador()
//    {
//        _temporizadorRecebimento.Enabled = true;
//    }

//    public void PararTemporizador()
//    {
//        _temporizadorRecebimento.Stop();
//    }

//    public void Fechar()
//    {
//        _tockenCancelamentoRecebimento.Dispose();

//        _temporizadorRecebimento.Dispose();

//        _canal.Fechar();
//    }

//    #endregion
//}

#endregion