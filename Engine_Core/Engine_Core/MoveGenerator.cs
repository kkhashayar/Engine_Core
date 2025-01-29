using static Engine_Core.Enumes;

namespace Engine_Core;

public class MoveObjects
{
    public int[] moves = new int[256];
    public int counter = 0;
    public MoveObjects()
    {
    }
};

public static class MoveGenerator
{
   
    public static readonly int[] CastlingRights = new int[]
    {
         7, 15, 15, 15,  3, 15, 15, 11,
        15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15,
        13, 15, 15, 15, 12, 15, 15, 14
    };
    public static List<int> ListOfMoves = new List<int>();


    public static int moveIndex = 0;
    public static int GetMoveStartSquare(int move) => move & 0x3f;
    public static int GetMoveTarget(int move) => (move & 0xfc0) >> 6;
    public static int GetMovePiece(int move) => (move & 0xf000) >> 12;
    public static int GetMovePromoted(int move) => (move & 0xf0000) >> 16;
    public static bool GetMoveCapture(int move) => (move & 0x100000) != 0;
    public static bool GetMoveDouble(int move) => (move & 0x200000) != 0;
    public static bool GetMoveEnpassant(int move) => (move & 0x400000) != 0;
    public static bool GetMoveCastling(int move) => (move & 0x800000) != 0;

    public static void CopyGameState(out ulong[] bitboardsCopy, out ulong[] occupanciesCopy, out Colors sideCopy, out int castlePermCopy, out int enpassantSquareCopy)
    {
        // Saving the current state of the game   
        bitboardsCopy = new ulong[12];
        occupanciesCopy = new ulong[3];
        Array.Copy(Boards.Bitboards, bitboardsCopy, 12);
        Array.Copy(Boards.OccupanciesBitBoards, occupanciesCopy, 3);
        sideCopy = (Colors)Boards.Side;
        castlePermCopy = Boards.CastlePerm;
        enpassantSquareCopy = Boards.EnpassantSquare;
    }

    public static void RestoreGameState(ulong[] bitboardsCopy, ulong[] occupanciesCopy, Colors sideCopy, int castlePermCopy, int enpassantSquareCopy)
    {
        Array.Copy(bitboardsCopy, Boards.Bitboards, 12);
        Array.Copy(occupanciesCopy, Boards.OccupanciesBitBoards, 3);
        Boards.Side = (int)sideCopy;
        Boards.CastlePerm = castlePermCopy;
        Boards.EnpassantSquare = enpassantSquareCopy;
    }

    public static Dictionary<int, char> promotedPieces = new Dictionary<int, char>
{
    { (int)Pieces.Q, 'q' },
    { (int)Pieces.R, 'r' },
    { (int)Pieces.B, 'b' },
    { (int)Pieces.N, 'n' },
    { (int)Pieces.q, 'q' },
    { (int)Pieces.r, 'r' },
    { (int)Pieces.b, 'b' },
    { (int)Pieces.n, 'n' }
};

    public static void PrintMoveList(MoveObjects moveList)
    {
        if (moveList.counter == 0)
        {
            Console.WriteLine("\n     No move in the move list!");
            return;
        }

        Console.WriteLine("\n     move    piece     capture   double    enpass    castling\n");

        for (int i = 0; i < moveList.counter; i++)
        {
            int move = moveList.moves[i];
            int source = MoveGenerator.GetMoveStartSquare(move);
            int target = MoveGenerator.GetMoveTarget(move);
            int promoted = MoveGenerator.GetMovePromoted(move);
            int piece = MoveGenerator.GetMovePiece(move);

            char pieceChar = Enumes.AsciiPieces[0][piece];

            char promotedChar = ' ';
            if (promoted != 0)
            {
                promotedChar = MoveGenerator.promotedPieces[promoted];
            }

            int captureVal = 0;
            if (MoveGenerator.GetMoveCapture(move)) captureVal = 1;

            int doubleVal = 0;
            if (MoveGenerator.GetMoveDouble(move)) doubleVal = 1;

            int enpassVal = 0;
            if (MoveGenerator.GetMoveEnpassant(move)) enpassVal = 1;

            int castleVal = 0;
            if (MoveGenerator.GetMoveCastling(move)) castleVal = 1;

            Console.WriteLine(
                "      {0}{1}{2}   {3}         {4}         {5}         {6}         {7}",
                Globals.SquareToCoordinates[source],
                Globals.SquareToCoordinates[target],
                promotedChar,
                pieceChar,
                captureVal,
                doubleVal,
                enpassVal,
                castleVal
            );
        }

        Console.WriteLine("\n\n     Total number of moves: {0}\n", moveList.counter);
    }



    public static readonly char[] PromotedPieces = new char[]
    {
        '-', // 0: No promotion
        'q', // 1: Queen
        'r', // 2: Rook
        'b', // 3: Bishop
        'n'  // 4: Knight
    };


    // Print move for UCI purposes
    public static void PrintMove(int move)
    {
        string moveString = Globals.SquareToCoordinates[GetMoveStartSquare(move)] +
                            Globals.SquareToCoordinates[GetMoveTarget(move)];

        // Check if the move is a promotion
        int promotedPiece = GetMovePromoted(move);
        if (promotedPiece != 0) // 0 means no promotion 
        {
            char promotedChar = PromotedPieces[promotedPiece];
            moveString += promotedChar;
        }

        Console.WriteLine(moveString);
    }

    public static void AddMove(MoveObjects moveList, int move)
    {
        if(move != 0 &&(GetMoveStartSquare(move) != GetMoveTarget(move)))
        {
            moveList.moves[moveList.counter] = move;
            moveList.counter++;
        }
        
    }


    public static void DecodeMove(int move, out int sourceSquare, out int targetSquare, out int piece, out int promotedPiece, out bool isCapture, out bool isDoublePush, out bool isEnPassant, out bool isCastling)
    {
        sourceSquare = GetMoveStartSquare(move);
        targetSquare = GetMoveTarget(move);
        piece = GetMovePiece(move);
        promotedPiece = GetMovePromoted(move);
        isCapture = GetMoveCapture(move);
        isDoublePush = GetMoveDouble(move);
        isEnPassant = GetMoveEnpassant(move);
        isCastling = GetMoveCastling(move);
    }
    public static int EncodeMove(int source, int target, int piece, int promoted, bool capture, bool isDouble, bool enpassant, bool castling)
    {
        return source
            | (target << 6)
            | (piece << 12)
            | (promoted << 16)
            | (Convert.ToInt32(capture) << 20)
            | (Convert.ToInt32(isDouble) << 21)
            | (Convert.ToInt32(enpassant) << 22)
            | (Convert.ToInt32(castling) << 23);
    }

    //**********************  Make a move function  ***********************
    public static bool IsLegal(int move, bool onlyCaptures = false)
    {
        if (move == 0 || GetMoveStartSquare(move) == GetMoveTarget(move)) return false;


        // only for captures
        if (onlyCaptures)
        {
            if (!GetMoveCapture(move))
            {
                return false;
            }
        }

        // Copy current state
        ulong[] bitboardsCopy, occupanciesCopy;
        Colors sideCopy;
        int castlePermCopy, enpassantSquareCopy;
        CopyGameState(out bitboardsCopy, out occupanciesCopy, out sideCopy, out castlePermCopy, out enpassantSquareCopy);

        // Decode move
        int sourceSquare = GetMoveStartSquare(move);
        int targetSquare = GetMoveTarget(move);
        int piece = GetMovePiece(move);
        int promoted = GetMovePromoted(move);
        bool capture = GetMoveCapture(move);
        bool doublePush = GetMoveDouble(move);
        bool enPass = GetMoveEnpassant(move);
        bool castling = GetMoveCastling(move);

        // Remove piece from source
        Globals.PopBit(ref Boards.Bitboards[piece], sourceSquare);
        // Place piece on target
        Globals.SetBit(ref Boards.Bitboards[piece], targetSquare);

        // Handle captures
        if (capture)
        {
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

            for (int bbPiece = startPiece; bbPiece <= endPiece; bbPiece++)
            {
                if (Globals.GetBit(Boards.Bitboards[bbPiece], targetSquare))
                {
                    Globals.PopBit(ref Boards.Bitboards[bbPiece], targetSquare);
                    break;
                }
            }
        }

        // Handle promotions
        if (promoted != 0)
        {
            if (Boards.Side == (int)Colors.white)
            {
                Globals.PopBit(ref Boards.Bitboards[(int)Pieces.P], targetSquare);
            }
            else
            {
                Globals.PopBit(ref Boards.Bitboards[(int)Pieces.p], targetSquare);
            }
            Globals.SetBit(ref Boards.Bitboards[promoted], targetSquare);
        }

        // Handle en passant
        if (enPass)
        {
            if (Boards.Side == (int)Colors.white)
            {
                Globals.PopBit(ref Boards.Bitboards[(int)Pieces.p], targetSquare + 8);
            }
            else
            {
                Globals.PopBit(ref Boards.Bitboards[(int)Pieces.P], targetSquare - 8);
            }
        }

        // Reset en-passant
        Boards.EnpassantSquare = (int)Squares.NoSquare;

        // Double pawn push sets en-passant square
        if (doublePush)
        {
            if (Boards.Side == (int)Colors.white)
            {
                Boards.EnpassantSquare = targetSquare + 8;
            }
            else
            {
                Boards.EnpassantSquare = targetSquare - 8;
            }
        }

        // Handle castling
        if (castling)
        {
            if (targetSquare == (int)Squares.g1) // white king side
            {
                Globals.PopBit(ref Boards.Bitboards[(int)Pieces.R], (int)Squares.h1);
                Globals.SetBit(ref Boards.Bitboards[(int)Pieces.R], (int)Squares.f1);
            }
            else if (targetSquare == (int)Squares.c1) // white queen side
            {
                Globals.PopBit(ref Boards.Bitboards[(int)Pieces.R], (int)Squares.a1);
                Globals.SetBit(ref Boards.Bitboards[(int)Pieces.R], (int)Squares.d1);
            }
            else if (targetSquare == (int)Squares.g8) // black king side
            {
                Globals.PopBit(ref Boards.Bitboards[(int)Pieces.r], (int)Squares.h8);
                Globals.SetBit(ref Boards.Bitboards[(int)Pieces.r], (int)Squares.f8);
            }
            else if (targetSquare == (int)Squares.c8) // black queen side
            {
                Globals.PopBit(ref Boards.Bitboards[(int)Pieces.r], (int)Squares.a8);
                Globals.SetBit(ref Boards.Bitboards[(int)Pieces.r], (int)Squares.d8);
            }
        }

        // Update castling rights
        Boards.CastlePerm &= CastlingRights[sourceSquare];
        Boards.CastlePerm &= CastlingRights[targetSquare];

        // Recompute occupancies
        Array.Clear(Boards.OccupanciesBitBoards, 0, Boards.OccupanciesBitBoards.Length);
        for (int i = (int)Pieces.P; i <= (int)Pieces.K; i++)
        {
            Boards.OccupanciesBitBoards[(int)Colors.white] |= Boards.Bitboards[i];
        }
        for (int i = (int)Pieces.p; i <= (int)Pieces.k; i++)
        {
            Boards.OccupanciesBitBoards[(int)Colors.black] |= Boards.Bitboards[i];
        }
        Boards.OccupanciesBitBoards[(int)Colors.both] = Boards.OccupanciesBitBoards[(int)Colors.white] |
                                                        Boards.OccupanciesBitBoards[(int)Colors.black];

        // Switch side
        if (Boards.Side == (int)Colors.white)
        {
            Boards.Side = (int)Colors.black;
        }
        else
        {
            Boards.Side = (int)Colors.white;
        }

        // Check if our king is in check after move
        int kingSquare;
        if (Boards.Side == (int)Colors.white)
        {
            kingSquare = Globals.GetLs1bIndex(Boards.Bitboards[(int)Pieces.k]);
            if (kingSquare != -1)
            {
                if (Attacks.IsSquareAttacked(kingSquare, Colors.white) != 0)
                {
                    RestoreGameState(bitboardsCopy, occupanciesCopy, sideCopy, castlePermCopy, enpassantSquareCopy);
                    return false;
                }
            }
        }
        else
        {
            kingSquare = Globals.GetLs1bIndex(Boards.Bitboards[(int)Pieces.K]);
            if (kingSquare != -1)
            {
                if (Attacks.IsSquareAttacked(kingSquare, Colors.black) != 0)
                {
                    RestoreGameState(bitboardsCopy, occupanciesCopy, sideCopy, castlePermCopy, enpassantSquareCopy);
                    return false;
                }
            }
        }
        
        return true;
    }

    //***********************  Move Generator Main Loop  ***************************//
    public static void GenerateMoves(MoveObjects moveList)
    {
        moveList.counter = 0;
        int sourceSquare, targetSquare;
        ulong bitboard, attacks;

        for (int piece = (int)Enumes.Pieces.P; piece <= (int)Enumes.Pieces.k; piece++)
        {
            bitboard = Boards.Bitboards[piece];

            if (Boards.Side == (int)Enumes.Colors.white)
            {
                if (piece == (int)Enumes.Pieces.P)
                {
                    while (bitboard != 0UL)
                    {
                        sourceSquare = Globals.GetLs1bIndex(bitboard);
                        targetSquare = sourceSquare - 8;
                        if (targetSquare >= (int)Enumes.Squares.a8 &&
                            !Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], targetSquare))
                        {
                            if (sourceSquare >= (int)Enumes.Squares.a7 && sourceSquare <= (int)Enumes.Squares.h7)
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.Q, false, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.R, false, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.B, false, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.N, false, false, false, false));
                            }
                            else
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                                if (sourceSquare >= (int)Enumes.Squares.a2 && sourceSquare <= (int)Enumes.Squares.h2 &&
                                    !Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], targetSquare - 8))
                                {
                                    MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare - 8, piece, 0, false, true, false, false));
                                }
                            }
                        }
                        attacks = Attacks.PawnAttacks[(int)Enumes.Colors.white, sourceSquare] & Boards.OccupanciesBitBoards[(int)Enumes.Colors.black];
                        while (attacks != 0UL)
                        {
                            targetSquare = Globals.GetLs1bIndex(attacks);
                            if (sourceSquare >= (int)Enumes.Squares.a7 && sourceSquare <= (int)Enumes.Squares.h7)
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.Q, true, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.R, true, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.B, true, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.N, true, false, false, false));
                            }
                            else
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                            }
                            Globals.PopBit(ref attacks, targetSquare);
                        }
                        if (Boards.EnpassantSquare != (int)Enumes.Squares.NoSquare)
                        {
                            ulong enpassantAttacks = Attacks.PawnAttacks[(int)Enumes.Colors.white, sourceSquare] &
                                                     (1UL << Boards.EnpassantSquare);
                            if (enpassantAttacks != 0UL)
                            {
                                int targetEnpassant = Globals.GetLs1bIndex(enpassantAttacks);
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetEnpassant, piece, 0, true, false, true, false));
                            }
                        }
                        Globals.PopBit(ref bitboard, sourceSquare);
                    }
                }
                if (piece == (int)Enumes.Pieces.K)
                {
                    if ((Boards.CastlePerm & (int)Enumes.Castling.WKCA) != 0)
                    {
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], (int)Enumes.Squares.f1) &&
                            !Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], (int)Enumes.Squares.g1))
                        {
                            if (Attacks.IsSquareAttacked((int)Enumes.Squares.e1, Enumes.Colors.black) == 0 &&
                                Attacks.IsSquareAttacked((int)Enumes.Squares.f1, Enumes.Colors.black) == 0)
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove((int)Enumes.Squares.e1, (int)Enumes.Squares.g1, piece, 0, false, false, false, true));
                            }
                        }
                    }
                    if ((Boards.CastlePerm & (int)Enumes.Castling.WQCA) != 0)
                    {
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], (int)Enumes.Squares.d1) &&
                            !Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], (int)Enumes.Squares.c1) &&
                            !Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], (int)Enumes.Squares.b1))
                        {
                            if (Attacks.IsSquareAttacked((int)Enumes.Squares.e1, Enumes.Colors.black) == 0 &&
                                Attacks.IsSquareAttacked((int)Enumes.Squares.d1, Enumes.Colors.black) == 0)
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove((int)Enumes.Squares.e1, (int)Enumes.Squares.c1, piece, 0, false, false, false, true));
                            }
                        }
                    }
                }
            }
            else
            {
                if (piece == (int)Enumes.Pieces.p)
                {
                    while (bitboard != 0UL)
                    {
                        sourceSquare = Globals.GetLs1bIndex(bitboard);
                        targetSquare = sourceSquare + 8;
                        if (targetSquare <= (int)Enumes.Squares.h1 &&
                            !Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], targetSquare))
                        {
                            if (sourceSquare >= (int)Enumes.Squares.a2 && sourceSquare <= (int)Enumes.Squares.h2)
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.q, false, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.r, false, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.b, false, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.n, false, false, false, false));
                            }
                            else
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                                if (sourceSquare >= (int)Enumes.Squares.a7 && sourceSquare <= (int)Enumes.Squares.h7 &&
                                    !Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], targetSquare + 8))
                                {
                                    MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare + 8, piece, 0, false, true, false, false));
                                }
                            }
                        }
                        attacks = Attacks.PawnAttacks[(int)Enumes.Colors.black, sourceSquare] & Boards.OccupanciesBitBoards[(int)Enumes.Colors.white];
                        while (attacks != 0UL)
                        {
                            targetSquare = Globals.GetLs1bIndex(attacks);
                            if (sourceSquare >= (int)Enumes.Squares.a2 && sourceSquare <= (int)Enumes.Squares.h2)
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.q, true, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.r, true, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.b, true, false, false, false));
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, (int)Enumes.Pieces.n, true, false, false, false));
                            }
                            else
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                            }
                            Globals.PopBit(ref attacks, targetSquare);
                        }
                        if (Boards.EnpassantSquare != (int)Enumes.Squares.NoSquare)
                        {
                            ulong enpassantAttacks = Attacks.PawnAttacks[(int)Enumes.Colors.black, sourceSquare] &
                                                     (1UL << Boards.EnpassantSquare);
                            if (enpassantAttacks != 0UL)
                            {
                                int targetEnpassant = Globals.GetLs1bIndex(enpassantAttacks);
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetEnpassant, piece, 0, true, false, true, false));
                            }
                        }
                        Globals.PopBit(ref bitboard, sourceSquare);
                    }
                }
                if (piece == (int)Enumes.Pieces.k)
                {
                    if ((Boards.CastlePerm & (int)Enumes.Castling.BKCA) != 0)
                    {
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], (int)Enumes.Squares.f8) &&
                            !Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], (int)Enumes.Squares.g8))
                        {
                            if (Attacks.IsSquareAttacked((int)Enumes.Squares.e8, Enumes.Colors.white) == 0 &&
                                Attacks.IsSquareAttacked((int)Enumes.Squares.f8, Enumes.Colors.white) == 0)
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove((int)Enumes.Squares.e8, (int)Enumes.Squares.g8, piece, 0, false, false, false, true));
                            }
                        }
                    }
                    if ((Boards.CastlePerm & (int)Enumes.Castling.BQCA) != 0)
                    {
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], (int)Enumes.Squares.d8) &&
                            !Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], (int)Enumes.Squares.c8) &&
                            !Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], (int)Enumes.Squares.b8))
                        {
                            if (Attacks.IsSquareAttacked((int)Enumes.Squares.e8, Enumes.Colors.white) == 0 &&
                                Attacks.IsSquareAttacked((int)Enumes.Squares.d8, Enumes.Colors.white) == 0)
                            {
                                MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove((int)Enumes.Squares.e8, (int)Enumes.Squares.c8, piece, 0, false, false, false, true));
                            }
                        }
                    }
                }
            }

            if (Boards.Side == (int)Enumes.Colors.white && piece == (int)Enumes.Pieces.N)
            {
                while (bitboard != 0UL)
                {
                    sourceSquare = Globals.GetLs1bIndex(bitboard);
                    attacks = Attacks.KnightAttacks[sourceSquare] &
                              ~Boards.OccupanciesBitBoards[(int)Enumes.Colors.white];
                    while (attacks != 0UL)
                    {
                        targetSquare = Globals.GetLs1bIndex(attacks);
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.black], targetSquare))
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                        }
                        else
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                        }
                        Globals.PopBit(ref attacks, targetSquare);
                    }
                    Globals.PopBit(ref bitboard, sourceSquare);
                }
            }
            else if (Boards.Side == (int)Enumes.Colors.black && piece == (int)Enumes.Pieces.n)
            {
                while (bitboard != 0UL)
                {
                    sourceSquare = Globals.GetLs1bIndex(bitboard);
                    attacks = Attacks.KnightAttacks[sourceSquare] &
                              ~Boards.OccupanciesBitBoards[(int)Enumes.Colors.black];
                    while (attacks != 0UL)
                    {
                        targetSquare = Globals.GetLs1bIndex(attacks);
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.white], targetSquare))
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                        }
                        else
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                        }
                        Globals.PopBit(ref attacks, targetSquare);
                    }
                    Globals.PopBit(ref bitboard, sourceSquare);
                }
            }

            if (Boards.Side == (int)Colors.white && piece == (int)Pieces.B)
            {
                while (bitboard != 0UL)
                {
                    sourceSquare = Globals.GetLs1bIndex(bitboard);
                    attacks = Attacks.GetBishopAttacks(sourceSquare, Boards.OccupanciesBitBoards[(int)Colors.both]) & ~Boards.OccupanciesBitBoards[(int)Colors.white];
                    while (attacks != 0UL)
                    {
                        targetSquare = Globals.GetLs1bIndex(attacks);
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Colors.black], targetSquare))
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                        }
                        else
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                        }
                        Globals.PopBit(ref attacks, targetSquare);
                    }
                    Globals.PopBit(ref bitboard, sourceSquare);
                }
            }

            else if (Boards.Side == (int)Colors.black && piece == (int)Pieces.b)
            {
                while (bitboard != 0UL)
                {
                    sourceSquare = Globals.GetLs1bIndex(bitboard);
                    attacks = Attacks.GetBishopAttacks(sourceSquare, Boards.OccupanciesBitBoards[(int)Colors.both]) &
                              ~Boards.OccupanciesBitBoards[(int)Colors.black];
                    while (attacks != 0UL)
                    {
                        targetSquare = Globals.GetLs1bIndex(attacks);
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Colors.white], targetSquare))
                        {
                            AddMove(moveList, EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                        }
                        else
                        {
                            AddMove(moveList, EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                        }
                        Globals.PopBit(ref attacks, targetSquare);
                    }
                    Globals.PopBit(ref bitboard, sourceSquare);
                }
            }

            if (Boards.Side == (int)Enumes.Colors.white && piece == (int)Enumes.Pieces.R)
            {
                while (bitboard != 0UL)
                {
                    sourceSquare = Globals.GetLs1bIndex(bitboard);
                    attacks = Attacks.GetRookAttacks(sourceSquare, Boards.OccupanciesBitBoards[(int)Enumes.Colors.both]) &
                              ~Boards.OccupanciesBitBoards[(int)Enumes.Colors.white];
                    while (attacks != 0UL)
                    {
                        targetSquare = Globals.GetLs1bIndex(attacks);
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.black], targetSquare))
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                        }
                        else
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                        }
                        Globals.PopBit(ref attacks, targetSquare);
                    }
                    Globals.PopBit(ref bitboard, sourceSquare);
                }
            }
            else if (Boards.Side == (int)Enumes.Colors.black && piece == (int)Enumes.Pieces.r)
            {
                while (bitboard != 0UL)
                {
                    sourceSquare = Globals.GetLs1bIndex(bitboard);
                    attacks = Attacks.GetRookAttacks(sourceSquare, Boards.OccupanciesBitBoards[(int)Enumes.Colors.both]) &
                              ~Boards.OccupanciesBitBoards[(int)Enumes.Colors.black];
                    while (attacks != 0UL)
                    {
                        targetSquare = Globals.GetLs1bIndex(attacks);
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.white], targetSquare))
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                        }
                        else
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                        }
                        Globals.PopBit(ref attacks, targetSquare);
                    }
                    Globals.PopBit(ref bitboard, sourceSquare);
                }
            }

            if (Boards.Side == (int)Enumes.Colors.white && piece == (int)Enumes.Pieces.Q)
            {
                while (bitboard != 0UL)
                {
                    sourceSquare = Globals.GetLs1bIndex(bitboard);
                    attacks = Attacks.GetQueenAttacks(sourceSquare, Boards.OccupanciesBitBoards[(int)Enumes.Colors.both]) &
                              ~Boards.OccupanciesBitBoards[(int)Enumes.Colors.white];
                    while (attacks != 0UL)
                    {
                        targetSquare = Globals.GetLs1bIndex(attacks);
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.black], targetSquare))
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                        }
                        else
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                        }
                        Globals.PopBit(ref attacks, targetSquare);
                    }
                    Globals.PopBit(ref bitboard, sourceSquare);
                }
            }
            else if (Boards.Side == (int)Enumes.Colors.black && piece == (int)Enumes.Pieces.q)
            {
                while (bitboard != 0UL)
                {
                    sourceSquare = Globals.GetLs1bIndex(bitboard);
                    attacks = Attacks.GetQueenAttacks(sourceSquare, Boards.OccupanciesBitBoards[(int)Enumes.Colors.both]) &
                              ~Boards.OccupanciesBitBoards[(int)Enumes.Colors.black];
                    while (attacks != 0UL)
                    {
                        targetSquare = Globals.GetLs1bIndex(attacks);
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.white], targetSquare))
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                        }
                        else
                        {
                            MoveGenerator.AddMove(moveList, MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                        }
                        Globals.PopBit(ref attacks, targetSquare);
                    }
                    Globals.PopBit(ref bitboard, sourceSquare);
                }
            }

            if (Boards.Side == (int)Colors.white && piece == (int)Pieces.K)
            {
                while (bitboard != 0UL)
                {
                    sourceSquare = Globals.GetLs1bIndex(bitboard);
                    attacks = Attacks.KingAttacks[sourceSquare] &
                              ~Boards.OccupanciesBitBoards[(int)Colors.white];
                    while (attacks != 0UL)
                    {
                        targetSquare = Globals.GetLs1bIndex(attacks);
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Colors.black], targetSquare))
                        {
                            AddMove(moveList, EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                        }
                        else
                        {
                            AddMove(moveList, EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                        }
                        Globals.PopBit(ref attacks, targetSquare);
                    }
                    Globals.PopBit(ref bitboard, sourceSquare);
                }
            }
            else if (Boards.Side == (int)Colors.black && piece == (int)Pieces.k)
            {
                while (bitboard != 0UL)
                {
                    sourceSquare = Globals.GetLs1bIndex(bitboard);
                    attacks = Attacks.KingAttacks[sourceSquare] &
                              ~Boards.OccupanciesBitBoards[(int)Colors.black];
                    while (attacks != 0UL)
                    {
                        targetSquare = Globals.GetLs1bIndex(attacks);
                        if (!Globals.GetBit(Boards.OccupanciesBitBoards[(int)Colors.white], targetSquare))
                        {
                            AddMove(moveList, EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, false));
                        }
                        else
                        {
                            AddMove(moveList, EncodeMove(sourceSquare, targetSquare, piece, 0, true, false, false, false));
                        }
                        Globals.PopBit(ref attacks, targetSquare);
                    }
                    Globals.PopBit(ref bitboard, sourceSquare);
                }
            }
        }
    }
}
/*
 * Move encoding: 
 * sourcesquare  targetsquare  piece  promoted  capture  isdouble  enpassant  Castle  
 */

/*
          binary move bits                               hexidecimal constants
    
    0000 0000 0000 0000 0011 1111    source square       0x3f
    0000 0000 0000 1111 1100 0000    target square       0xfc0
    0000 0000 1111 0000 0000 0000    piece               0xf000
    0000 1111 0000 0000 0000 0000    promoted piece      0xf0000
    0001 0000 0000 0000 0000 0000    capture flag        0x100000
    0010 0000 0000 0000 0000 0000    double push flag    0x200000
    0100 0000 0000 0000 0000 0000    enpassant flag      0x400000
    1000 0000 0000 0000 0000 0000    castling flag       0x800000
*/
