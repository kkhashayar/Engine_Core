


// Entry point for using WinBoard or low-level engine functions.
using Engine_Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;


void InitAll()
{
    Attacks.InitLeapersAttacks();
    Attacks.InitBishopsAttacks();
    Attacks.InitRooksAttacks();
}

List<string> GameHistory = new List<string>();  

// Temporary solution 
bool running = true;
void PlayThePosition()
{
    while (running)
    {
        
        Thread.Sleep(2000);
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("  Calculating...");
        Console.WriteLine();
        Boards.DisplayBoard();
        int move = 0;

        // move = Search.GetBestMove(8);
        move = Search.GetBestMoveWithIterativeDeepening(10, 5); 
        
        Console.Beep(1000, 500);
        
        if(MoveGenerator.GetMoveStartSquare(move) == MoveGenerator.GetMoveTarget(move))break;
        
        Boards.ApplyTheMove(move);
        
        GameHistory.Add(Globals.MoveToString(move)+" ");
        
        if (Boards.whiteCheckmate || Boards.blackCheckmate) break;
        
        Boards.DisplayBoard();
         
        //Console.ReadKey();  
    }

    if (Boards.whiteCheckmate)
    {
        Console.WriteLine("White checkmate");
    }

    else if (Boards.blackCheckmate)
    {
        Console.WriteLine("Black checkmate");
    }
    else
    {
        Console.WriteLine("Position solved");
    }
    Boards.DisplayBoard();
    foreach (var move in GameHistory)
    {
        Console.Write(move);
    }
}


Run();
void Run()
{
    InitAll();

    //IO.FenReader("8/1pB1rnbk/6pn/7q/P3B2P/1P6/6P1/2Q1R2K b - - 0 1");
    //Boards.DisplayBoard();

    //PerftTeste.RunPerft(5, true);

    //PlayThePosition();

    // DebugSearchMethods();


    //*********************************  SML FLOW TEST  *********************************// 
    string pgnFilePath = @"D:\Data\Chess-Data_ML\lichess_db_standard_rated_2025-01.pgn";
    string outputFilePath = @"D:\Data\Chess-Data_ML\Training_Result\training_data.txt";

    PgnProcessor.LoadPGNFile(pgnFilePath, 100000);

    // Saving the  extracted training data
    //TrainingEngine.SaveTrainingData(outputFilePath);        




    // WinBoardLoop();

}

static void DebugSearchMethods()
{
    MoveObjects moveList = new MoveObjects();
    MoveGenerator.GenerateMoves(moveList);
    MoveGenerator.PrintMoveList(moveList);

    Console.WriteLine();
    Console.WriteLine("********************************************************************");
    Console.WriteLine();
    Console.WriteLine("Scorring capture moves: ");
    foreach (var move in moveList.moves.Take(moveList.counter))
    {
        Console.WriteLine(Search.ScoreMove(move));
    }
    Console.WriteLine();
    Console.WriteLine("********************************************************************");
    Console.WriteLine();
    Console.WriteLine("Sorting scored capture moves: ");

    Search.SortMoves(moveList);


    foreach (var move in moveList.moves.Take(moveList.counter))
    {
        Console.WriteLine(Search.ScoreMove(move));
    }

    Console.WriteLine();
    Console.WriteLine("********************************************************************");
    Console.WriteLine();
    Console.WriteLine("Sorting Only scored capture moves: ");

    foreach (int move in moveList.moves.Take(moveList.counter))
    {

        if (MoveGenerator.GetMoveCapture(move))
        {
            MoveGenerator.PrintMove(move);
            Console.WriteLine(Search.ScoreMove(move));
            Console.WriteLine();
        }

        Search.killerMoves[0, 1] = moveList.moves[0];
    }
    Console.WriteLine();
    Console.WriteLine("********************************************************************");
    Console.WriteLine();
    Console.WriteLine("Adding killer moves: ");
    var score1 = Search.killerMoves[0, 0] = moveList.moves[2];
    var score2 = Search.killerMoves[0, 1] = moveList.moves[3];
    MoveGenerator.PrintMove(moveList.moves[2]);
    Console.WriteLine(score1);
    MoveGenerator.PrintMove(moveList.moves[3]);
    Console.WriteLine(score2);
}

// Possible bug postion = rnb1kbnr/ppp2ppp/8/8/3qp3/2N5/PPP2PPP/R1BQKBNR w KQkq - 0 
// 6k1/5p1p/2Q1p1p1/5n1r/N7/1B3P1P/1PP3PK/4q3 b - - 0 1                mate in 3
// rn4k1/pp1r1pp1/1q1b4/5QN1/5N2/4P3/PP3PPP/3R1RK1 w - - 1 0           mate in 3
// r1b1rk2/ppq3p1/2nbpp2/3pN1BQ/2PP4/7R/PP3PPP/R5K1 w - - 1 0          mate in 4
// br1qr1k1/b1pnnp2/p2p2p1/P4PB1/3NP2Q/2P3N1/B5PP/R3R1K1 w - - 1 0     mate in 4
// rn3rk1/pbppq1pp/1p2pb2/4N2Q/3PN3/3B4/PPP2PPP/R3K2R w KQ - 7 11      mate in 7

// 8/1pB1rnbk/6pn/7q/P3B2P/1P6/6P1/2Q1R2K b - - 0 1                    mate in 10
// rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
//empty_board "8/8/8/8/8/8/8/8 b - - "
//start_position "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 "
//tricky_position "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1 "
//killer_position "rnbqkb1r/pp1p1pPp/8/2p1pP2/1P1P4/3P3P/P1P1P3/RNBQKBNR w KQkq e6 0 1"
//cmk_position "r2q1rk1/ppp2ppp/2n1bn2/2b1p3/3pP3/3P1NPP/PPP1NPB1/R1BQ1RK1 b - - 0 9 "

// Console.WriteLine("Size: " + (Boards.OccupanciesBitBoards.Length * sizeof(ulong)));
// need this variables to restore the game state    
// ulong[] bitboardsCopy, occupanciesCopy;
// Colors sideCopy;
// int castlePermCopy, enpassantSquareCopy;
// MoveGenerator.CopyGameState(out bitboardsCopy, out occupanciesCopy, out sideCopy, out castlePermCopy, out enpassantSquareCopy);

// restore the game state   
// MoveGenerator.RestoreGameState(bitboardsCopy, occupanciesCopy, sideCopy, castlePermCopy, enpassantSquareCopy);
// */

static void WinBoardLoop()
{
    
    using (var log = new StreamWriter("Engine_Logs.txt", append: true) { AutoFlush = true })
    {
        log.WriteLine($"[{DateTime.Now}] Engine started.");

        try
        {
            // Prepare I/O
            Console.SetIn(new StreamReader(Console.OpenStandardInput(), System.Text.Encoding.Default, false, 1024));
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

            string initialMessage = "# WinBoard engine ready";
            Console.WriteLine(initialMessage);
            log.WriteLine($"Sent: {initialMessage}");

            bool forceMode = false;  // Engine won't auto-move in force mode
            bool engineGo = true;    // Engine auto-moves if true
            int depth = 8;           // Default search depth
            int remainingTime = 0;   // Time remaining for engine (centiseconds)
            int opponentTime = 0;    // Time remaining for opponent

            while (true)
            {
                string input = Console.ReadLine();
                
                if (string.IsNullOrEmpty(input)) continue;

                log.WriteLine($"Received: {input}");

                var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = tokens[0].ToLower();

                switch (command)
                {

                    case "status":
                        {
                            string response = "# Status command acknowledged";
                            Console.WriteLine(response);
                            log.WriteLine($"Sent: {response}");
                            break;
                        }

                    case "xboard":
                        {
                            string response = "# WinBoard engine ready";
                            Console.WriteLine(response);
                            log.WriteLine($"Sent: {response}");
                            break;
                        }

                    case "protover":
                        {
                            string feature1 = "feature sigint=0 name=1 myname=\"K-Chess\"";
                            string feature2 = "feature done=1";
                            Console.WriteLine(feature1);
                            log.WriteLine($"Sent: {feature1}");
                            Console.WriteLine(feature2);
                            log.WriteLine($"Sent: {feature2}");
                            break;
                        }

                    case "new":
                        {
                            IO.FenReader("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
                            forceMode = false;
                            engineGo = true;
                            string response = "# New game started";
                            Console.WriteLine(response);
                            log.WriteLine($"Sent: {response}");
                            break;
                        }

                    case "force":
                        {
                            forceMode = true;
                            engineGo = false;
                            string response = "# Force mode enabled";
                            Console.WriteLine(response);
                            log.WriteLine($"Sent: {response}");
                            break;
                        }

                    case "go":
                        {
                            forceMode = false;
                            engineGo = true;
                            string response = "# Engine is thinking...";
                            Console.WriteLine(response);
                            log.WriteLine($"Sent: {response}");
                            MakeEngineMove(depth, log);
                            break;
                        }

                    // Still using too much time in depth 8
                    case "time":
                        {
                            if (tokens.Length > 1 && int.TryParse(tokens[1], out int time))
                            {
                                remainingTime = time;
                                string response = $"# Time updated: {remainingTime} centiseconds";
                                Console.WriteLine(response);
                                log.WriteLine($"Sent: {response}");
                            }
                            break;
                        }

                    case "otim":
                        {
                            if (tokens.Length > 1 && int.TryParse(tokens[1], out int otim))
                            {
                                opponentTime = otim;
                                string response = $"# Opponent time updated: {opponentTime} centiseconds";
                                Console.WriteLine(response);
                                log.WriteLine($"Sent: {response}");
                            }
                            break;
                        }

                    // Using it to handle user input and other engine moves
                    case "usermove":
                        {
                            if (tokens.Length > 1)
                            {
                                HandleMove(tokens[1], forceMode, engineGo, depth, log);
                            }
                            break;
                        }

                    case "result":
                        {
                            if (tokens.Length > 1)
                            {
                                string response = $"# Game result: {tokens[1]}";
                                Console.WriteLine(response);
                                log.WriteLine($"Sent: {response}");
                            }
                            break;
                        }

                    case "ping":
                        {
                            if (tokens.Length > 1)
                            {
                                string response = $"pong {tokens[1]}";
                                Console.WriteLine(response);
                                log.WriteLine($"Sent: {response}");
                            }
                            break;
                        }

                    case "post":
                    case "hard":
                    case "easy":
                    case "computer":
                    case "name":
                    case "random":
                    case "level":
                        {
                            string response = $"# {command} command acknowledged";
                            Console.WriteLine(response);
                            log.WriteLine($"Sent: {response}");
                            break;
                        }

                    default:
                        {
                            
                            if (input.StartsWith("a") ||
                               input.StartsWith("b")  ||
                               input.StartsWith("c")  ||
                               input.StartsWith("d")  ||
                               input.StartsWith("e")  ||
                               input.StartsWith("f")  ||
                               input.StartsWith("g")  ||
                               input.StartsWith("h"))
                            {
                                HandleMove(input, forceMode, engineGo, depth, log);
                            }

                            else if (!input.StartsWith("#"))
                            {
                                string response = $"# Unknown or unhandled command: {input}";
                                Console.WriteLine(response);
                                log.WriteLine($"Sent: {response}");
                            }
                            break;
                        }
                }
            }
        }
        catch (Exception ex)
        {
            string errorMessage = $"# Exception: {ex.Message}";
            Console.Error.WriteLine(errorMessage);
            log.WriteLine($"Error: {ex}");
            Environment.Exit(1);
        }
    }
}


static void HandleMove(string moveString, bool forceMode, bool engineGo, int depth, StreamWriter log)
{
    try
    {
        int move = WinBoardParseMove(moveString);
        if (move != 0)
        {
            Boards.ApplyTheMove(move); // Update internal board state
            log.WriteLine($"Move applied: {moveString}");

            if (!forceMode && engineGo)
            {
                MakeEngineMove(depth, log); // Respond with engine's move
            }
        }
        else
        {
            string invalidMoveMsg = "# Invalid move received";
            Console.WriteLine(invalidMoveMsg);
            log.WriteLine($"Sent: {invalidMoveMsg}");
        }
    }
    catch (Exception ex)
    {
        string errorMsg = $"# HandleMove Exception: {ex.Message}";
        Console.Error.WriteLine(errorMsg);
        log.WriteLine($"Error in HandleMove: {ex}");
    }
}

// Parses WinBoard move strings (e.g., e2e4) into move data
static int WinBoardParseMove(string moveString)
{
    if (moveString.Length < 4) return 0;

    int sourceFile = moveString[0] - 'a';
    int sourceRank = 8 - (moveString[1] - '0');
    int sourceSquare = sourceRank * 8 + sourceFile;

    int targetFile = moveString[2] - 'a';
    int targetRank = 8 - (moveString[3] - '0');
    int targetSquare = targetRank * 8 + targetFile;

    char promotion = moveString.Length == 5 ? moveString[4] : '\0';

    var moveObjects = new MoveObjects();
    MoveGenerator.GenerateMoves(moveObjects);

    foreach (int move in moveObjects.moves.Take(moveObjects.counter))
    {
        if (MoveGenerator.GetMoveStartSquare(move) == sourceSquare &&
            MoveGenerator.GetMoveTarget(move) == targetSquare)
        {
            int promotedPiece = MoveGenerator.GetMovePromoted(move);
            if (promotedPiece != 0 && promotion != '\0')
            {
                return move; // Valid promotion move
            }
            if (promotedPiece == 0 && promotion == '\0')
            {
                return move; // Valid non-promotion move
            }
        }
    }
    return 0;
}
// Generates and sends the engine's best move
static void MakeEngineMove(int depth, StreamWriter log)
{
    try
    {
        int bestMove = Search.GetBestMove(depth);
        if (bestMove != 0)
        {
            Boards.ApplyTheMove(bestMove); // Update board state
            string moveStr = Globals.MoveToString(bestMove);
            string response = $"move {moveStr}";
            Console.WriteLine(response);
            log.WriteLine($"Sent: {response}");
        }
        else
        {
            string noMoveMsg = "# No valid moves found.";
            Console.WriteLine(noMoveMsg);
            log.WriteLine($"Sent: {noMoveMsg}");
        }
    }
    catch (Exception ex)
    {
        string errorMsg = $"# MakeEngineMove Exception: {ex.Message}";
        Console.Error.WriteLine(errorMsg);
        log.WriteLine($"Error in MakeEngineMove: {ex}");
    }
}

