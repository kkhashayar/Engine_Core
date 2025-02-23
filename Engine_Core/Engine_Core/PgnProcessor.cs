using System.Runtime.InteropServices;

namespace Engine_Core;

public static class PgnProcessor
{
    public static void LoadPGNFile(string filePath, int maxGamesToProcess)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: File {filePath} not found");
            return;
        }

        List<string> games = new List<string>();
        string gameMoves = "";
        int processedGames = 0;

        foreach (string line in File.ReadLines(filePath))
        {
            if (processedGames >= maxGamesToProcess)
                break;

            if (line.StartsWith("["))
                continue; // Skip metadata

            if (string.IsNullOrEmpty(line))
            {
                if (!string.IsNullOrEmpty(gameMoves))
                {
                    games.Add(gameMoves.Trim());
                    gameMoves = "";
                    processedGames++;
                }
                continue;
            }

            gameMoves += " " + line;
        }

        Console.WriteLine($"Loaded {games.Count} games from PGN (Processing {maxGamesToProcess} games).");

        foreach (string game in games)
        {
            ProcessGame(game);
        }
    }

    public static void ProcessGame(string game)
    {
        string[] tokens = game.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> moves = new List<string>();

        foreach (string token in tokens)
        {
            if (token.Contains(".") || token.Contains("{") 
                                    || token.Contains("}") 
                                    || token.Contains("[")
                                    || token.Contains("%")
                                    || token.Contains("]")) 
                continue;
            if (token == "1-0" || token == "0-1" || token == "1/2-1/2") break;

            moves.Add(token);
        }
        var testMoves = moves; 
        
        if (moves.Count < 2) return;

        IO.FenReader("");

        for (int i = 0; i < moves.Count - 1; i++)
        {
            string fen = IO.FenWriter();
            
            
            int bestMove = Globals.ConvertUciMoveToBitcode(moves[i]);

            if(bestMove != 0)
            {
                StoreTrainingData(fen, bestMove);
                Boards.ApplyTheMove(bestMove);
            }
        }
    }

    public static void StoreTrainingData(string fen, int bestMove)
    {
        TrainingEngine.AddTrainingSample(fen, bestMove);
    }
}
