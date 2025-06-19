using Engine;
using Microsoft.Extensions.Logging;
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

    //One of the factors in determining the game phase.
    private static int CountPieces()
    {
        int total = 0;
        for (int piece = 0; piece < Boards.Bitboards.Length; piece++)
        {
            total += BitOperations.PopCount(Boards.Bitboards[piece]);
        }
        return total;
    }

    // *****************************************    Iterative Deepening Search Negamax entry ***************************************************** //

    public static int GetBestMoveWithIterativeDeepening(int maxTimeSeconds, int maxDepth)
    {
        GetGamePhase(); 

        MoveObjects moveList = new MoveObjects();
        MoveGenerator.GenerateMoves(moveList);
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

            int score = Negamax(-50000, 50000, currentDepth);

            Console.WriteLine($"Depth:{currentDepth} Nodes:{nodes} Score:{score} Time:{(DateTime.UtcNow - depthStartTime).TotalSeconds}Sec Pv:{PrintPVLine()}");
            
            ExecutablePv.Clear();
            for (int i = 0; i < pvLength[0]; i++)
                ExecutablePv.Add(pvTable[0, i]);

            bestMove = pvTable[0, 0];

            if ((DateTime.UtcNow - startTime).TotalSeconds >= maxTimeSeconds)
            {
                Console.WriteLine($"Max time reached ({maxTimeSeconds * maxDepth}s). Stopping search.");
                Console.WriteLine($"bestmove {Globals.MoveToString(bestMove)}");
                bestMove = pvTable[0, 0];
                //return bestMove;
            }

        }

        return bestMove;
    }
    // **********************************************************************************************  Negamax
    private static int Negamax(int alpha, int beta, int depth)
    {
        if (TranspositionSwitch)
        {   //--- I dont know why when entry.depth >= depth, engine will stop after finding the right move!
            if (transpositionTable.TryGetValue(positionHashKey, out var entry) && entry.depth == depth)
            {
                if (depth >= 8) Console.WriteLine($"Hit! Key:{positionHashKey} - depth: {entry.depth} - score: {entry.score}");

                if (entry.flag == NodeType.Exact) return entry.score;

                else if (entry.flag == NodeType.Alpha && entry.score <= alpha) return alpha;

                else if (entry.flag == NodeType.Beta && entry.score >= beta) return beta;

            }
        }
        pvLength[ply] = ply;

        if (depth == 0) return Quiescence(alpha, beta);

        nodes++;

        MoveObjects moveList = new MoveObjects();
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
    public static GamePhase GetGamePhase()
    {
        int numberOfPieces = CountPieces();

        // Full starting position
        if (numberOfPieces == 32)
        {
            Console.WriteLine("\nGamePhase: Opening\n");
            return GamePhase.Opening;
        }

        // Simplified check for pure endgames
        if (numberOfPieces <= 3)
        {
            bool whiteHasPawn = Boards.Bitboards[(int)Pieces.P] != 0;
            bool blackHasPawn = Boards.Bitboards[(int)Pieces.p] != 0;
            bool whiteHasBishop = MoveGenerator.wb == 1;
            bool blackHasBishop = MoveGenerator.bb == 1;
            bool whiteHasKnight = MoveGenerator.wn == 1;
            bool blackHasKnight = MoveGenerator.bn == 1;

            // King and Pawn vs King
            if ((whiteHasPawn && numberOfPieces == 3) || (blackHasPawn && numberOfPieces == 3))
            {
                Console.WriteLine("King and Pawn vs King");
                return GamePhase.KPvK;
            }
            // King and Bishop vs King
            if ((whiteHasBishop && numberOfPieces == 3) || (blackHasBishop && numberOfPieces == 3))
            {
                Console.WriteLine("King and Bishop vs King");
                return GamePhase.KBvK;
            }
            // King and Knight + Bishop vs King
            if ((MoveGenerator.wn == 1 && MoveGenerator.wb == 1 && numberOfPieces == 4) ||
                (MoveGenerator.bn == 1 && MoveGenerator.bb == 1 && numberOfPieces == 4))
            {
                Console.WriteLine("King and Knight + Bishop vs King");
                return GamePhase.KBNvK;
            }
            // Two bishops vs king
            if ((MoveGenerator.wb == 2 && numberOfPieces == 4) ||
                (MoveGenerator.bb == 2 && numberOfPieces == 4))
            {
                Console.WriteLine("Two bishops vs king");
                return GamePhase.K2BvK;
            }

            Console.WriteLine("Unspecified End game");
            return GamePhase.EndGame;
        }

        // Midgame: some trades but queens still around
        if ((numberOfPieces < 32 && numberOfPieces > 24) && MoveGenerator.wq >= 1 && MoveGenerator.bq >= 1)
        {
            Console.WriteLine("\nGamePhase: Middle game\n");
            return GamePhase.MiddleGame;
        }

        // Console.WriteLine("\nGamePhase: EndGame\n");
        return GamePhase.EndGame;
    }

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
    To implement threefold repetition, we need a unique position identifier. 
    By using a hash key to identify each position, we can also implement a transposition table, 
    which will improve the engine’s accuracy and overall search speed.

 */