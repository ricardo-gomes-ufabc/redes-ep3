using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EP3;

public class Roteador
{
    public int id { get; private set; }
    private int[,] matrizAdjacencia;
    private int[] vetorDistancias;
    private UdpClient udpClient;
    private const int PORT_OFFSET = 10000;
    private const int INFINITO = int.MaxValue;

    public Roteador(int id)
    {
        this.id = id;
    }

    public void Inicializar()
    {
        LerMatrizAdjacencia();

        udpClient = new UdpClient(id + PORT_OFFSET);
        Thread thread = new Thread(ReceberMensagens);
        thread.Start();

        EnviarMensagens();
    }

    private void LerMatrizAdjacencia()
    {
        string[] linhas = File.ReadAllLines("matriz_adjacencia.txt");
        int tamanho = linhas.Length;
        matrizAdjacencia = new int[tamanho, tamanho];
        vetorDistancias = new int[tamanho];

        for (int i = 0; i < tamanho; i++)
        {
            string[] valores = linhas[i].Split(' ');
            for (int j = 0; j < tamanho; j++)
            {
                matrizAdjacencia[i, j] = int.Parse(valores[j]);
            }
        }
    }

    private void EnviarMensagens()
    {
        while (true)
        {
            for (int i = 0; i < matrizAdjacencia.GetLength(0); i++)
            {
                if (matrizAdjacencia[id, i] > 0 && matrizAdjacencia[id, i] != INFINITO)
                {
                    DatagramaInfo datagrama = new DatagramaInfo(id, i, vetorDistancias);
                    EnviarDatagrama(datagrama);
                }
            }
            Thread.Sleep(5000); // Envia a cada 5 segundos
        }
    }

    private void EnviarDatagrama(DatagramaInfo datagrama)
    {
        byte[] bytes = SerializarDatagrama(datagrama);
        udpClient.Send(bytes, bytes.Length, "127.0.0.1", datagrama.Destino + PORT_OFFSET);
        Console.WriteLine($"Datagrama enviado de {datagrama.Origem} para {datagrama.Destino}: {string.Join(", ", datagrama.VetorDistancias)}");
    }

    private void ReceberMensagens()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            byte[] bytes = udpClient.Receive(ref remoteEndPoint);
            DatagramaInfo datagrama = DeserializarDatagrama(bytes);
            AtualizarTabelaRoteamento(datagrama);
        }
    }

    private byte[] SerializarDatagrama(DatagramaInfo datagrama)
    {
        string data = $"{datagrama.Origem},{datagrama.Destino},{string.Join(",", datagrama.VetorDistancias)}";
        return Encoding.ASCII.GetBytes(data);
    }

    private DatagramaInfo DeserializarDatagrama(byte[] bytes)
    {
        string data = Encoding.ASCII.GetString(bytes);
        string[] partes = data.Split(',');
        int origem = int.Parse(partes[0]);
        int destino = int.Parse(partes[1]);
        int[] vetorDistancias = Array.ConvertAll(partes[2].Split(','), int.Parse);
        return new DatagramaInfo(origem, destino, vetorDistancias);
    }

    private void AtualizarTabelaRoteamento(DatagramaInfo datagrama)
    {
        bool atualizou = false;
        for (int i = 0; i < datagrama.VetorDistancias.Length; i++)
        {
            int novaDistancia = datagrama.VetorDistancias[datagrama.Origem] + datagrama.VetorDistancias[i];
            if (novaDistancia < vetorDistancias[i])
            {
                vetorDistancias[i] = novaDistancia;
                atualizou = true;
            }
        }

        if (atualizou)
        {
            Console.WriteLine($"Tabela de roteamento atualizada pelo roteador {datagrama.Origem}.");
            ImprimirTabelaRoteamento();
        }
    }

    private void ImprimirTabelaRoteamento()
    {
        Console.WriteLine($"Tabela de roteamento do roteador {id}:");
        for (int i = 0; i < vetorDistancias.Length; i++)
        {
            Console.WriteLine($"Para o roteador {i}: Distância = {vetorDistancias[i]}");
        }
    }

    public void Finalizar()
    {
        Console.WriteLine($"O roteador {id} finalizou o processo.");
    }

    public void AlterarLink(int origem, int destino, int novoPeso)
    {
        matrizAdjacencia[origem, destino] = novoPeso;
        vetorDistancias[destino] = novoPeso;
        EnviarMensagens();
    }

    public void RemoverRoteador(int idRoteador)
    {
        for (int i = 0; i < matrizAdjacencia.GetLength(0); i++)
        {
            matrizAdjacencia[idRoteador, i] = INFINITO;
            matrizAdjacencia[i, idRoteador] = INFINITO;
        }
        vetorDistancias[idRoteador] = INFINITO;
        EnviarMensagens();
    }

    public int[] ObterVetorDistancias()
    {
        return vetorDistancias;
    }
}