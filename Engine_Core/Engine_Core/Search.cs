using Engine;
using System;
using static Engine_Core.Enumes;

namespace Engine_Core
{
    public static class Search
    {
        public static List<int> ExecutablePv = new List<int>(); 
        public static long nodes;
        
        // Max number ply in search 
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

        // Negamax single call – no iterative deepening yet

        public static int GetBestMoveWithIterativeDeepening(int maxDepth)
        {
            int score = 0;
            nodes = 0;
            ply = 0;
            int bestScore = 0;
            int bestMove = 0; 


            ClearKillerAndHistoryMoves();
            ClearPV();  

            for(int currentDepth = 1; currentDepth <= maxDepth; currentDepth++)
            {
                nodes = 0;
                score = Negamax(-50000, 50000, currentDepth);
                Console.WriteLine("info depth " + currentDepth + " score " + score + " nodes " + nodes + " pv " + PrintPVLine());

                ExecutablePv.Clear();
                for (int i = 0; i < pvLength[0]; i++)
                {
                    ExecutablePv.Add(pvTable[0, i]);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = pvTable[0, 0];
                }

                if (bestScore >= 48000)
                {
                    break;
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

        public static int GetBestMove(int depth)
        {
            int score = 0;
            // Iterativee deepening 
            // clear data structures
            ClearKillerAndHistoryMoves(); 
            ClearPV();

            nodes = 0;
            ply = 0;

            // Generate all possible root moves
            MoveObjects moveList = new MoveObjects();
            MoveGenerator.GenerateMoves(moveList);
            
            FlagCheckmate(moveList);

            SortMoves(moveList);

            int alpha = NEG_INF;
            int beta = POS_INF;
            int bestScore = NEG_INF;
            int bestMove = 0;

            // We'll do the root loop ourselves
            for (int i = 0; i < moveList.counter; i++)
            {
                int move = moveList.moves[i];

                // Save board state
                MoveGenerator.CopyGameState(
                    out ulong[] bbCopy, out ulong[] occCopy,
                    out Colors sideCopy, out int castleCopy, out int enpassCopy
                );

                // If this move is not legal, skip it
                bool legal = MoveGenerator.IsLegal(move, false);
                if (!legal)
                {
                    MoveGenerator.RestoreGameState(bbCopy, occCopy, sideCopy, castleCopy, enpassCopy);
                    continue;
                }

                ply++;
                score = -Negamax(-beta, -alpha, depth - 1);
                ply--;

                // Restore board
                MoveGenerator.RestoreGameState(bbCopy, occCopy, sideCopy, castleCopy, enpassCopy);

                // Check if we found a new best move
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;

                    // Update alpha at the root
                    if (score > alpha)
                        alpha = score;

                    // Root PV update:
                    // We know `pvTable[ply]` starts at `ply=0` for root
                    // So we set the first move in the root's PV
                    pvTable[0, 0] = move;
                    int nextPly = 1;
                    // Copy the sub‐PV from child
                    while (nextPly < pvLength[1])
                    {
                        pvTable[0, nextPly] = pvTable[1, nextPly];
                        nextPly++;
                    }
                    pvLength[0] = pvLength[1];

                }

                // Print partial info for *this* move
                // (like “info depth X currmove e2e4 score bestScore …”)
                Console.WriteLine(
                    "info depth " + depth +
                    " currmove " + Globals.MoveToString(move) +
                    " currmovenumber " + i +
                    " nodes " + nodes +
                    " score" + bestScore +
                    " pv " + PrintPVLine()
                );
                if (bestScore >= 48000)
                {
                    Console.WriteLine("info string Found forced mate. Stopping root loop.");
                    
                    break;
                }

            }

            MoveGenerator.PrintMove(bestMove);
            


            return bestMove;
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
            // Keep track of this ply's PV length
            pvLength[ply] = ply;

            if (depth == 0) return Quiescence(alpha, beta);

            // we are too deep, there is an owerflow of arrays relying on max ply constant
            // most likely we are not going to reach this point
            if (ply > maxPly - 1)
            {
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

            // Sort moves by MVV-LVA, killer, history, etc.
            SortMoves(moveList);

            int legalMoves = 0;
            int oldAlpha = alpha;
            int bestMove = 0;

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
                    i++;
                    continue;
                }

                legalMoves++;
                ply++;

                int score = -Negamax(-beta, -alpha, depth - 1);

                ply--;
                MoveGenerator.RestoreGameState(bitboardsCopy, occCopy, sideCopy, castleCopy, enpassCopy);

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

            return alpha;
        }

       

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

        // Sort moves by MVV-LVA, killer, history (bubble sort)
        public static void SortMoves(MoveObjects moveList)
        {
            int count = moveList.counter;

            // Simple array for scores
            int[] scores = new int[count];
            int index = 0;
            while (index < count)
            {
                int move = moveList.moves[index];
                scores[index] = ScoreMove(move);
                index++;
            }

            // Bubble-sort by descending
            int current = 0;
            while (current < count)
            {
                int next = current + 1;
                while (next < count)
                {
                    if (scores[current] < scores[next])
                    {
                        // Swap scores
                        int tempScore = scores[current];
                        scores[current] = scores[next];
                        scores[next] = tempScore;

                        // Swap moves
                        int tempMove = moveList.moves[current];
                        moveList.moves[current] = moveList.moves[next];
                        moveList.moves[next] = tempMove;
                    }
                    next++;
                }
                current++;
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
                return Evaluators.MvvLLvaTable[attacker, victim] + 10000;
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
    }
}
