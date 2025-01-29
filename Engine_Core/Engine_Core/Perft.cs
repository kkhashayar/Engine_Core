using Engine_Core;
using static Engine_Core.Enumes;
using System.Diagnostics;

public static class PerftTeste
{
    public static long Nodes { get;  set; } = 0;
    private static Stopwatch stopwatch = new Stopwatch();

    public static void RunPerft(int depth, bool subdivide = false)
    {
        Nodes = 0;
        stopwatch.Restart();

        if (subdivide)
        {
            PerftSubdivide(depth);
        }
        else
        {
            PerftDriver(depth);
            stopwatch.Stop();
            Console.WriteLine($"Perft {depth}: {Nodes} nodes");
            Console.WriteLine($"Time taken (ms): {stopwatch.ElapsedMilliseconds}");
        }
    }
    // Print out the list of nodes for each move
    private static void PerftSubdivide(int depth)
    {
        MoveObjects moveList = new MoveObjects();
        MoveGenerator.GenerateMoves(moveList);

        long totalNodes = 0;

        for (int i = 0; i < moveList.counter; i++)
        {
            int move = moveList.moves[i];
            // copy current state of game
            MoveGenerator.CopyGameState(out ulong[] bitboardsCopy, out ulong[] occupanciesCopy, out Colors sideCopy, out int castlePermCopy, out int enpassantSquareCopy);

            if (MoveGenerator.IsLegal(move, false))
            {
                long nodes = 0;
                PerftDriver(depth - 1, ref nodes);

                // restore to previous state of game
                MoveGenerator.RestoreGameState(bitboardsCopy, occupanciesCopy, sideCopy, castlePermCopy, enpassantSquareCopy);

                string moveStr = Globals.MoveToString(move);
                Console.WriteLine($"{moveStr}: {nodes}");
                totalNodes += nodes;
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"\nNodes searched: {totalNodes}");
        Console.WriteLine($"Time taken (ms): {stopwatch.ElapsedMilliseconds}");
    }

    private static void PerftDriver(int depth)
    {
        if (depth == 0)
        {
            Nodes++;
            return;
        }

        MoveObjects moveList = new MoveObjects();
        MoveGenerator.GenerateMoves(moveList);

        
        for (int i = 0; i < moveList.counter; i++)
        {
            int move = moveList.moves[i];

            MoveGenerator.CopyGameState(out ulong[] bitboardsCopy, out ulong[] occupanciesCopy, out Colors sideCopy, out int castlePermCopy, out int enpassantSquareCopy);

            if (MoveGenerator.IsLegal(move, false))
            {
                PerftDriver(depth - 1);
                MoveGenerator.RestoreGameState(bitboardsCopy, occupanciesCopy, sideCopy, castlePermCopy, enpassantSquareCopy);
            }
        }
        MoveGenerator.PrintMoveList(moveList);

    }

    private static void PerftDriver(int depth, ref long nodes)
    {
        if (depth == 0)
        {
            nodes++;
            return;
        }

        MoveObjects moveList = new MoveObjects();
        MoveGenerator.GenerateMoves(moveList);

        for (int i = 0; i < moveList.counter; i++)
        {
            int move = moveList.moves[i];

            MoveGenerator.CopyGameState(out ulong[] bitboardsCopy, out ulong[] occupanciesCopy, out Colors sideCopy, out int castlePermCopy, out int enpassantSquareCopy);

            if (MoveGenerator.IsLegal(move, false))
            {
                PerftDriver(depth - 1, ref nodes);
                MoveGenerator.RestoreGameState(bitboardsCopy, occupanciesCopy, sideCopy, castlePermCopy, enpassantSquareCopy);
            }
        }
    }
}
