using static Engine_Core.Enumes;

namespace Engine_Core;

public static class Attacks
{

    // Pawn attacks table [side][square]
    public static readonly ulong[,] PawnAttacks = new ulong[2, 64];
    // Knight attacks table [square]
    public static readonly ulong[] KnightAttacks = new ulong[64];
    // King attacks table [square]
    public static readonly ulong[] KingAttacks = new ulong[64];

    public static ulong[] BishopMasks = new ulong[64];
    public static ulong[,] BishopAttacks = new ulong[64, 512];

    public static ulong[] RookMasks = new ulong[64];
    public static ulong[,] RookAttacks = new ulong[64, 4096];

    public static ulong GetQueenAttacks(int square, ulong occupancy)
    {
        // Initialize queen attacks
        return GetBishopAttacks(square, occupancy) | GetRookAttacks(square, occupancy);
    }

    public static ulong GetRookAttacks(int square, ulong occupancy)
    {
        occupancy &= RookMasks[square];
        occupancy *= MagicNumbers.RookMagicNumbers[square];
        occupancy >>= 64 - RookRelevantBitCount[square];

        return RookAttacks[square, occupancy];
    }

    public static ulong GetBishopAttacks(int square, ulong occupancy)
    {
        occupancy &= BishopMasks[square];
        occupancy *= MagicNumbers.BishopMagicNumbers[square];
        occupancy >>= 64 - BishopRelevantBitCount[square];

        return BishopAttacks[square, occupancy];
    }

    // We have to initialize Slider piece attack tables 
    public static void InitBishopsAttacks()
    {
        for (int square = 0; square < 64; square++)
        {
            BishopMasks[square] = Masks.MaskBishopAttacks(square);
            ulong attackMask = BishopMasks[square];

            // Correct bit count without shifting
            int relevantBitcounts = Globals.CountBits(attackMask);
            int occupancyIndices = 1 << relevantBitcounts;

            for (int index = 0; index < occupancyIndices; index++)
            {
                ulong occupancy = Globals.SetOccupancy(index, relevantBitcounts, attackMask);
                int magicIndex = (int)((occupancy * MagicNumbers.BishopMagicNumbers[square]) >> (64 - BishopRelevantBitCount[square]));
                BishopAttacks[square, magicIndex] = BishopAttacksOnTheFly(square, occupancy);
            }
        }
    }


    public static void InitRooksAttacks()
    {
        for (int square = 0; square < 64; square++)
        {
            RookMasks[square] = Masks.MaskRookAttacks(square);
            ulong attackMask = RookMasks[square];

            // Correct bit count without shifting
            int relevantBitcounts = Globals.CountBits(attackMask);
            int occupancyIndices = 1 << relevantBitcounts;

            for (int index = 0; index < occupancyIndices; index++)
            {
                ulong occupancy = Globals.SetOccupancy(index, relevantBitcounts, attackMask);
                int magicIndex = (int)((occupancy * MagicNumbers.RookMagicNumbers[square]) >> (64 - RookRelevantBitCount[square]));
                RookAttacks[square, magicIndex] = RookAttacksOnTheFly(square, occupancy);
            }
        }
    }



    // Rook Relevant occupancy bit count
    public static int[] RookRelevantBitCount = new int[]
    {
        12, 11, 11, 11, 11, 11, 11, 12,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        12, 11, 11, 11, 11, 11, 11, 12
    };

    // Bishop Relevant occupancy bit count 
    public static int[] BishopRelevantBitCount = new int[]
    {
        6, 5, 5, 5, 5, 5, 5, 6,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        6, 5, 5, 5, 5, 5, 5, 6
    };

    public static ulong BishopAttacksOnTheFly(int square, ulong block)
    {
        ulong attacks = 0UL;

        int tr = square / 8;
        int tf = square % 8;

        // Diagonally up-right
        for (int r = tr + 1, f = tf + 1; r <= 7 && f <= 7; r++, f++)
        {
            attacks |= (1UL << (r * 8 + f));
            if (((1UL << (r * 8 + f)) & block) != 0)
                break;
        }

        // Diagonally up-left
        for (int r = tr - 1, f = tf + 1; r >= 0 && f <= 7; r--, f++)
        {
            attacks |= (1UL << (r * 8 + f));
            if (((1UL << (r * 8 + f)) & block) != 0)
                break;
        }

        // Diagonally down-right
        for (int r = tr + 1, f = tf - 1; r <= 7 && f >= 0; r++, f--)
        {
            attacks |= (1UL << (r * 8 + f));
            if (((1UL << (r * 8 + f)) & block) != 0)
                break;
        }

        // Diagonally down-left
        for (int r = tr - 1, f = tf - 1; r >= 0 && f >= 0; r--, f--)
        {
            attacks |= (1UL << (r * 8 + f));
            if (((1UL << (r * 8 + f)) & block) != 0)
                break;
        }

        return attacks;
    }

    // Generate rook attacks on the fly considering blockers
    public static ulong RookAttacksOnTheFly(int square, ulong block)
    {
        ulong attacks = 0UL;

        int tr = square / 8;
        int tf = square % 8;

        // Vertically up
        for (int r = tr + 1; r <= 7; r++)
        {
            attacks |= (1UL << (r * 8 + tf));
            if (((1UL << (r * 8 + tf)) & block) != 0)
                break;
        }

        // Vertically down
        for (int r = tr - 1; r >= 0; r--)
        {
            attacks |= (1UL << (r * 8 + tf));
            if (((1UL << (r * 8 + tf)) & block) != 0)
                break;
        }

        // Horizontally right
        for (int f = tf + 1; f <= 7; f++)
        {
            attacks |= (1UL << (tr * 8 + f));
            if (((1UL << (tr * 8 + f)) & block) != 0)
                break;
        }

        // Horizontally left
        for (int f = tf - 1; f >= 0; f--)
        {
            attacks |= (1UL << (tr * 8 + f));
            if (((1UL << (tr * 8 + f)) & block) != 0)
                break;
        }

        return attacks;
    }

    // Initialize leaper pieces' attack tables
    public static void InitLeapersAttacks()
    {
        for (int square = 0; square < 64; square++)
        {
            // Initialize pawn attacks for both colors
            PawnAttacks[(int)Enumes.Colors.white, square] = Masks.MaskPawnAttacks((int)Enumes.Colors.white, square);
            PawnAttacks[(int)Enumes.Colors.black, square] = Masks.MaskPawnAttacks((int)Enumes.Colors.black, square);

            // Initialize knight attacks
            KnightAttacks[square] = Masks.MaskKnightAttacks(square);

            // Initialize king attacks
            KingAttacks[square] = Masks.MaskKingAttacks(square);
        }
    }



    public static void PrintAttackedSquares(Colors side)
    {
        Console.WriteLine();

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;

                if (file == 0)
                    Console.Write($"  {8 - rank} ");

                int attacked = 0;
                if (IsSquareAttacked(square, side) != 0)
                {
                    attacked = 1;
                }

                Console.Write($" {attacked}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("\n     a b c d e f g h\n");
    }

    // TODO: getting index out of range exception in checkmate positions.
    public static int IsSquareAttacked(int square, Colors side)
    {
        // attacked by white pawns
        if (side == Colors.white && (PawnAttacks[(int)Colors.black, square] & Boards.Bitboards[(int)Pieces.P]) != 0) return 1;

        // attacked by black pawns
        if (side == Colors.black && (PawnAttacks[(int)Colors.white, square] & Boards.Bitboards[(int)Pieces.p]) != 0) return 1;

        // attacked by knights
        if ((KnightAttacks[square] & (side == Colors.white ? Boards.Bitboards[(int)Pieces.N] : Boards.Bitboards[(int)Pieces.n])) != 0) return 1;

        // attacked by bishops
        if ((GetBishopAttacks(square, Boards.OccupanciesBitBoards[(int)Colors.both]) & (side == Colors.white ? Boards.Bitboards[(int)Pieces.B] : Boards.Bitboards[(int)Pieces.b])) != 0) return 1;

        // attacked by rooks
        if ((GetRookAttacks(square, Boards.OccupanciesBitBoards[(int)Colors.both]) & (side == Colors.white ? Boards.Bitboards[(int)Pieces.R] : Boards.Bitboards[(int)Pieces.r])) != 0) return 1;

        // attacked by queens
        if ((GetQueenAttacks(square, Boards.OccupanciesBitBoards[(int)Colors.both]) & (side == Colors.white ? Boards.Bitboards[(int)Pieces.Q] : Boards.Bitboards[(int)Pieces.q])) != 0) return 1;

        // attacked by kings
        if ((KingAttacks[square] & (side == Colors.white ? Boards.Bitboards[(int)Pieces.K] : Boards.Bitboards[(int)Pieces.k])) != 0) return 1;

        // by default return false
        return 0;
    }


}

