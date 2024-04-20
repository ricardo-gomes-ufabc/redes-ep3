namespace EP3;

public class DatagramaInfo
{
    public int OrigemId { get; set; }
    public int[] VetorDistancias { get; set; }

    public DatagramaInfo(int origemId, int[] vetorDistancias)
    {
        OrigemId = origemId;
        VetorDistancias = vetorDistancias;
    }
}
