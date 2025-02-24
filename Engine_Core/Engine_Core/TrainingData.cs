using Microsoft.ML.Data;

namespace Engine_Core;

public class TrainingData
{
    [LoadColumn(0)]
    public string FEN { get; set; }
    [LoadColumn(1)]
    public float BestMove { get; set; }
}


