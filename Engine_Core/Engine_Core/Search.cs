using Engine;
using System.Numerics;
using static Engine_Core.Enumes;

namespace Engine_Core;

public struct Transposition
{
    public ulong position;
    public int depth;
    public int score;
    public NodeType flag;
}

public static class Search
{
    // Test 
    private static readonly MoveObjects[] moveLists = Enumerable.Range(0, 128).Select(_ => new MoveObjects()).ToArray();
    public static DateTime SearchStartTime { get; set; }
    public static int MaxSearchTimeSeconds { get; set; }

    public static GamePhase CurrentGamePhase { get; set; }
    //--- Search configuration switches ---
    public static bool TranspositionSwitch { get; set; }
    public static bool TimeLimitDeepeningSwitch { get; set; }

    // --- Variables to determine game phase ---
    public static int NumberOfAllPieces { get; set; }


    public static Dictionary<ulong, Transposition> transpositionTable = new Dictionary<ulong, Transposition>();

    // --- Variables needed for late move reduction and PV ---
    private static int FullDepthMoves = 2;
    private static int ReductionLimit = 1;


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

    // **********************************************   ZOBRIST  HASHING  

    // Random piece keys [piece, squar]  give a random unique number to piece on given square
    public static ulong[,] pieceKeysOnSquare = new ulong[12, 64];

    // Random En-passant key and square 
    public static ulong[] enPassantKey = new ulong[64];

    // Random Side to play key 

    public static ulong sideKey;

    // Random castling keys 
    public static ulong[] castlingKeys = new ulong[16];

    // Almost unique position identifier hash key  / position key 
    public static ulong positionHashKey;


    public static void InitializeRandomKeys()
    {
        int index = 0;

        for (Pieces piece = (int)Pieces.P; (int)piece <= (int)Pieces.k; piece++)
        {
            for (int square = 0; square < 64; square++)
            {
                pieceKeysOnSquare[(int)piece, square] = Globals.GetPolyglotKey(index++);
            }
        }

        for (int square = 0; square < 8; square++)
        {
            enPassantKey[square] = Globals.GetPolyglotKey(index++);
        }

        sideKey = Globals.GetPolyglotKey(index++);

        for (int i = 0; i < 16; i++)
        {
            castlingKeys[i] = Globals.GetPolyglotKey(index++);
        }
    }

    public static ulong GeneratePositionHashKey()
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
        if (Boards.EnpassantSquare != (int)Squares.NoSquare)
        {
            int epFile = Boards.EnpassantSquare % 8;
            int epRank = Boards.EnpassantSquare / 8;

            if (Boards.Side == (int)Colors.white && epRank == 5)
            {
                ulong pawns = Boards.Bitboards[(int)Pieces.P];
                bool leftCapture = ((pawns >> 1) & ~0x0101010101010101UL & (1UL << (Boards.EnpassantSquare - 8))) != 0;
                bool rightCapture = ((pawns << 1) & ~0x8080808080808080UL & (1UL << (Boards.EnpassantSquare - 8))) != 0;

                if (leftCapture || rightCapture)
                {
                    positionHashKey ^= enPassantKey[epFile];
                }
            }
            else if (Boards.Side == (int)Colors.black && epRank == 2)
            {
                ulong pawns = Boards.Bitboards[(int)Pieces.p];
                bool leftCapture = ((pawns >> 1) & ~0x0101010101010101UL & (1UL << (Boards.EnpassantSquare + 8))) != 0;
                bool rightCapture = ((pawns << 1) & ~0x8080808080808080UL & (1UL << (Boards.EnpassantSquare + 8))) != 0;

                if (leftCapture || rightCapture)
                {
                    positionHashKey ^= enPassantKey[epFile];
                }
            }
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


    // *****************************************    Iterative Deepening Search Negamax entry ***************************************************** //
    public static int GetBestMoveWithIterativeDeepening(int maxTimeSeconds, int maxDepth)
    {
        int score = 0;
        MoveObjects moveList = new MoveObjects();
        MoveGenerator.GenerateMoves(moveList);

        CurrentGamePhase = GetGamePhase();

        if (CurrentGamePhase is GamePhase.KQK || CurrentGamePhase is GamePhase.KRK)
        {
            maxTimeSeconds = 50; maxDepth = 12;
        }

        SortMoves(moveList);

        int bestMove = 0;
        ply = 0;
        var startTime = DateTime.UtcNow;

        ClearKillerAndHistoryMoves();
        ClearPV();

        //--- Turn on or off from program.cs ---    
        if (TranspositionSwitch) GeneratePositionHashKey();

        for (int currentDepth = 1; currentDepth <= maxDepth; currentDepth++)
        {

            var depthStartTime = DateTime.UtcNow;
            nodes = 0;

            score = Negamax(-50000, 50000, currentDepth);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"Depth:{currentDepth} Nodes:{nodes} Score:{score} Time:{(DateTime.UtcNow - depthStartTime).TotalSeconds}Sec Pv:{PrintPVLine()}");
            Console.ResetColor();
            ExecutablePv.Clear();
            for (int i = 0; i < pvLength[0]; i++)
                ExecutablePv.Add(pvTable[0, i]);

            bestMove = pvTable[0, 0];

            if ((DateTime.UtcNow - startTime).TotalSeconds > maxTimeSeconds)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Max time reached ({maxTimeSeconds * maxDepth}s). Stopping search.");
                Console.WriteLine($"bestmove {Globals.MoveToString(bestMove)}");
                Console.ResetColor();
                bestMove = pvTable[0, 0];
                Thread.Sleep(500);
                return bestMove;

            }

        }

        return bestMove;
    }
    // **********************************************************************************************  Negamax
    private static int Negamax(int alpha, int beta, int depth)
    {
        var negamaxMaxTimeStart = DateTime.UtcNow;

        if (TranspositionSwitch)
        {   //--- I dont know why when entry.depth >= depth, engine will stop after finding the right move!
            if (transpositionTable.TryGetValue(positionHashKey, out var entry) && entry.depth == depth)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                if (depth >= 9) Console.WriteLine($"Hit! Key:{positionHashKey} - depth: {entry.depth} - score: {entry.score} Time: {negamaxMaxTimeStart.Millisecond}");
                Console.ResetColor();
                if (entry.flag == NodeType.Exact) return entry.score;

                else if (entry.flag == NodeType.Alpha && entry.score <= alpha) return alpha;

                else if (entry.flag == NodeType.Beta && entry.score >= beta) return beta;

            }
        }
        pvLength[ply] = ply;

        if (depth == 0) return Quiescence(alpha, beta);

        nodes++;

        //MoveObjects moveList = new MoveObjects();
        //MoveGenerator.GenerateMoves(moveList);

        MoveObjects moveList = moveLists[ply];
        moveList.counter = 0;
        MoveGenerator.GenerateMoves(moveList);

        bool inCheck = false;
        inCheck = IsCheck(inCheck);

        if (inCheck)
        {
            depth++;
        }

        SortMoves(moveList);

        int legalMoves = 0;
        int oldAlpha = alpha;
        int bestMove = 0;
        int moveSearched = 0;

        int i = 0;
        while (i < moveList.counter)
        {
            int move = moveList.moves[i];
            try
            {
                MoveGenerator.CopyGameState(
                out ulong[] bitboardsCopy,
                out ulong[] occCopy,
                out Colors sideCopy,
                out int castleCopy,
                out int enpassCopy
            );

                if (!MoveGenerator.IsLegal(move, false))
                {
                    MoveGenerator.RestoreGameState(bitboardsCopy, occCopy, sideCopy, castleCopy, enpassCopy);
                    i++;
                    continue;
                }

                ulong oldHash = positionHashKey;

                if (TranspositionSwitch)
                {
                    positionHashKey = GeneratePositionHashKey();
                }

                legalMoves++;
                moveSearched++;
                ply++;

                bool isCapture = MoveGenerator.GetMoveCapture(move);
                int promoted = MoveGenerator.GetMovePromoted(move);

                bool canReduce = false;
                if (moveSearched > FullDepthMoves &&
                    depth > ReductionLimit &&
                    !inCheck &&
                    !isCapture &&
                    promoted == 0)
                {
                    canReduce = true;
                }

                int newDepth;
                if (canReduce)
                {
                    newDepth = depth - 2;
                }
                else
                {
                    newDepth = depth - 1;
                }

                int score = -Negamax(-beta, -alpha, newDepth);

                ply--;

                MoveGenerator.RestoreGameState(bitboardsCopy, occCopy, sideCopy, castleCopy, enpassCopy);

                positionHashKey = oldHash;

                if (score >= beta)
                {
                    if (!isCapture)
                    {
                        killerMoves[1, ply] = killerMoves[0, ply];
                        killerMoves[0, ply] = move;
                    }

                    if (TranspositionSwitch)
                    {
                        if (!transpositionTable.ContainsKey(positionHashKey) || transpositionTable[positionHashKey].depth < depth)
                        {
                            transpositionTable[positionHashKey] = new Transposition
                            {
                                position = positionHashKey,
                                score = beta,
                                depth = depth,
                            };
                        }
                    }

                    return beta;
                }

                if (score > alpha)
                {
                    alpha = score;
                    bestMove = move;

                    if (!isCapture)
                    {
                        int piece = MoveGenerator.GetMovePiece(move);
                        int targetSq = MoveGenerator.GetMoveTarget(move);
                        historyMoves[piece, targetSq] += depth;
                    }

                    pvTable[ply, ply] = move;
                    for (int next = ply + 1; next < pvLength[ply + 1]; next++)
                    {
                        pvTable[ply, next] = pvTable[ply + 1, next];
                    }

                    pvLength[ply] = pvLength[ply + 1];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Crash on move: {Globals.MoveToString(move)} (raw: {move})");
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }


            i++; // make sure we continue looping
        }

        if (legalMoves == 0)
        {
            if (inCheck)
                return -49000 + ply;
            else
                return 0;
        }

        if (TranspositionSwitch)
        {
            NodeType flag;

            if (alpha <= oldAlpha)
                flag = NodeType.Alpha;
            else if (alpha >= beta)
                flag = NodeType.Beta;
            else
                flag = NodeType.Exact;

            if (!transpositionTable.ContainsKey(positionHashKey) || transpositionTable[positionHashKey].depth < depth)
            {
                transpositionTable[positionHashKey] = new Transposition
                {
                    position = positionHashKey,
                    score = alpha,  // ok, alpha holds best found score now
                    depth = depth,
                    flag = flag
                };
            }
        }

        return alpha;
    }

    private static bool IsCheck(bool inCheck)
    {
        if (Boards.Side == (int)Colors.white)
        {
            int whiteKingSq = Globals.GetLs1bIndex(Boards.Bitboards[(int)Pieces.K]);
            if (whiteKingSq < 0 || whiteKingSq >= 64)
                Console.WriteLine($"Warning: Invalid white king square index: {whiteKingSq}");
            if (Attacks.IsSquareAttacked(whiteKingSq, Colors.black) > 0)
                inCheck = true;
        }
        else
        {
            int blackKingSq = Globals.GetLs1bIndex(Boards.Bitboards[(int)Pieces.k]);
            if (blackKingSq < 0 || blackKingSq >= 64)
                Console.WriteLine($"Warning: Invalid black king square index: {blackKingSq}");
            if (Attacks.IsSquareAttacked(blackKingSq, Colors.white) > 0)
                inCheck = true;
        }

        return inCheck;
    }

    // TODO: Find a way to return game phase first , time and other parameters should be adjusted based on game phase.


    // Not sure if I implement it correctly 
    public static int Quiescence(int alpha, int beta)
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
            if (!MoveGenerator.IsLegal(move, true))
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

    private static readonly int[,] MvvLLvaTable = new int[12, 12]
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


    // Detecting game phase // 
    public static int CountPieces()
    {
        int total = 0;
        for (int piece = 0; piece < Boards.Bitboards.Length; piece++)
        {
            total += BitOperations.PopCount(Boards.Bitboards[piece]);
        }
        return total;
    }


    public static GamePhase GetGamePhase()
    {
        // Count pieces by bitboards directly
        int whiteRooks = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.R]);
        int whiteBishops = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.B]);
        int whiteKnights = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.N]);
        int whiteQueens = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.Q]);
        int whitePawns = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.P]);

        int blackRooks = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.r]);
        int blackBishops = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.b]);
        int blackKnights = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.n]);
        int blackQueens = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.q]);
        int blackPawns = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.p]);

        int totalPieces = whiteRooks + whiteBishops + whiteKnights + whiteQueens + whitePawns +
                          blackRooks + blackBishops + blackKnights + blackQueens + blackPawns + 2; // +2 kings

        // Opening
        if (totalPieces == 32)
        {
            Console.WriteLine($"Game phase: {CurrentGamePhase}");
            return GamePhase.Opening;
        }

        // King vs rook endgame
        else if (totalPieces == 3 && (whiteRooks == 1 || blackRooks == 1))
        {
            Console.WriteLine($"Game phase: {CurrentGamePhase}");
            return GamePhase.KRK;
        }

        else if (totalPieces == 3 && (whiteQueens == 1 || blackQueens == 1))
        {
            Console.WriteLine($"Game phase: {CurrentGamePhase}");
            return GamePhase.KQK;
        }

        // Middle game: queens still on board, but some trades
        if (totalPieces < 32 && totalPieces > 24 && whiteQueens >= 1 && blackQueens >= 1)
        {
            return GamePhase.MiddleGame;
        }


        return GamePhase.None;
    }

}
///*
//*      Inspired by Code monkey King channel
//* 
//*            MOVE ORDERING MAP   
//*            1. PV Move
//*            2. Captures in MVV-LVA order  
//*            3. 1st Killer moves
//*            4. 2nd Killer moves
//*            5. History moves
//*            6. Unsorted moves
//*/

///*
//    To implement threefold repetition, we need a unique position identifier. 
//    By using a hash key to identify each position, we can also implement a transposition table, 
//    which will improve the engine’s accuracy and overall search speed.

// */







//using Engine;
//using System.Numerics;
//using static Engine_Core.Enumes;

//namespace Engine_Core;

//public struct Transposition
//{
//    public ulong position;
//    public int depth;
//    public int score;
//    public NodeType flag;
//    public int bestMove; // ADDED: Field to store the move that caused the TT entry
//}

//public static class Search
//{
//    // Test 
//    private static readonly MoveObjects[] moveLists = Enumerable.Range(0, 128).Select(_ => new MoveObjects()).ToArray();
//    public static DateTime SearchStartTime { get; set; }
//    public static int MaxSearchTimeSeconds { get; set; }

//    public static GamePhase CurrentGamePhase { get; set; }
//    //--- Search configuration switches ---
//    public static bool TranspositionSwitch { get; set; }
//    public static bool TimeLimitDeepeningSwitch { get; set; }

//    // --- Variables to determine game phase ---
//    public static int NumberOfAllPieces { get; set; }


//    public static Dictionary<ulong, Transposition> transpositionTable = new Dictionary<ulong, Transposition>();

//    // --- Variables needed for late move reduction and PV ---\
//    private static int FullDepthMoves = 2;
//    private static int ReductionLimit = 1;


//    public static List<int> ExecutablePv = new List<int>();
//    public static long nodes;

//    public static int maxPly = 64;

//    public static int[,] killerMoves = new int[2, maxPly];
//    public static int[,] historyMoves = new int[12, 64];

//    // Triangular principal variation table
//    public static int[] pvLength = new int[maxPly];
//    public static int[,] pvTable = new int[maxPly, maxPly];

//    // Half-move (ply) counter
//    public static int ply;

//    // ********************************************** ZOBRIST  HASHING  

//    // Random piece keys [piece, squar]  give a random unique number to piece on given square
//    public static ulong[,] pieceKeysOnSquare = new ulong[12, 64];

//    // Random En-passant key and square 
//    public static ulong[] enPassantKey = new ulong[64];

//    // Random Side to play key 

//    public static ulong sideKey;

//    // Random castling keys 
//    public static ulong[] castlingKeys = new ulong[16];

//    // Almost unique position identifier hash key  / position key 
//    public static ulong positionHashKey;


//    public static void InitializeRandomKeys()
//    {
//        int index = 0;

//        for (Pieces piece = (int)Pieces.P; (int)piece <= (int)Pieces.k; piece++)
//        {
//            for (int square = 0; square < 64; square++)
//            {
//                pieceKeysOnSquare[(int)piece, square] = Globals.GetPolyglotKey(index++);
//            }
//        }

//        for (int square = 0; square < 8; square++)
//        {
//            enPassantKey[square] = Globals.GetPolyglotKey(index++);
//        }

//        sideKey = Globals.GetPolyglotKey(index++);

//        for (int i = 0; i < 16; i++)
//        {
//            castlingKeys[i] = Globals.GetPolyglotKey(index++);
//        }
//    }

//    public static ulong GeneratePositionHashKey()
//    {
//        positionHashKey = 0;
//        // Temp board 
//        ulong pieceBitboard;
//        int square = 0;
//        // loop over pieces 
//        for (int piece = (int)Pieces.P; (int)piece <= (int)Pieces.k; piece++)
//        {
//            pieceBitboard = Boards.Bitboards[piece];

//            while (pieceBitboard != 0)
//            {
//                // init square occupied by piece 
//                square = Globals.GetLs1bIndex(pieceBitboard);
//                Globals.PopBit(ref pieceBitboard, square);

//                // Testing piece positions
//                //Console.WriteLine($"Piece: {Globals.SquareToCoordinates[square]}");

//                // adding piece hash to position hash!
//                positionHashKey ^= pieceKeysOnSquare[piece, square];
//            }
//        }

//        // En-passant 
//        if (Boards.EnpassantSquare != (int)Squares.NoSquare)
//        {
//            int epFile = Boards.EnpassantSquare % 8;

//            // OPTIMIZATION: Removed redundant EP attack validation checks.
//            positionHashKey ^= enPassantKey[epFile];
//        }

//        // Castling
//        positionHashKey ^= castlingKeys[Boards.CastlePerm];

//        // Hashing the side only if black is to move
//        if (Boards.Side == (int)Colors.black)
//        {

//            positionHashKey ^= sideKey;
//        }

//        return positionHashKey;
//    }


//    // ***************************************** Iterative Deepening Search Negamax entry ***************************************************** //
//    public static int GetBestMoveWithIterativeDeepening(int maxTimeSeconds, int maxDepth)
//    {
//        int score = 0;
//        MoveObjects moveList = new MoveObjects();
//        MoveGenerator.GenerateMoves(moveList);

//        CurrentGamePhase = GetGamePhase();

//        if (CurrentGamePhase is GamePhase.KQK || CurrentGamePhase is GamePhase.KRK)
//        {
//            maxTimeSeconds = 50; maxDepth = 12;
//        }


//        if (moveList.counter == 1) return moveList.moves[0];

//        SortMoves(moveList);

//        int bestMove = 0;
//        ply = 0;
//        var startTime = DateTime.UtcNow;

//        ClearKillerAndHistoryMoves();
//        ClearPV();

//        //--- Turn on or off from program.cs ---   
//        if (TranspositionSwitch) GeneratePositionHashKey();

//        for (int currentDepth = 1; currentDepth <= maxDepth; currentDepth++)
//        {

//            var depthStartTime = DateTime.UtcNow;
//            nodes = 0;

//            score = Negamax(-50000, 50000, currentDepth);

//            Console.ForegroundColor = ConsoleColor.DarkCyan;
//            Console.WriteLine($"Depth:{currentDepth} Nodes:{nodes} Score:{score} Time:{(DateTime.UtcNow - depthStartTime).TotalSeconds}Sec Pv:{PrintPVLine()}");
//            Console.ResetColor();
//            ExecutablePv.Clear();
//            for (int i = 0; i < pvLength[0]; i++)
//                ExecutablePv.Add(pvTable[0, i]);

//            bestMove = pvTable[0, 0];

//            if ((DateTime.UtcNow - startTime).TotalSeconds > maxTimeSeconds)
//            {
//                Console.ForegroundColor = ConsoleColor.Red;
//                Console.WriteLine($"Max time reached ({maxTimeSeconds * maxDepth}s). Stopping search.");
//                Console.WriteLine($"bestmove {Globals.MoveToString(bestMove)}");
//                Console.ResetColor();
//                bestMove = pvTable[0, 0];
//                Thread.Sleep(500);
//                return bestMove;

//            }

//        }

//        return bestMove;
//    }
//    // ********************************************************************************************** Negamax
//    private static int Negamax(int alpha, int beta, int depth)
//    {
//        var negamaxMaxTimeStart = DateTime.UtcNow;

//        if (TranspositionSwitch)
//        {    //--- I dont know why when entry.depth >= depth, engine will stop after finding the right move!
//            if (transpositionTable.TryGetValue(positionHashKey, out var entry) && entry.depth == depth)
//            {
//                Console.ForegroundColor = ConsoleColor.DarkRed;
//                if (depth >= 9) Console.WriteLine($"Hit! Key:{positionHashKey} - depth: {entry.depth} - score: {entry.score} Time: {negamaxMaxTimeStart.Millisecond}");
//                Console.ResetColor();
//                if (entry.flag == NodeType.Exact) return entry.score;

//                else if (entry.flag == NodeType.Alpha && entry.score <= alpha) return alpha;

//                else if (entry.flag == NodeType.Beta && entry.score >= beta) return beta;

//            }
//        }
//        pvLength[ply] = ply;

//        if (depth == 0) return Quiescence(alpha, beta);

//        nodes++;

//        //MoveObjects moveList = new MoveObjects();
//        //MoveGenerator.GenerateMoves(moveList);

//        MoveObjects moveList = moveLists[ply];
//        moveList.counter = 0;
//        MoveGenerator.GenerateMoves(moveList);

//        bool inCheck = false;
//        inCheck = IsCheck(inCheck);

//        if (inCheck)
//        {
//            depth++;
//        }

//        SortMoves(moveList);

//        int legalMoves = 0;
//        int oldAlpha = alpha;
//        int bestMove = 0; // Move that produces the best score
//        int moveSearched = 0;

//        int i = 0;
//        while (i < moveList.counter)
//        {
//            int move = moveList.moves[i];
//            try
//            {
//                MoveGenerator.CopyGameState(
//                out ulong[] bitboardsCopy,
//                out ulong[] occCopy,
//                out Colors sideCopy,
//                out int castleCopy,
//                out int enpassCopy
//                );

//                if (!MoveGenerator.IsLegal(move, false))
//                {
//                    MoveGenerator.RestoreGameState(bitboardsCopy, occCopy, sideCopy, castleCopy, enpassCopy);
//                    i++;
//                    continue;
//                }

//                ulong oldHash = positionHashKey;

//                if (TranspositionSwitch)
//                {
//                    positionHashKey = GeneratePositionHashKey();
//                }

//                legalMoves++;
//                moveSearched++;
//                ply++;

//                bool isCapture = MoveGenerator.GetMoveCapture(move);
//                int promoted = MoveGenerator.GetMovePromoted(move);

//                bool canReduce = false;
//                if (moveSearched > FullDepthMoves &&
//                    depth > ReductionLimit &&
//                    !inCheck &&
//                    !isCapture &&
//                    promoted == 0)
//                {
//                    canReduce = true;
//                }

//                int newDepth;
//                if (canReduce)
//                {
//                    newDepth = depth - 2;
//                }
//                else
//                {
//                    newDepth = depth - 1;
//                }

//                int score = -Negamax(-beta, -alpha, newDepth);

//                ply--;

//                MoveGenerator.RestoreGameState(bitboardsCopy, occCopy, sideCopy, castleCopy, enpassCopy);

//                positionHashKey = oldHash;

//                if (score >= beta)
//                {
//                    if (!isCapture)
//                    {
//                        killerMoves[1, ply] = killerMoves[0, ply];
//                        killerMoves[0, ply] = move;
//                    }

//                    if (TranspositionSwitch)
//                    {
//                        if (!transpositionTable.ContainsKey(positionHashKey) || transpositionTable[positionHashKey].depth < depth)
//                        {
//                            transpositionTable[positionHashKey] = new Transposition
//                            {
//                                position = positionHashKey,
//                                score = beta,
//                                depth = depth,
//                                bestMove = move // ADDED: Store the best move
//                            };
//                        }
//                    }

//                    return beta;
//                }

//                if (score > alpha)
//                {
//                    alpha = score;
//                    bestMove = move; // Update bestMove

//                    if (!isCapture)
//                    {
//                        int piece = MoveGenerator.GetMovePiece(move);
//                        int targetSq = MoveGenerator.GetMoveTarget(move);
//                        historyMoves[piece, targetSq] += depth;
//                    }

//                    pvTable[ply, ply] = move;
//                    for (int next = ply + 1; next < pvLength[ply + 1]; next++)
//                    {
//                        pvTable[ply, next] = pvTable[ply + 1, next];
//                    }

//                    pvLength[ply] = pvLength[ply + 1];
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Crash on move: {Globals.MoveToString(move)} (raw: {move})");
//                Console.WriteLine($"Exception: {ex.Message}");
//                throw;
//            }


//            i++; // make sure we continue looping
//        }

//        if (legalMoves == 0)
//        {
//            if (inCheck)
//                return -49000 + ply;
//            else
//                return 0;
//        }

//        if (TranspositionSwitch)
//        {
//            NodeType flag;

//            if (alpha <= oldAlpha)
//                flag = NodeType.Alpha;
//            else if (alpha >= beta)
//                flag = NodeType.Beta;
//            else
//                flag = NodeType.Exact;

//            if (!transpositionTable.ContainsKey(positionHashKey) || transpositionTable[positionHashKey].depth < depth)
//            {
//                transpositionTable[positionHashKey] = new Transposition
//                {
//                    position = positionHashKey,
//                    score = alpha,  // ok, alpha holds best found score now
//                    depth = depth,
//                    flag = flag,
//                    bestMove = bestMove // ADDED: Store the move that produced the final alpha score
//                };
//            }
//        }

//        return alpha;
//    }

//    private static bool IsCheck(bool inCheck)
//    {
//        if (Boards.Side == (int)Colors.white)
//        {
//            int whiteKingSq = Globals.GetLs1bIndex(Boards.Bitboards[(int)Pieces.K]);
//            if (whiteKingSq < 0 || whiteKingSq >= 64)
//                Console.WriteLine($"Warning: Invalid white king square index: {whiteKingSq}");
//            if (Attacks.IsSquareAttacked(whiteKingSq, Colors.black) > 0)
//                inCheck = true;
//        }
//        else
//        {
//            int blackKingSq = Globals.GetLs1bIndex(Boards.Bitboards[(int)Pieces.k]);
//            if (blackKingSq < 0 || blackKingSq >= 64)
//                Console.WriteLine($"Warning: Invalid black king square index: {blackKingSq}");
//            if (Attacks.IsSquareAttacked(blackKingSq, Colors.white) > 0)
//                inCheck = true;
//        }

//        return inCheck;
//    }

//    // TODO: Find a way to return game phase first , time and other parameters should be adjusted based on game phase.


//    // Not sure if I implement it correctly 
//    public static int Quiescence(int alpha, int beta)
//    {
//        nodes++;

//        int eval = Evaluators.GetByMaterialAndPosition(Boards.Bitboards);


//        if (eval >= beta)
//        {
//            return beta;
//        }
//        if (eval > alpha)
//        {
//            alpha = eval;
//        }

//        // Generate only captures
//        MoveObjects moveList = new MoveObjects();
//        MoveGenerator.GenerateMoves(moveList);

//        SortMoves(moveList);

//        int i = 0;
//        while (i < moveList.counter)
//        {
//            int move = moveList.moves[i];
//            // Only consider captures
//            bool capture = MoveGenerator.GetMoveCapture(move);
//            if (!capture)
//            {
//                i++;
//                continue;
//            }

//            // Save
//            MoveGenerator.CopyGameState(
//                out ulong[] bbCopy,
//                out ulong[] occCopy,
//                out Colors sideCopy,
//                out int castleCopy,
//                out int enpassCopy
//            );

//            // Must be legal capture
//            if (!MoveGenerator.IsLegal(move, true))
//            {
//                MoveGenerator.RestoreGameState(bbCopy, occCopy, sideCopy, castleCopy, enpassCopy);
//                i++;
//                continue;
//            }

//            // --- Hashing is currently missing here for TT/Repetition checks in Quiescence ---

//            ply++;
//            int score = -Quiescence(-beta, -alpha);
//            ply--;

//            MoveGenerator.RestoreGameState(bbCopy, occCopy, sideCopy, castleCopy, enpassCopy);

//            if (score >= beta)
//            {
//                return beta;
//            }
//            if (score > alpha)
//            {
//                alpha = score;
//            }

//            i++;
//        }

//        return alpha;
//    }


//    public static void SortMoves(MoveObjects movelIst)
//    {
//        int count = movelIst.counter;
//        int[] scores = new int[count];

//        // --- ADDED: Check TT for stored best move (highest priority) ---
//        int bestMoveFromTT = 0;
//        if (TranspositionSwitch && transpositionTable.TryGetValue(positionHashKey, out var entry))
//        {
//            bestMoveFromTT = entry.bestMove;
//        }
//        // -------------------------------------------------------------


//        for (int i = 0; i < count; i++)
//        {
//            if (movelIst.moves[i] == bestMoveFromTT)
//            {
//                scores[i] = 20000; // Highest score: 20000 (above all MVV-LVA captures)
//            }
//            else
//            {
//                scores[i] = ScoreMove(movelIst.moves[i]);
//            }

//        }

//        // Insertion sort using descending order 
//        for (int i = 1; i < count; i++)
//        {
//            int keyMove = movelIst.moves[i];
//            int keyScore = scores[i];
//            int j = i - 1;

//            while (j >= 0 && scores[j] < keyScore)
//            {
//                movelIst.moves[j + 1] = movelIst.moves[j];
//                scores[j + 1] = scores[j];
//                j--;
//            }
//            movelIst.moves[j + 1] = keyMove;
//            scores[j + 1] = keyScore;

//        }
//    }


//    // Score move – MVV-LVA plus killers/history
//    public static int ScoreMove(int move)
//    {
//        bool capture = MoveGenerator.GetMoveCapture(move);
//        if (capture)
//        {
//            // mvv-lva
//            int attacker = MoveGenerator.GetMovePiece(move);
//            int victim = FindVictimPiece(move);
//            // + 10000 so captures outrank any quiet move
//            return MvvLLvaTable[attacker, victim] + 10000;
//        }
//        else
//        {
//            // killer?
//            if (killerMoves[0, ply] == move)
//            {
//                return 9000;
//            }
//            else
//            {
//                if (killerMoves[1, ply] == move)
//                {
//                    return 8000;
//                }
//                else
//                {
//                    // history
//                    int piece = MoveGenerator.GetMovePiece(move);
//                    int target = MoveGenerator.GetMoveTarget(move);
//                    return historyMoves[piece, target];
//                }
//            }
//        }
//        return 0;
//    }

//    private static readonly int[,] MvvLLvaTable = new int[12, 12]
//    {
//        { 105, 205, 305, 405, 505, 605, 105, 205, 305, 405, 505, 605 },
//        { 104, 204, 304, 404, 504, 604, 104, 204, 304, 404, 504, 604 },
//        { 103, 203, 303, 403, 503, 603, 103, 203, 303, 403, 503, 603 },
//        { 102, 202, 302, 402, 502, 602, 102, 202, 302, 402, 502, 602 },
//        { 101, 201, 301, 401, 501, 601, 101, 201, 301, 401, 501, 601 },
//        { 100, 200, 300, 400, 500, 600, 100, 200, 300, 400, 500, 600 },
//        { 105, 205, 305, 405, 505, 605, 105, 205, 305, 405, 505, 605 },
//        { 104, 204, 304, 404, 504, 604, 104, 204, 304, 404, 504, 604 },
//        { 103, 203, 303, 403, 503, 603, 103, 203, 303, 403, 503, 603 },
//        { 102, 202, 302, 402, 502, 602, 102, 202, 302, 402, 502, 602 },
//        { 101, 201, 301, 401, 501, 601, 101, 201, 301, 401, 501, 601 },
//        { 100, 200, 300, 400, 500, 600, 100, 200, 300, 400, 500, 600 }
//    };

//    private static void ClearKillerAndHistoryMoves()
//    {
//        int i = 0;
//        while (i < 2)
//        {
//            int j = 0;
//            while (j < 64)
//            {
//                killerMoves[i, j] = 0;
//                j++;
//            }
//            i++;
//        }

//        int p = 0;
//        while (p < 12)
//        {
//            int s = 0;
//            while (s < 64)
//            {
//                historyMoves[p, s] = 0;
//                s++;
//            }
//            p++;
//        }
//    }
//    private static void ClearPV()
//    {
//        int i = 0;
//        while (i < 64)
//        {
//            pvLength[i] = 0;
//            int j = 0;
//            while (j < 64)
//            {
//                pvTable[i, j] = 0;
//                j++;
//            }
//            i++;
//        }
//    }

//    private static string PrintPVLine()
//    {
//        // Build a string of moves from pvTable[0]
//        int length = pvLength[0];
//        string line = "";
//        int i = 0;
//        while (i < length)
//        {
//            int move = pvTable[0, i];
//            line += Globals.MoveToString(move);
//            line += " ";
//            i++;
//        }
//        return line;
//    }


//    private static int FindVictimPiece(int move)
//    {
//        int targetSquare = MoveGenerator.GetMoveTarget(move);

//        // For white side -> search black piece range, etc.
//        // Mirrors the C code’s logic
//        int startPiece;
//        int endPiece;

//        if (Boards.Side == (int)Colors.white)
//        {
//            startPiece = (int)Pieces.p;
//            endPiece = (int)Pieces.k;
//        }
//        else
//        {
//            startPiece = (int)Pieces.P;
//            endPiece = (int)Pieces.K;
//        }

//        int i = startPiece;
//        while (i <= endPiece)
//        {
//            bool hasPiece = Globals.GetBit(Boards.Bitboards[i], targetSquare);
//            if (hasPiece)
//            {
//                return i;
//            }
//            i++;
//        }

//        // fallback
//        return (int)Pieces.P;
//    }


//    // Detecting game phase // 
//    public static int CountPieces()
//    {
//        int total = 0;
//        for (int piece = 0; piece < Boards.Bitboards.Length; piece++)
//        {
//            total += BitOperations.PopCount(Boards.Bitboards[piece]);
//        }
//        return total;
//    }


//    public static GamePhase GetGamePhase()
//    {
//        // Count pieces by bitboards directly
//        int whiteRooks = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.R]);
//        int whiteBishops = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.B]);
//        int whiteKnights = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.N]);
//        int whiteQueens = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.Q]);
//        int whitePawns = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.P]);

//        int blackRooks = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.r]);
//        int blackBishops = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.b]);
//        int blackKnights = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.n]);
//        int blackQueens = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.q]);
//        int blackPawns = BitOperations.PopCount(Boards.Bitboards[(int)Pieces.p]);

//        int totalPieces = whiteRooks + whiteBishops + whiteKnights + whiteQueens + whitePawns +
//                          blackRooks + blackBishops + blackKnights + blackQueens + blackPawns + 2; // +2 kings

//        // Opening
//        if (totalPieces == 32)
//        {
//            Console.WriteLine($"Game phase: {CurrentGamePhase}");
//            return GamePhase.Opening;
//        }

//        // King vs rook endgame
//        else if (totalPieces == 3 && (whiteRooks == 1 || blackRooks == 1))
//        {
//            Console.WriteLine($"Game phase: {CurrentGamePhase}");
//            return GamePhase.KRK;
//        }

//        else if (totalPieces == 3 && (whiteQueens == 1 || blackQueens == 1))
//        {
//            Console.WriteLine($"Game phase: {CurrentGamePhase}");
//            return GamePhase.KQK;
//        }

//        // Middle game: queens still on board, but some trades
//        if (totalPieces < 32 && totalPieces > 24 && whiteQueens >= 1 && blackQueens >= 1)
//        {
//            return GamePhase.MiddleGame;
//        }


//        return GamePhase.None;
//    }

//}
/*
* Inspired by Code monkey King channel
* * MOVE ORDERING MAP   
* 1. PV Move
* 2. Captures in MVV-LVA order  
* 3. 1st Killer moves
* 4. 2nd Killer moves
* 5. History moves
* 6. Unsorted moves
*/

/*
     To implement threefold repetition, we need a unique position identifier. 
     By using a hash key to identify each position, we can also implement a transposition table, 
     which will improve the engine’s accuracy and overall search speed.

*/