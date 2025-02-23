namespace Engine_Core;

public static class TrainingEngine
{
    private static string outputFilePath = @"D:\Data\Chess-Data_ML\Training_Result\training_data.txt";
    private static List<(string FEN, int BestMove)> trainingData = new List<(string, int)>();
    private static HashSet<string> UniqueFens = new HashSet<string>();  // too lookup  positions if already have them or not
    private const int BatchSize = 10000; // size of each save entries
    public static void AddTrainingSample(string fen, int bestMove)
    {
        // Check and add only if fen is not already stored 
        if (!UniqueFens.Contains(fen))
        {
            UniqueFens.Add(fen);
            trainingData.Add((fen, bestMove));  

            // save periodically to avoid memory loss and excessive memory usage
            if(trainingData.Count >= BatchSize)
            {
                SaveTrainingData(outputFilePath);
                trainingData.Clear();   
                UniqueFens.Clear(); 
            }
        }


        
    }

    public static void SaveTrainingData(string filePath)
    {
        if(trainingData.Count == 0) return;  // No more data to process 

        string folderPath = Path.GetDirectoryName(filePath);
        File.WriteAllLines(filePath, trainingData.Select(data => $"{data.FEN},{data.BestMove}"));

        Console.WriteLine($"Save {BatchSize} training sample to {outputFilePath}");
    }
}
