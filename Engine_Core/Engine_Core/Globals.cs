using System.Diagnostics;
using System.Numerics;
using static Engine_Core.Enumes;
namespace Engine_Core;

public static class Globals
{

    public static readonly Dictionary<string, int> CoordinatesToSquare = SquareToCoordinates
    .Select((coord, index) => new { coord, index }).ToDictionary(x => x.coord, x => x.index);

    public static int GetPieceOnSquare(int square)
    {
        for (int piece = (int)Pieces.P; piece <= (int)Pieces.k; piece++)
        {
            if (GetBit(Boards.Bitboards[piece], square))
                return piece;
        }
        return -1; // No piece found
    }

    
    private static readonly Random random = new Random();
    // Mapping from square index to coordinates
    public static readonly string[] SquareToCoordinates =
    {
        "a8", "b8", "c8", "d8", "e8", "f8", "g8", "h8",
        "a7", "b7", "c7", "d7", "e7", "f7", "g7", "h7",
        "a6", "b6", "c6", "d6", "e6", "f6", "g6", "h6",
        "a5", "b5", "c5", "d5", "e5", "f5", "g5", "h5",
        "a4", "b4", "c4", "d4", "e4", "f4", "g4", "h4",
        "a3", "b3", "c3", "d3", "e3", "f3", "g3", "h3",
        "a2", "b2", "c2", "d2", "e2", "f2", "g2", "h2",
        "a1", "b1", "c1", "d1", "e1", "f1", "g1", "h1",
    };

    //**********************************  Bit methods  **********************************

    // Set a bit on the bitboard
    public static void SetBit(ref ulong bitboard, int square)
    {
        bitboard |= (1UL << square);
    }

    // Get the state of a bit on the bitboard
    public static bool GetBit(ulong bitboard, int square)
    {
        return (bitboard & (1UL << square)) != 0;
    }

    // Pop (toggle) a bit on the bitboard if it's set
    public static void PopBit(ref ulong bitboard, int square)
    {
        bitboard &= ~(1UL << square);
    }


    // Count the number of set bits in the bitboard (Brian Kernighan's way)
    public static int CountBits(ulong bitboard)
    {
        int count = 0;
        while (bitboard != 0)
        {
            count++;
            bitboard &= bitboard - 1;
        }
        return count;
    }

    // Get the index of the least significant set bit (LS1B)
    public static int GetLs1bIndex(ulong bitboard)
    {
        if (bitboard == 0)
            return -1;
        return BitOperations.TrailingZeroCount(bitboard);
    }


    // Set Occupancy bitboard

    public static ulong SetOccupancy(int index, int bitsInMask, ulong attackMask)
    {
        ulong occupancy = 0UL;
        for (int count = 0; count < bitsInMask; count++)
        {
            int square = GetLs1bIndex(attackMask);
            if (square == -1)
                break; // No more bits set in attackMask
            PopBit(ref attackMask, square);
            if ((index & (1 << count)) != 0)
                occupancy |= (1UL << square);
        }
        return occupancy;
    }


    public static ulong GenerateMagicNumberCandidate()
    {
        ulong magicNumber = GetRandomU64Numbers() & GetRandomU64Numbers() & GetRandomU64Numbers();
        return magicNumber;
    }


    // A manual way to generate Bishops magic numbers
    public static ulong FindBishopMagicNumber(int square, int relevantBits)
    {
        // init Occupancies
        ulong[] occupancies = new ulong[4096];

        // init attack tables 
        ulong[] attacks = new ulong[4096];

        // init used attacks
        ulong[] usedAttacks = new ulong[4096];

        // init attack mask for current piece
        ulong attackMask = Masks.MaskBishopAttacks(square);

        //init occupancy indices   
        int occupancyIndices = 1 << relevantBits;

        // loop over occupancy indices
        for (int index = 0; index < occupancyIndices; index++)
        {
            // init occupancy 
            occupancies[index] = SetOccupancy(index, relevantBits, attackMask);

            // init attacks
            attacks[index] = Attacks.BishopAttacksOnTheFly(square, occupancies[index]);
        }
        // test magic number 
        for (int randomCount = 0; randomCount < 100000000; randomCount++)
        {
            // generate magic number candidate   
            ulong magicNumber = GenerateMagicNumberCandidate();

            // check if the magic number is valid 
            //Skip wrong magic numbers 
            if (CountBits((attackMask * magicNumber) & 0xFF00000000000000) < 6) continue;

            // init used attacks
            Array.Fill(usedAttacks, 0UL);

            // Test magic index
            int index = 0, fail = 0;

            for (; fail == 0 && index < occupancyIndices; index++)
            {
                int magicIndex = (int)((occupancies[index] * magicNumber) >> (64 - relevantBits));
                // if magic index works 
                if (usedAttacks[magicIndex] == 0ul)
                {
                    // init used attacks
                    usedAttacks[magicIndex] = attacks[index];// magicIndex
                }


                else if (usedAttacks[magicIndex] != attacks[index])
                {
                    fail = 1;
                    
                }
            }
            // if works :) 
            if(fail == 0) 
            {
                return magicNumber;
            }
        }
        Console.WriteLine("Magic number fucked ");
        return 0UL;
    }

    // A manual way to generate Rook magic numbers
    public static ulong FindRookMagicNumber(int square, int relevantBits)
    {
        // init Occupancies
        ulong[] occupancies = new ulong[4096];

        // init attack tables 
        ulong[] attacks = new ulong[4096];

        // init used attacks
        ulong[] usedAttacks = new ulong[4096];

        // init attack mask for current piece
        ulong attackMask = Masks.MaskRookAttacks(square);

        //init occupancy indices   
        int occupancyIndices = 1 << relevantBits;

        // loop over occupancy indices
        for (int index = 0; index < occupancyIndices; index++)
        {
            // init occupancy 
            occupancies[index] = SetOccupancy(index, relevantBits, attackMask);

            // init attacks
            attacks[index] = Attacks.RookAttacksOnTheFly(square, occupancies[index]);
        }
        // test magic number 
        for (int randomCount = 0; randomCount < 100000000; randomCount++)
        {
            // generate magic number candidate   
            ulong magicNumber = GenerateMagicNumberCandidate();

            // check if the magic number is valid 
            //Skip wrong magic numbers 
            if (CountBits((attackMask * magicNumber) & 0xFF00000000000000) < 6) continue;

            // init used attacks
            Array.Fill(usedAttacks, 0UL);

            // Test magic index
            int index = 0, fail = 0;

            for (; fail == 0 && index < occupancyIndices; index++)
            {
                int magicIndex = (int)((occupancies[index] * magicNumber) >> (64 - relevantBits));
                // if magic index works 
                if (usedAttacks[magicIndex] == 0ul)
                {
                    // init used attacks
                    usedAttacks[magicIndex] = attacks[index];// magicIndex
                }


                else if (usedAttacks[magicIndex] != attacks[index])
                {
                    fail = 1;

                }
            }
            // if works :) 
            if (fail == 0)
            {
                return magicNumber;
            }
        }
        Console.WriteLine("Magic number fucked ");
        return 0UL;
    }


    // Converting UCI notation to bitcoded moves 
    public static int ConvertUciMoveToBitcode(string uciMove)
    {
        if (uciMove.Length < 4) return 0; 

        // Extract source and target squares
        int sourceSquare = Globals.CoordinatesToSquare[uciMove.Substring(0, 2)];
        int targetSquare = Globals.CoordinatesToSquare[uciMove.Substring(2, 2)];

        // Determine the piece on the source square
        int piece = -1;
        for (int i = 0; i < Boards.Bitboards.Length; i++)
        {
            if (Globals.GetBit(Boards.Bitboards[i], sourceSquare))
            {
                piece = i;
                break;
            }
        }
        if (piece == -1) return 0; // No piece found at source

        // Check if the move is a promotion (5th character in UCI string)
        int promoted = 0;
        if (uciMove.Length == 5)
        {
            char promotedChar = uciMove[4];
            promoted = Enumes.charPieces.ContainsKey(promotedChar) ? Enumes.charPieces[promotedChar] : 0;
        }

        // Determine special move flags
        bool isCapture = Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], targetSquare);
        bool isDoublePush = Math.Abs(sourceSquare - targetSquare) == 16 && (piece == (int)Enumes.Pieces.P || piece == (int)Enumes.Pieces.p);
        bool isEnPassant = (piece == (int)Enumes.Pieces.P || piece == (int)Enumes.Pieces.p) && targetSquare == Boards.EnpassantSquare;
        bool isCastling = (piece == (int)Enumes.Pieces.K || piece == (int)Enumes.Pieces.k) && Math.Abs(sourceSquare - targetSquare) == 2;

        // Encode move using your existing method
        return MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, promoted, isCapture, isDoublePush, isEnPassant, isCastling);
    }


    private static int GetXORShiftedNumber32Bit()
    {
        /*
        XOR-shift is a fast and simple method to generate pseudo-random numbers.
        It uses XOR and bit shifting operations on an initial seed.

        Magic numbers help quickly calculate all possible moves for sliding pieces
        by indexing into precomputed attack tables based on the current board setup.

    */
        int initSeed = 1804289383;
        int seed = random.Next(initSeed);
        seed ^= (seed << 13);
        seed ^= (seed >> 17);
        seed ^= (seed << 17);
        return seed;
    }
    private static ulong GetRandomU64Numbers()
    {
        // define 4 random numbers 
        ulong n1, n2, n3, n4;

        // initital random numbers and slicing 16 bits from MS1B side 
        n1 = ((ulong)GetXORShiftedNumber32Bit()) & 0xffff;
        n2 = ((ulong)GetXORShiftedNumber32Bit()) & 0xffff;
        n3 = ((ulong)GetXORShiftedNumber32Bit()) & 0xffff;
        n4 = ((ulong)GetXORShiftedNumber32Bit()) & 0xffff;


        return n1 | (n2 << 16) | (n3 << 32) | (n4 << 48);
    }
    public static string MoveToString(int move)
    {
        int source = MoveGenerator.GetMoveStartSquare(move);
        int target = MoveGenerator.GetMoveTarget(move);
        // We only print something like "a2a4" etc.
        return SquareToCoordinates[source] + Globals.SquareToCoordinates[target];
    }

    public static void TestSetOccupancy()
    {
        // Example: Square positions 0, 1, 2, 3, 4, 5 (for simplicity)
        // Let's say attackMask has bits set at squares 0, 2, 4
        ulong attackMask = (1UL << 0) | (1UL << 2) | (1UL << 4); // 0b00010101

        // Test with index 0 (binary 000)
        ulong occupancy0 = Globals.SetOccupancy(0, 3, attackMask);
        Debug.Assert(occupancy0 == 0UL, $"Expected 0, got {occupancy0}");

        // Test with index 1 (binary 001)
        ulong occupancy1 = Globals.SetOccupancy(1, 3, attackMask);
        Debug.Assert(occupancy1 == (1UL << 0), $"Expected {1UL << 0}, got {occupancy1}");

        // Test with index 2 (binary 010)
        ulong occupancy2 = Globals.SetOccupancy(2, 3, attackMask);
        Debug.Assert(occupancy2 == (1UL << 2), $"Expected {1UL << 2}, got {occupancy2}");

        // Test with index 3 (binary 011)
        ulong occupancy3 = Globals.SetOccupancy(3, 3, attackMask);
        Debug.Assert(occupancy3 == ((1UL << 0) | (1UL << 2)), $"Expected {(1UL << 0) | (1UL << 2)}, got {occupancy3}");

        // Test with index 5 (binary 101)
        ulong occupancy5 = Globals.SetOccupancy(5, 3, attackMask);
        Debug.Assert(occupancy5 == ((1UL << 0) | (1UL << 4)), $"Expected {(1UL << 0) | (1UL << 4)}, got {occupancy5}");

        Console.WriteLine("SetOccupancy tests passed.");
    }





}
