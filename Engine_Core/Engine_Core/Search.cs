using Engine;
using System.Numerics;
using static Engine_Core.Enumes;

namespace Engine_Core;
public struct Transposition
{
    public ulong position;
    public int depth;
    public int score;
}

public static class Search
{
    public static bool GamePhaseOppening = false;
    public static int NumberOfAllPieces { get; set; }
    public static int DynamicDepth { get; set; }// TODO: Implement Phase detection
    public static int MaxSearchTime { get; set; }
    
    // Search config switches 
    public static bool TranspositionSwitch = false;
    public static bool TimeLimitDeepeningSwitch = false;
    public static bool EarlyExitSwitch = false;

    public static Dictionary<ulong, Transposition> transpositionTable = new Dictionary<ulong, Transposition>(); 
  
    // Variables needed for late move reduction 
    private static int FullDepthMoves = 4;
    private static int ReductionLimit = 3;


    public static List<int> ExecutablePv = new List<int>();
    public static long nodes;

    public static int maxPly = 64;

    public static int[,] killerMoves = new int[2, maxPly];
    public static int[,] historyMoves = new int[12, 64];

    // Triangular principal variation table
    public static int[] pvLength = new int[maxPly];
    public static int[,] pvTable = new int[maxPly, maxPly];

    // Half-move (ply) counter
    public static int ply;

    // A typical big negative/positive bound for mate scores
    private const int NEG_INF = -50000;
    private const int POS_INF = 50000;


    // **********************************************   ZOBRIST  HASHING 

    // Random piece keys [piece, squar]  give a random unique number to piece on given square
    public static ulong[,] pieceKeysOnSquare = new ulong[12, 64];

    // Random En-passant key and square 
    public static ulong[] enpassantKey = new ulong[64];

    // Random Side to play key 

    public static ulong sideKey;

    // Random castling keys 
    public static ulong[] castlingKeys = new ulong[16];

    // Almost unique position identifier hash key  / position key 
    public static ulong positionHashKey;

    // Set it to public for testing 
    public static void InitializeRandomKeys()
    {
        for (Pieces piece = (int)Pieces.P; (int)piece <= (int)Pieces.k; piece++)
        {
            for (int square = 0; square < 64; square++)
            {
                pieceKeysOnSquare[(int)piece, square] = Globals.GetFixedRandom64Numbers();
            }
        }

        // En-passant key 
        for (int square = 0; (int)square < 64; square++)
        {
            enpassantKey[square] = Globals.GetFixedRandom64Numbers();
        }

        // Side key 
        sideKey = Globals.GetFixedRandom64Numbers();

        // Castling keys 
        for (int index = 0; (int)index < 16; index++)
        {
            castlingKeys[index] = Globals.GetFixedRandom64Numbers();
        }
    }

    // Generate hash key. 
    public static ulong GeneratepositionHashKey()
    {
        positionHashKey = 0;
        // Temp board 
        ulong pieceBitboard;
        int square = 0;
        // loop over pieces 
        for (int piece = (int)Pieces.P; (int)piece <= (int)Pieces.k; piece++)
        {
            pieceBitboard = Boards.Bitboards[piece];

            while (pieceBitboard != 0)
            {
                // init square occupied by piece 
                square = Globals.GetLs1bIndex(pieceBitboard);
                Globals.PopBit(ref pieceBitboard, square);

                // Testing piece positions
                //Console.WriteLine($"Piece: {Globals.SquareToCoordinates[square]}");

                // adding piece hash to position hash!
                positionHashKey ^= pieceKeysOnSquare[piece, square];
            }
        }

        // En-passant 
        if (enpassantKey[square] != (ulong)Enumes.Squares.NoSquare)
        {
            positionHashKey ^= enpassantKey[square];
        }
        // Castling
        positionHashKey ^= castlingKeys[Boards.CastlePerm];

        // Hashing the side only if black is to move
        if (Boards.Side == (int)Colors.black)
        {
            positionHashKey ^= sideKey;
        }
         
        return positionHashKey;
    }

    private static int CountPieces()
    {
        int total = 0; 
        for(int piece = 0; piece < Boards.Bitboards.Length; piece++)
        {
            total += BitOperations.PopCount(Boards.Bitboards[piece]);
        }
        return total;
    }

    // Negamax call with iterative deepening 
    public static int GetBestMoveWithIterativeDeepening(int maxTimeSeconds)
    {
        maxTimeSeconds = GetGamePhase(maxTimeSeconds);
        
        int maxDepth = DynamicDepth;
        int score = 0;
        nodes = 0;
        ply = 0;
        int bestScore = 0;
        int bestMove = 0;
        var startTime = DateTime.UtcNow;

        ClearKillerAndHistoryMoves();
        ClearPV();


        if (TranspositionSwitch) GeneratepositionHashKey();

        for (int currentDepth = 1; currentDepth <= maxDepth; currentDepth++)
        {

            nodes = 0;

            var depthStartTime = DateTime.UtcNow;

            score = Negamax(-50000, 50000, currentDepth);

            Console.WriteLine("info depth " + currentDepth + " score " + score + " nodes " + nodes + " pv " + PrintPVLine());

            ExecutablePv.Clear();
            for (int i = 0; i < pvLength[0]; i++)
            {
                ExecutablePv.Add(pvTable[0, i]);
            }

            if (EarlyExitSwitch)
            {
                // It's a forced mate, But I am not sure the effect of this in more strategic positions. 
                if (Math.Abs(score) >= 48000) // Found a forced mate!
                {
                    bestMove = pvTable[0, 0]; // Store the best move
                    Console.WriteLine($"info string Found forced mate at depth {currentDepth}. Stopping search.");
                    return bestMove; // Immediately return the best move.
                }
            }


            if (score > bestScore)
            {
                bestScore = score;
                bestMove = pvTable[0, 0];
            }

            if (TimeLimitDeepeningSwitch)
            {
                // Looks like in some of my tests, engine works better without this feature.
                if ((DateTime.UtcNow - depthStartTime).TotalSeconds >= maxTimeSeconds)
                {
                    Console.WriteLine($"info string Depth {currentDepth} took too long ({maxTimeSeconds}s), going deeper.");
                    continue;
                }
            }

            // If total max time is exceeded, stop completely
            if ((DateTime.UtcNow - startTime).TotalSeconds >= maxTimeSeconds * maxDepth)
            {
                Console.WriteLine($"info string Max time reached ({maxTimeSeconds * maxDepth}s). Stopping search.");
                return bestMove;

            }
        }
        nodes = 0; // Reset nodes counter
        ClearKillerAndHistoryMoves();
        ClearPV();

        // Final Negamax search at maxDepth
        score = Negamax(-50000, 50000, maxDepth);

        // Output final search information
        Console.WriteLine($"info score cp {score} depth {maxDepth} nodes {nodes} pv {PrintPVLine()}");

        // Determine and output the best move based on the final search
        bestMove = pvTable[0, 0]; // Update bestMove based on the final PV
        Console.WriteLine($"bestmove {Globals.MoveToString(bestMove)}");



        return bestMove;
    }

    // TODO: Find a way to return game phase first , time and other parameters should be adjusted based on game phase.
    private static int GetGamePhase(int maxTimeSeconds)
    {
        var defaultTime = maxTimeSeconds;
        NumberOfAllPieces = CountPieces();

        
        //if(MoveGenerator.wq == 0 && MoveGenerator.bq == 0)
        //{
        //    // endgame phase, King can be more active  
        //}

        

        if (NumberOfAllPieces == 32) maxTimeSeconds = 5;

        else if (NumberOfAllPieces <= 30 && NumberOfAllPieces >16)
        {
            maxTimeSeconds = defaultTime;
        }

        // Beside using the game phase for time management, We can use available pieces to determinate end-game types, king movements etc..
        Console.WriteLine();
        Console.WriteLine($"Maximum calculation time: {maxTimeSeconds} Seconds");
        Console.WriteLine();
        return maxTimeSeconds;
    }

    private static void FlagCheckmate(MoveObjects moveList)
    {
        if (moveList.counter == 0)
        {
            if (Boards.Side == 0)
            {
                Boards.whiteCheckmate = true;
            }
            else
            {
                Boards.blackCheckmate = true;
            }
        }
    }

    private static int Negamax(int alpha, int beta, int depth)
    {
        if (TranspositionSwitch)
        {
            // here we should check if there is a hit in Transpositiontable 
            var transpositionKey = new Transposition
            {
                position = positionHashKey,
                depth = depth,
                score = 0
            };
            // When I change it to >= for some reason stops the game after finding the checkmate pattern! :|
            if (transpositionTable.TryGetValue(positionHashKey, out Transposition entry) && entry.depth > depth)
            {
                if(entry.depth >= 5)
                {
                    Console.WriteLine($"Hit! Key:{positionHashKey} - depth: {entry.depth} - score: {entry.score}");
                }
                
                //return entry.score;
            }
        }
        

        // Keep track of this ply's PV length
        pvLength[ply] = ply;

        // Base condition 
        if (depth == 0) return Quiescence(alpha, beta);

        // we are too deep, there is an owerflow of arrays relying on max ply constant
        // most likely we are not going to reach this point
        // Safety check
        if (ply > maxPly - 1)
        {
            //NumberOfAllPieces = Globals.CountBits();
            return Evaluators.GetByMaterialAndPosition(Boards.Bitboards);
        }

        nodes++;

        // Handle checks
        bool inCheck = false;
        if (Boards.Side == (int)Colors.white)
        {
            int whiteKingSq = Globals.GetLs1bIndex(Boards.Bitboards[(int)Pieces.K]);
            if (Attacks.IsSquareAttacked(whiteKingSq, Colors.black) > 0)
            {
                inCheck = true;
            }
        }
        else
        {
            int blackKingSq = Globals.GetLs1bIndex(Boards.Bitboards[(int)Pieces.k]);
            if (Attacks.IsSquareAttacked(blackKingSq, Colors.white) > 0)
            {
                inCheck = true;
            }
        }

        // If in check, extend depth by 1, to see if leads to checkmate
        if (inCheck)
        {
            depth++;
        }

        // Generate moves
        MoveObjects moveList = new MoveObjects();
        MoveGenerator.GenerateMoves(moveList);
        
        if(moveList.counter == 0) FlagCheckmate(moveList);  

        // Sort moves by MVV-LVA, killer, history, etc.
        SortMoves(moveList);

        int legalMoves = 0;
        int oldAlpha = alpha;
        int bestMove = 0;


        // LMR tracking variable. 
        int moveSearched = 0;
        // Loop through moves
        int i = 0;
        while (i < moveList.counter)
        {
            int move = moveList.moves[i];

            // Save board state
            MoveGenerator.CopyGameState(
                out ulong[] bitboardsCopy,
                out ulong[] occCopy,
                out Colors sideCopy,
                out int castleCopy,
                out int enpassCopy
            );

            // Skip illegal
            bool legal = MoveGenerator.IsLegal(move, false);
            if (!legal)
            {
                MoveGenerator.RestoreGameState(bitboardsCopy, occCopy, sideCopy, castleCopy, enpassCopy);
                i++;  // Not sure about this!
                continue;
            }


            // we have to update it here too
            //GeneratepositionHashKey();

            ulong oldHash = positionHashKey;
            
            if(TranspositionSwitch)  positionHashKey = GeneratepositionHashKey();

            legalMoves++;
            // Track how many moves we have searched    
            moveSearched++;
            ply++;

            // =============================
            // LMR logic starts here
            // =============================
            // We only reduce if:
            // 1) we’ve already searched a few moves
            // 2) there's enough depth to reduce
            // 3) we’re not in check
            // 4) it's not a capture
            // 5) there's no promotion

            bool isCapture = MoveGenerator.GetMoveCapture(move);
            int promoted = MoveGenerator.GetMovePromoted(move); // 0 if no promotion move


            bool canReduce = moveSearched > FullDepthMoves && depth > ReductionLimit && !inCheck && !isCapture && promoted == 0;
            int newDepth = depth - 1; // Reduced depth

            int score = 0;
            if (canReduce)
            {
                // First reduced search
                score = -Negamax(-beta, -alpha, depth - 1);
                if (score > alpha)
                    score = -Negamax(-beta, -alpha, newDepth);
            }
            else
            {
                score = -Negamax(-beta, -alpha, newDepth);
            }
            // =============================
            // LMR logic ends here
            // =============================

            ply--;

            //score = -Negamax(-beta, -alpha, depth - 1);

            //ply--;

            MoveGenerator.RestoreGameState(bitboardsCopy, occCopy, sideCopy, castleCopy, enpassCopy);

            positionHashKey = oldHash;  
            // Alpha-beta pruning
            if (score >= beta)
            {
                bool capture = MoveGenerator.GetMoveCapture(move);
                // If quiet move => update killer
                if (!capture)
                {
                    killerMoves[1, ply] = killerMoves[0, ply];
                    killerMoves[0, ply] = move;
                }
                if (TranspositionSwitch)
                {
                    // Before returning we store the cutoff 
                    Transposition betaEntry = new();

                    betaEntry.score = beta;
                    betaEntry.depth = depth;
                    transpositionTable[positionHashKey] = betaEntry;
                }
                
                return beta;
            }
            if (score > alpha)
            {
                alpha = score;
                bestMove = move;

                bool capture2 = MoveGenerator.GetMoveCapture(move);
                // If quiet => update history  // without: 22402927 nodes in depth 8 
                //                             // with:    22167494
                if (!capture2)
                {
                    int piece = MoveGenerator.GetMovePiece(move);
                    int targetSq = MoveGenerator.GetMoveTarget(move);
                    historyMoves[piece, targetSq] += depth;
                }

                // Update PV
                pvTable[ply, ply] = move;
                int nextPly = ply + 1;
                while (nextPly < pvLength[ply + 1])
                {
                    pvTable[ply, nextPly] = pvTable[ply + 1, nextPly];
                    nextPly++;
                }
                pvLength[ply] = pvLength[ply + 1];
            }

            i++;
        }

        // No legal moves => checkmate / stalemate
        if (legalMoves == 0)
        {
            if (inCheck)
            {
                // checkmate
                return -49000 + ply;
            }
            else
            {
                // stalemate
                return 0;
            }
        }

        if (TranspositionSwitch)
        {
            Transposition alphaEntry = new();

            alphaEntry.score = alpha;
            alphaEntry.depth = depth;
            transpositionTable[positionHashKey] = alphaEntry;
        }
        
        return alpha;
    }
    // Sort moves by MVV-LVA, killer, history (bubble sort)
    //public static void SortMoves(MoveObjects moveList)
    //{
    //    int count = moveList.counter;

    //    // Simple array for scores
    //    int[] scores = new int[count];
    //    int index = 0;
    //    while (index < count)
    //    {
    //        int move = moveList.moves[index];
    //        scores[index] = ScoreMove(move);
    //        index++;
    //    }

    //    // Bubble-sort by descending
    //    int current = 0;
    //    while (current < count)
    //    {
    //        int next = current + 1;
    //        while (next < count)
    //        {
    //            if (scores[current] < scores[next])
    //            {
    //                // Swap scores
    //                int tempScore = scores[current];
    //                scores[current] = scores[next];
    //                scores[next] = tempScore;

    //                // Swap moves
    //                int tempMove = moveList.moves[current];
    //                moveList.moves[current] = moveList.moves[next];
    //                moveList.moves[next] = tempMove;
    //            }
    //            next++;
    //        }
    //        current++;
    //    }
    //}

    // Using Insertion sort.  will see if it works faster , but for sure will use less memory
    public static void SortMoves(MoveObjects movelIst)
    {
        int count = movelIst.counter;
        int[] scores = new int[count];


        for (int i = 0; i < count; i++)
        {
            scores[i] = ScoreMove(movelIst.moves[i]);

        }

        // Insertion sort using descending order 
        for (int i = 1; i < count; i++)
        {
            int keyMove = movelIst.moves[i];
            int keyScore = scores[i];
            int j = i - 1;

            while (j >= 0 && scores[j] < keyScore)
            {
                movelIst.moves[j + 1] = movelIst.moves[j];
                scores[j + 1] = scores[j];
                j--;
            }
            movelIst.moves[j + 1] = keyMove;
            scores[j + 1] = keyScore;

        }
    }


    // Score move – MVV-LVA plus killers/history
    public static int ScoreMove(int move)
    {
        bool capture = MoveGenerator.GetMoveCapture(move);
        if (capture)
        {
            // mvv-lva
            int attacker = MoveGenerator.GetMovePiece(move);
            int victim = FindVictimPiece(move);
            // + 10000 so captures outrank any quiet move
            return MvvLLvaTable[attacker, victim] + 10000;
        }
        else
        {
            // killer?
            if (killerMoves[0, ply] == move)
            {
                return 9000;
            }
            else
            {
                if (killerMoves[1, ply] == move)
                {
                    return 8000;
                }
                else
                {
                    // history
                    int piece = MoveGenerator.GetMovePiece(move);
                    int target = MoveGenerator.GetMoveTarget(move);
                    return historyMoves[piece, target];
                }
            }
        }
        return 0;
    }

    // Find the victim piece on the target square
    private static int FindVictimPiece(int move)
    {
        int targetSquare = MoveGenerator.GetMoveTarget(move);

        // For white side -> search black piece range, etc.
        // Mirrors the C code’s logic
        int startPiece;
        int endPiece;

        if (Boards.Side == (int)Colors.white)
        {
            startPiece = (int)Pieces.p;
            endPiece = (int)Pieces.k;
        }
        else
        {
            startPiece = (int)Pieces.P;
            endPiece = (int)Pieces.K;
        }

        int i = startPiece;
        while (i <= endPiece)
        {
            bool hasPiece = Globals.GetBit(Boards.Bitboards[i], targetSquare);
            if (hasPiece)
            {
                return i;
            }
            i++;
        }

        // fallback
        return (int)Pieces.P;
    }

    /////////////////////////////////////////////////// PRIVATE METHODS ////////////////////////////////////////////

    // Not sure if I implement it correctly 
    private static int Quiescence(int alpha, int beta)
    {
        nodes++;

        int eval = Evaluators.GetByMaterialAndPosition(Boards.Bitboards);

        if (eval >= beta)
        {
            return beta;
        }
        if (eval > alpha)
        {
            alpha = eval;
        }

        // Generate only captures
        MoveObjects moveList = new MoveObjects();
        MoveGenerator.GenerateMoves(moveList);
        SortMoves(moveList);

        int i = 0;
        while (i < moveList.counter)
        {
            int move = moveList.moves[i];

            // Only consider captures
            bool capture = MoveGenerator.GetMoveCapture(move);
            if (!capture)
            {
                i++;
                continue;
            }

            // Save
            MoveGenerator.CopyGameState(
                out ulong[] bbCopy,
                out ulong[] occCopy,
                out Colors sideCopy,
                out int castleCopy,
                out int enpassCopy
            );

            // Must be legal capture
            bool legal = MoveGenerator.IsLegal(move, true);
            if (!legal)
            {
                MoveGenerator.RestoreGameState(bbCopy, occCopy, sideCopy, castleCopy, enpassCopy);
                i++;
                continue;
            }

            ply++;
            int score = -Quiescence(-beta, -alpha);
            ply--;

            MoveGenerator.RestoreGameState(bbCopy, occCopy, sideCopy, castleCopy, enpassCopy);

            if (score >= beta)
            {
                return beta;
            }
            if (score > alpha)
            {
                alpha = score;
            }

            i++;
        }

        return alpha;
    }
    private static void ClearKillerAndHistoryMoves()
    {
        int i = 0;
        while (i < 2)
        {
            int j = 0;
            while (j < 64)
            {
                killerMoves[i, j] = 0;
                j++;
            }
            i++;
        }

        int p = 0;
        while (p < 12)
        {
            int s = 0;
            while (s < 64)
            {
                historyMoves[p, s] = 0;
                s++;
            }
            p++;
        }
    }


    private static void ClearPV()
    {
        int i = 0;
        while (i < 64)
        {
            pvLength[i] = 0;
            int j = 0;
            while (j < 64)
            {
                pvTable[i, j] = 0;
                j++;
            }
            i++;
        }
    }


    private static string PrintPVLine()
    {
        // Build a string of moves from pvTable[0]
        int length = pvLength[0];
        string line = "";
        int i = 0;
        while (i < length)
        {
            int move = pvTable[0, i];
            line += Globals.MoveToString(move);
            line += " ";
            i++;
        }
        return line;
    }


    public static readonly int[,] MvvLLvaTable = new int[12, 12]
    {
        { 105, 205, 305, 405, 505, 605, 105, 205, 305, 405, 505, 605 },
        { 104, 204, 304, 404, 504, 604, 104, 204, 304, 404, 504, 604 },
        { 103, 203, 303, 403, 503, 603, 103, 203, 303, 403, 503, 603 },
        { 102, 202, 302, 402, 502, 602, 102, 202, 302, 402, 502, 602 },
        { 101, 201, 301, 401, 501, 601, 101, 201, 301, 401, 501, 601 },
        { 100, 200, 300, 400, 500, 600, 100, 200, 300, 400, 500, 600 },
        { 105, 205, 305, 405, 505, 605, 105, 205, 305, 405, 505, 605 },
        { 104, 204, 304, 404, 504, 604, 104, 204, 304, 404, 504, 604 },
        { 103, 203, 303, 403, 503, 603, 103, 203, 303, 403, 503, 603 },
        { 102, 202, 302, 402, 502, 602, 102, 202, 302, 402, 502, 602 },
        { 101, 201, 301, 401, 501, 601, 101, 201, 301, 401, 501, 601 },
        { 100, 200, 300, 400, 500, 600, 100, 200, 300, 400, 500, 600 }
    };
}




/*
*      Inspired by Code monkey King channel
* 
*            MOVE ORDERING MAP   
*            1. PV Move
*            2. Captures in MVV-LVA order  
*            3. 1st Killer moves
*            4. 2nd Killer moves
*            5. History moves
*            6. Unsorted moves
*/

/*
    In order to implement threefold repetition we need to have unique position identifier. 
    And using the key "Hash key" to identify position we can additionally implement transposition table 
    Which will improve acurace and speed of search and overal the engine. 
 */