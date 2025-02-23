namespace Engine_Core;

public static class TrainingEngine
{
    private static List<(string FEN, int BestMove)> trainingData = new List<(string, int)>();

    public static void AddTrainingSample(string fen, int bestMove)
    {
        trainingData.Add((fen, bestMove));
    }

    public static void SaveTrainingData(string filePath)
    {
        string folderPath = Path.GetDirectoryName(filePath);
        File.WriteAllLines(filePath, trainingData.Select(data => $"{data.FEN},{data.BestMove}"));
    }
}
