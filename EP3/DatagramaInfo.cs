namespace EP3;

public class DatagramaInfo
{
    public int Origem { get; set; }
    public int[] VetorDistancias { get; set; }

    public DatagramaInfo(int origem, int[] vetorDistancias)
    {
        Origem = origem;
        VetorDistancias = vetorDistancias;
    }
}
