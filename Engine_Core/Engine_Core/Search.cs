using Engine;
using System;
using System.Collections.Generic;
using static Engine_Core.Enumes;

namespace Engine_Core
{
    public static class Search
    {
        // TT flag constants
        private const int FLAG_ALPHA = 0;
        private const int FLAG_BETA = 1;
        private const int FLAG_EXACT = 2;

        // Transposition Table
        public static Dictionary<ulong, PositionScoreInDepth> TranspositionTable = new Dictionary<ulong, PositionScoreInDepth>();

        public static ulong PositionHashKey { get; set; }
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

        // Boundaries for evaluation scores
        private const int NEG_INF = -50000;
        private const int POS_INF = 50000;

        // **********************************************   ZOBRIST  HASHING 

        // Random piece keys [piece, square]
        public static ulong[,] pieceKeysOnSquare = new ulong[12, 64];
        // Random en-passant keys 
        public static ulong[] enpassantKey = new ulong[64];
        // Random side key 
        public static ulong sideKey;
        // Random castling keys 
        public static ulong[] castlingKeys = new ulong[16];
        // Position key (almost unique identifier)
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
            for (int square = 0; square < 64; square++)
            {
                enpassantKey[square] = Globals.GetFixedRandom64Numbers();
            }

            // Side key 
            sideKey = Globals.GetFixedRandom64Numbers();

            // Castling keys 
            for (int index = 0; index < 16; index++)
            {
                castlingKeys[index] = Globals.GetFixedRandom64Numbers();
            }
        }

        // Generate hash key for the current board position.
        public static ulong GeneratepositionHashKey()
        {
            positionHashKey = 0UL;
            // Loop over pieces on board
            for (int piece = (int)Pieces.P; piece <= (int)Pieces.k; piece++)
            {
                ulong pieceBitboard = Boards.Bitboards[piece];
                while (pieceBitboard != 0)
                {
                    int square = Globals.GetLs1bIndex(pieceBitboard);
                    Globals.PopBit(ref pieceBitboard, square);
                    positionHashKey ^= pieceKeysOnSquare[piece, square];
                }
            }
            // Do not hash en-passant in this version (as you decided)
            // Hash castling rights
            positionHashKey ^= castlingKeys[Boards.CastlePerm];
            // Hash side if black to move
            if (Boards.Side == (int)Colors.black)
            {
                positionHashKey ^= sideKey;
            }
            return positionHashKey;
        }

        // **********************************************   NEGAMAX WITH TT 

        // Negamax search with iterative deepening.
        public static int GetBestMoveWithIterativeDeepening(int maxDepth, int maxTimeSeconds)
        {
            
            //GeneratepositionHashKey();

            int score = 0;
            nodes = 0;
            ply = 0;
            int bestScore = NEG_INF;
            int bestMove = 0;
            var startTime = DateTime.UtcNow;

            ClearKillerAndHistoryMoves();
            ClearPV();

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

                if (Math.Abs(score) >= 48000)
                {
                    bestMove = pvTable[0, 0];
                    Console.WriteLine($"info string Found forced mate at depth {currentDepth}. Stopping search.");
                    return bestMove;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = pvTable[0, 0];
                }

                if ((DateTime.UtcNow - depthStartTime).TotalSeconds >= maxTimeSeconds)
                {
                    Console.WriteLine($"info string Depth {currentDepth} took too long ({maxTimeSeconds}s), going deeper.");
                    continue;
                }

                if ((DateTime.UtcNow - startTime).TotalSeconds >= maxTimeSeconds * maxDepth)
                {
                    Console.WriteLine($"info string Max time reached ({maxTimeSeconds * maxDepth}s). Stopping search.");
                    break;
                }
            }

            // Final search at maximum depth.
            score = Negamax(-50000, 50000, maxDepth);
            Console.WriteLine($"info score cp {score} depth {maxDepth} nodes {nodes} pv {PrintPVLine()}");
            bestMove = pvTable[0, 0];
            Console.WriteLine($"bestmove {Globals.MoveToString(bestMove)}");

            return bestMove;
        }

        private static void FlagCheckmate(MoveObjects moveList)
        {
            if (moveList.counter == 0)
            {
                if (Boards.Side == (int)Colors.white)
                    Boards.whiteCheckmate = true;
                else
                    Boards.blackCheckmate = true;
            }
        }

        private static int Negamax(int alpha, int beta, int depth)
        {
            // Store current ply's PV length.
            pvLength[ply] = ply;

            // Recompute the current position hash key for this board state.
            ulong currentKey = GeneratepositionHashKey();

            // TT Lookup: Only use the entry if its stored depth exactly matches the current depth.
            if (TranspositionTable.TryGetValue(currentKey, out PositionScoreInDepth ttEntry))
            {
                if(depth >= 6)
                {
                    Console.WriteLine($"TT hit: Hash={currentKey}, Depth={ttEntry.depth}");
                }
                
                if (ttEntry.depth == depth)
                {
                    return ttEntry.score;
                }
            }
            //else
            //{

            //    Console.WriteLine($"TT miss: Hash={currentKey}");
            //}

            // Terminal condition: if at leaf node, do quiescence search.
            if (depth == 0)
                return Quiescence(alpha, beta);

            // Safety check for maximum ply.
            if (ply > maxPly - 1)
                return Evaluators.GetByMaterialAndPosition(Boards.Bitboards);

            nodes++;

            // Determine if the side to move is in check.
            bool inCheck = false;
            if (Boards.Side == (int)Colors.white)
            {
                int whiteKingSq = Globals.GetLs1bIndex(Boards.Bitboards[(int)Pieces.K]);
                if (Attacks.IsSquareAttacked(whiteKingSq, Colors.black) > 0)
                    inCheck = true;
            }
            else
            {
                int blackKingSq = Globals.GetLs1bIndex(Boards.Bitboards[(int)Pieces.k]);
                if (Attacks.IsSquareAttacked(blackKingSq, Colors.white) > 0)
                    inCheck = true;
            }

            // If in check, extend search depth by one.
            if (inCheck)
                depth++;

            MoveObjects moveList = new MoveObjects();
            MoveGenerator.GenerateMoves(moveList);

            if (moveList.counter == 0) FlagCheckmate(moveList);

            // Order moves (using your sorting heuristic).
            SortMoves(moveList);

            int legalMoves = 0;
            int oldAlpha = alpha;
            int bestMove = 0;
            int moveSearched = 0;
            int i = 0;

            while (i < moveList.counter)
            {
                int move = moveList.moves[i];
                // Save board state.
                MoveGenerator.CopyGameState(out ulong[] bbCopy, out ulong[] occCopy, out Colors sideCopy, out int castleCopy, out int enpassCopy);

                // Skip illegal moves.
                if (!MoveGenerator.IsLegal(move, false))
                {
                    MoveGenerator.RestoreGameState(bbCopy, occCopy, sideCopy, castleCopy, enpassCopy);
                    i++;
                    continue;
                }

                legalMoves++;
                moveSearched++;
                ply++;

                bool isCapture = MoveGenerator.GetMoveCapture(move);
                int promoted = MoveGenerator.GetMovePromoted(move);
                bool canReduce = moveSearched > FullDepthMoves && depth > ReductionLimit && !inCheck && !isCapture && promoted == 0;
                int newDepth = depth - 1;
                int score = 0;
                if (canReduce)
                {
                    score = -Negamax(-beta, -alpha, depth - 1);
                    if (score > alpha)
                        score = -Negamax(-beta, -alpha, newDepth);
                }
                else
                {
                    score = -Negamax(-beta, -alpha, newDepth);
                }
                ply--;

                // Restore board state.
                MoveGenerator.RestoreGameState(bbCopy, occCopy, sideCopy, castleCopy, enpassCopy);

                if (score >= beta)
                {
                    if (!MoveGenerator.GetMoveCapture(move))
                    {
                        killerMoves[1, ply] = killerMoves[0, ply];
                        killerMoves[0, ply] = move;
                    }
                    // Store TT entry as a beta cutoff using the current key.
                    TranspositionTable[currentKey] = new PositionScoreInDepth
                    {
                        depth = depth,
                        score = beta,
                        bestMove = move,
                        PositionHashKey = currentKey
                    };
                    return beta;
                }
                if (score > alpha)
                {
                    alpha = score;
                    bestMove = move;
                    if (!MoveGenerator.GetMoveCapture(move))
                    {
                        int piece = MoveGenerator.GetMovePiece(move);
                        int targetSq = MoveGenerator.GetMoveTarget(move);
                        historyMoves[piece, targetSq] += depth;
                    }
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

            if (legalMoves == 0)
            {
                // No legal moves: checkmate or stalemate.
                return inCheck ? (-49000 + ply) : 0;
            }

            // Store the TT entry using the current key.
            TranspositionTable[currentKey] = new PositionScoreInDepth
            {
                depth = depth,
                score = alpha,
                bestMove = bestMove,
                PositionHashKey = currentKey
            };

            return alpha;
        }


        // Using Insertion sort to order moves.
        public static void SortMoves(MoveObjects movelist)
        {
            int count = movelist.counter;
            int[] scores = new int[count];
            for (int i = 0; i < count; i++)
            {
                scores[i] = ScoreMove(movelist.moves[i]);
            }
            for (int i = 1; i < count; i++)
            {
                int keyMove = movelist.moves[i];
                int keyScore = scores[i];
                int j = i - 1;
                while (j >= 0 && scores[j] < keyScore)
                {
                    movelist.moves[j + 1] = movelist.moves[j];
                    scores[j + 1] = scores[j];
                    j--;
                }
                movelist.moves[j + 1] = keyMove;
                scores[j + 1] = keyScore;
            }
        }

        // Score move using MVV-LVA plus killer and history heuristics.
        public static int ScoreMove(int move)
        {
            bool capture = MoveGenerator.GetMoveCapture(move);
            if (capture)
            {
                int attacker = MoveGenerator.GetMovePiece(move);
                int victim = FindVictimPiece(move);
                return MvvLLvaTable[attacker, victim] + 10000;
            }
            else
            {
                if (killerMoves[0, ply] == move)
                    return 9000;
                else if (killerMoves[1, ply] == move)
                    return 8000;
                else
                {
                    int piece = MoveGenerator.GetMovePiece(move);
                    int target = MoveGenerator.GetMoveTarget(move);
                    return historyMoves[piece, target];
                }
            }
        }

        // Find the victim piece on target square.
        private static int FindVictimPiece(int move)
        {
            int targetSquare = MoveGenerator.GetMoveTarget(move);
            int startPiece, endPiece;
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
            for (int i = startPiece; i <= endPiece; i++)
            {
                if (Globals.GetBit(Boards.Bitboards[i], targetSquare))
                    return i;
            }
            return (int)Pieces.P; // fallback
        }

        // Quiescence search.
        private static int Quiescence(int alpha, int beta)
        {
            nodes++;
            int eval = Evaluators.GetByMaterialAndPosition(Boards.Bitboards);
            if (eval >= beta)
                return beta;
            if (eval > alpha)
                alpha = eval;
            MoveObjects moveList = new MoveObjects();
            MoveGenerator.GenerateMoves(moveList);
            SortMoves(moveList);
            int i = 0;
            while (i < moveList.counter)
            {
                int move = moveList.moves[i];
                if (!MoveGenerator.GetMoveCapture(move))
                {
                    i++;
                    continue;
                }
                MoveGenerator.CopyGameState(out ulong[] bbCopy, out ulong[] occCopy, out Colors sideCopy, out int castleCopy, out int enpassCopy);
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
                    return beta;
                if (score > alpha)
                    alpha = score;
                i++;
            }
            return alpha;
        }

        private static void ClearKillerAndHistoryMoves()
        {
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 64; j++)
                    killerMoves[i, j] = 0;
            for (int p = 0; p < 12; p++)
                for (int s = 0; s < 64; s++)
                    historyMoves[p, s] = 0;
        }

        private static void ClearPV()
        {
            for (int i = 0; i < 64; i++)
            {
                pvLength[i] = 0;
                for (int j = 0; j < 64; j++)
                    pvTable[i, j] = 0;
            }
        }

        private static string PrintPVLine()
        {
            int length = pvLength[0];
            string line = "";
            for (int i = 0; i < length; i++)
            {
                int move = pvTable[0, i];
                line += Globals.MoveToString(move) + " ";
            }
            return line;
        }

        // MVV-LVA table.
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

    // Structure to hold TT entries.
    public struct PositionScoreInDepth
    {
        public int depth;
        public int score;
        public ulong PositionHashKey;
        public int flag;
        public int bestMove;
        public override bool Equals(object obj)
        {
            return obj is PositionScoreInDepth other &&
                   depth == other.depth &&
                   PositionHashKey == other.PositionHashKey;
        }
        public override int GetHashCode() => HashCode.Combine(depth, PositionHashKey);
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
    In order to implement threefold repetition we need to have unique position identifier. 
    And using the key "Hash key" to identify position we can additionally implement transposition table 
    Which will improve acurace and speed of search and overal the engine. 
 */