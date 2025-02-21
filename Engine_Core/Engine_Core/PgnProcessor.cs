namespace Engine_Core;

public static class PgnProcessor
{
    public static void LoadPGNFile(string filePath)
    {
        // Checks if file exists in give path
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: File {filePath} not found");
            return; 
        }

        List<string> games = new List<string>();
        string gameMoves = "";


        int counter = 0; 
        foreach(string line in File.ReadLines(filePath))
        {
            counter++;  
            if (line.StartsWith("["))
            {
                // ignoring metadata like event etc..
                continue;
            }
            if (string.IsNullOrEmpty(line))
            {
                if (!string.IsNullOrEmpty(gameMoves))
                {
                    games.Add(gameMoves.Trim());
                    gameMoves = "";
                }
                continue;
                
            }
            Console.WriteLine($"{counter} Game added..");
            gameMoves += " " + line;
        }

        Console.WriteLine($"Loaded {games.Count} games fro, PGN");
        return ;
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
            if (token.Contains(".")) continue;
            if (token == "1-0" || token == "0-1" || token == "1/2-1/2") break;

            moves.Add(token);
        }

        if (moves.Count < 2) return;

        // Reset board to starting position
        IO.FenReader("");

        //for (int i = 0; i < moves.Count - 1; i++)
        //{
        //    // Extract FEN before making the move
        //    string fen = IO.FenWriter();

        //    // Convert UCI move to bitcoded move
        //    int bestMove = Globals.ConvertUciMoveToBitcode(moves[i]);

        //    // Store training data
        //    //StoreTrainingData(fen, bestMove);

        //    // Apply move to update board position
        //    Boards.ApplyTheMove(bestMove);
        //}
    }

    //public static void StoreTrainingData(string fen, int bestMove)
    //{
    //    TrainingEngine.AddTrainingSample(fen, bestMove);
    //}

}
