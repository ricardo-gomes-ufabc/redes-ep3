namespace EP3;

public class DatagramaInfo
{
    public int Origem { get; set; }
    public int Destino { get; set; }
    public int[] VetorDistancias { get; set; }

    public DatagramaInfo(int origem, int destino, int[] vetorDistancias)
    {
        Origem = origem;
        Destino = destino;
        VetorDistancias = vetorDistancias;
    }
}
