using System;

namespace Engine_Core;

public static class Boards
{
    

    public static int SizeOfBitboards = 96;
    public static int SizeOfOccupanciesBitBoards = 24;

    public static ulong NotAFile = 18374403900871474942UL;
    public static ulong NotHFile = 9187201950435737471UL;
    public static ulong NotHgFile = 4557430888798830399UL;
    public static ulong NotAbFile = 18229723555195321596UL;

    // Piece bitboards
    public static ulong[] Bitboards = new ulong[12];

    public static ulong[] OccupanciesBitBoards = new ulong[3];

    public static int CastlePerm { get; set; } = 0;
    public static int EnpassantSquare { get; set; } = (int)Enumes.Squares.NoSquare;
    public static int InitialSide { get; set; } = -1;
    public static int Side { get; set; }

    public static bool whiteCheckmate = false;
    public static bool blackCheckmate = false;

    

    public static void PrintBitboard(ulong bitboard)
    {
        Console.WriteLine();
        // Loop over board ranks (8 to 1)
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;
                // Print rank numbers on the left
                if (file == 0)
                    Console.Write($"  {8 - rank} ");
                // Print bit state (1 or 0)
                Console.Write($" {(Globals.GetBit(bitboard, square) ? 1 : 0)}");
            }
            Console.WriteLine();
        }
        // Print file letters at the bottom
        Console.WriteLine("\n     a b c d e f g h\n");
        Console.WriteLine($"     Bitboard: {bitboard}\n");
    }


    public static void DisplayBoard()
    {
        
        Console.Clear();        
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Black;
        // Loop the ranks
        for (int rank = 0; rank < 8; rank++)
        {
            // Loop over over files
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;
                // Print ranks
                if (file == 0) Console.Write($"  {8 - rank} ");
                // Define piece variable
                int piece = -1;

                // Loop over all piece bitboards
                for (int bbPiece = (int)Enumes.Pieces.P; bbPiece <= (int)Enumes.Pieces.k; bbPiece++)
                {
                    if (Globals.GetBit(Bitboards[bbPiece], square))
                    {
                        piece = bbPiece;
                        break;
                    }
                }

                // Print Unicode or ASCII pieces
                if (piece == -1)
                {
                    Console.Write(" .");
                }
                else
                {
                    Console.Write($" {Enumes.UnicodePieces[piece]}");
                }
            }

            // Print new line for each rank
            Console.WriteLine();
            
        }

        // Print file letters
        Console.WriteLine("\n     a b c d e f g h\n");

        // Print side to move
        Console.Write("     Side:     ");
        if (Side == (int)Enumes.Colors.white)
        {
            Console.WriteLine("white");
        }
        else
        {
            Console.WriteLine("black");
        }

        // Print en passant square
        Console.Write("     Enpassant:   ");
        if (EnpassantSquare != (int)Enumes.Squares.NoSquare)
        {
            Console.WriteLine(Globals.SquareToCoordinates[EnpassantSquare]);
        }
        else
        {
            Console.WriteLine("no");
        }

        // Print castling rights
        Console.Write("     Castling:  ");
        if ((CastlePerm & (int)Enumes.Castling.WKCA) != 0)
        {
            Console.Write("K");
        }
        else
        {
            Console.Write("-");
        }
        if ((CastlePerm & (int)Enumes.Castling.WQCA) != 0)
        {
            Console.Write("Q");
        }
        else
        {
            Console.Write("-");
        }
        if ((CastlePerm & (int)Enumes.Castling.BKCA) != 0)
        {
            Console.Write("k");
        }
        else
        {
            Console.Write("-");
        }
        if ((CastlePerm & (int)Enumes.Castling.BQCA) != 0)
        {
            Console.Write("q");
        }
        else
        {
            Console.Write("-");
        }
        
        Console.ForegroundColor = ConsoleColor.DarkBlue;    
        Console.WriteLine("\n");
        Console.WriteLine($"Position Key: {Search.positionHashKey}");
        Console.WriteLine("------------------------------------");
        Console.ResetColor();
        Console.Beep(1000, 200);
        Console.ResetColor();
        
    }


    // Final Apply Move to the board changes the board pernamently
    public static bool ApplyTheMove(int move)
    {
        // Decode move
        int source = MoveGenerator.GetMoveStartSquare(move);
        int target = MoveGenerator.GetMoveTarget(move);
        int piece = MoveGenerator.GetMovePiece(move);
        int promoted = MoveGenerator.GetMovePromoted(move);
        bool capture = MoveGenerator.GetMoveCapture(move);
        bool doublePush = MoveGenerator.GetMoveDouble(move);
        bool enPassant = MoveGenerator.GetMoveEnpassant(move);
        bool castling = MoveGenerator.GetMoveCastling(move);


        


        // Handle capture first to clear the target square
        if (capture)
        {
            int capturedPiece = GetCapturedPiece(target);
            if (capturedPiece != -1)
            {
                Globals.PopBit(ref Bitboards[capturedPiece], target);
            }
        }

        // Handle en passant capture
        if (enPassant)
        {
            int epSquare;
            int epPiece;
            if (Side == (int)Enumes.Colors.white)
            {
                epSquare = target + 8;
                epPiece = (int)Enumes.Pieces.p;
            }
            else
            {
                epSquare = target - 8;
                epPiece = (int)Enumes.Pieces.P;
            }
            Globals.PopBit(ref Bitboards[epPiece], epSquare);
        }

        // Move the piece
        Globals.PopBit(ref Bitboards[piece], source);

        if (promoted != 0)
        {
            // Handle promotion
            Globals.SetBit(ref Bitboards[promoted], target);
        }
        else
        {
            Globals.SetBit(ref Bitboards[piece], target);
        }

        // Handle castling
        if (castling)
        {
            HandleCastling(move, target);
        }

        // Update castling rights
        UpdateCastlingRights(source, target);

        // Update en passant square
        if (doublePush)
        {
            if (Side == (int)Enumes.Colors.white)
            {
                EnpassantSquare = target + 8;
            }
            else
            {
                EnpassantSquare = target - 8;
            }
        }
        else
        {
            EnpassantSquare = (int)Enumes.Squares.NoSquare;
        }

        // Update occupancies
        UpdateOccupancies();

        // Switch side
        if (Side == (int)Enumes.Colors.white)
        {
            Side = (int)Enumes.Colors.black;
        }
        else
        {
            Side = (int)Enumes.Colors.white;
        }

        return true;
    }




    public static int GetCapturedPiece(int target)
    {
        for (int i = 0; i < Bitboards.Length; i++)
        {
            if (Globals.GetBit(Bitboards[i], target))
                return i;
        }
        return -1;
    }

    private static void HandleCastling(int move, int target)
    {
        if (Side == (int)Enumes.Colors.white)
        {
            if (target == (int)Enumes.Squares.g1)
            {
                Globals.PopBit(ref Bitboards[(int)Enumes.Pieces.R], (int)Enumes.Squares.h1);
                Bitboards[(int)Enumes.Pieces.R] |= (1UL << (int)Enumes.Squares.f1);
            }
            else if (target == (int)Enumes.Squares.c1)
            {
                Globals.PopBit(ref Bitboards[(int)Enumes.Pieces.R], (int)Enumes.Squares.a1);
                Bitboards[(int)Enumes.Pieces.R] |= (1UL << (int)Enumes.Squares.d1);
            }
        }
        else
        {
            if (target == (int)Enumes.Squares.g8)
            {
                Globals.PopBit(ref Bitboards[(int)Enumes.Pieces.r], (int)Enumes.Squares.h8);
                Bitboards[(int)Enumes.Pieces.r] |= (1UL << (int)Enumes.Squares.f8);
            }
            else if (target == (int)Enumes.Squares.c8)
            {
                Globals.PopBit(ref Bitboards[(int)Enumes.Pieces.r], (int)Enumes.Squares.a8);
                Bitboards[(int)Enumes.Pieces.r] |= (1UL << (int)Enumes.Squares.d8);
            }
        }
    }

    private static void UpdateCastlingRights(int source, int target)
    {
        CastlePerm &= MoveGenerator.CastlingRights[source];
        CastlePerm &= MoveGenerator.CastlingRights[target];
    }

    public static void UpdateOccupancies()
    {
        OccupanciesBitBoards[(int)Enumes.Colors.white] = 0UL;
        OccupanciesBitBoards[(int)Enumes.Colors.black] = 0UL;
        for (int i = (int)Enumes.Pieces.P; i <= (int)Enumes.Pieces.K; i++)
        {
            OccupanciesBitBoards[(int)Enumes.Colors.white] |= Bitboards[i];
        }
        for (int i = (int)Enumes.Pieces.p; i <= (int)Enumes.Pieces.k; i++)
        {
            OccupanciesBitBoards[(int)Enumes.Colors.black] |= Bitboards[i];
        }
        OccupanciesBitBoards[(int)Enumes.Colors.both] = OccupanciesBitBoards[(int)Enumes.Colors.white] | OccupanciesBitBoards[(int)Enumes.Colors.black];
    }
}

