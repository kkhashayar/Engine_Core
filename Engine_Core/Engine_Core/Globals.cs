using System.Diagnostics;
using System.Numerics;
using static Engine_Core.Enumes;
namespace Engine_Core;

public static class Globals
{



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

    public static readonly int[] CoordinateToSquare =
    {
        0,  1,  2,  3,  4,  5,  6,  7,
        8,  9, 10, 11, 12, 13, 14, 15,
        16, 17, 18, 19, 20, 21, 22, 23,
        24, 25, 26, 27, 28, 29, 30, 31,
        32, 33, 34, 35, 36, 37, 38, 39,
        40, 41, 42, 43, 44, 45, 46, 47,
        48, 49, 50, 51, 52, 53, 54, 55,
        56, 57, 58, 59, 60, 61, 62, 63
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
            if (fail == 0)
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


    // Convert UCI notation to bitcoded moves 
    public static int ConvertUciMoveToBitcode(string uciMove)
    {
        if (string.IsNullOrEmpty(uciMove) || uciMove.Length < 2) return 0; // Invalid UCI move

        // Remove non-move characters like "x", "+", "#", and "="
        string sanitizedMove = uciMove.Replace("+", "").Replace("#", "").Replace("x", "").Replace("=", "");

        // Extract target square (always last two characters)
        var targetSquareStr = sanitizedMove.Substring(sanitizedMove.Length - 2, 2);
        var targetSquareTest = targetSquareStr;
        //if (!Globals.CoordinatesToSquare.ContainsKey(targetSquareStr)) return 0; // Invalid target square
        int targetSquare = Globals.CoordinatesToSquare[targetSquareStr];

        int sourceSquare = -1;
        int piece = -1;
        int promoted = 0;

        // **Case 1: Standard UCI format (e2e4, g1f3)**
        if (sanitizedMove.Length >= 4 && char.IsLetter(sanitizedMove[0]) && char.IsDigit(sanitizedMove[1]))
        {
            sourceSquare = Globals.CoordinatesToSquare[sanitizedMove.Substring(0, 2)];
            piece = GetPieceFromBitboards(sourceSquare);
        }
        // **Case 2: Algebraic Notation (Nc3, Bf2, etc.)**
        else
        {
            char pieceChar = sanitizedMove[0];
            if (!Enumes.charPieces.ContainsKey(pieceChar)) return 0; // Invalid piece

            piece = Enumes.charPieces[pieceChar];

            // Check if there is a disambiguation rank/file (like `Nbd2`, `R1e1`)
            string disambiguation = "";
            if (sanitizedMove.Length == 4) disambiguation = sanitizedMove[1].ToString();
            if (sanitizedMove.Length == 5) disambiguation = sanitizedMove.Substring(1, 2);

            // Find the correct source square
            sourceSquare = FindPieceSourceSquare(piece, targetSquare, disambiguation);
        }

        if (sourceSquare == -1 || piece == -1) return 0; // Invalid move

        // Handle promotions
        if (sanitizedMove.Length == 5 && char.IsLetter(sanitizedMove[4]))
        {
            char promotedChar = sanitizedMove[4];
            promoted = Enumes.charPieces.ContainsKey(promotedChar) ? Enumes.charPieces[promotedChar] : 0;
        }

        // Determine special move flags
        bool isCapture = Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], targetSquare);
        bool isDoublePush = Math.Abs(sourceSquare - targetSquare) == 16 && (piece == (int)Enumes.Pieces.P || piece == (int)Enumes.Pieces.p);
        bool isEnPassant = (piece == (int)Enumes.Pieces.P || piece == (int)Enumes.Pieces.p) && targetSquare == Boards.EnpassantSquare;
        bool isCastling = (piece == (int)Enumes.Pieces.K || piece == (int)Enumes.Pieces.k) && Math.Abs(sourceSquare - targetSquare) == 2;

        // Encode move using existing method
        return MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, promoted, isCapture, isDoublePush, isEnPassant, isCastling);
    }

    // **Find the source square for a given piece that can move to the target**
    private static int FindPieceSourceSquare(int piece, int targetSquare, string disambiguation)
    {
        List<int> possibleSources = new List<int>();

        for (int i = 0; i < 64; i++) // Loop over all board squares
        {
            if (Globals.GetBit(Boards.Bitboards[piece], i))
            {
                // Check if this piece at 'i' can move to targetSquare

                possibleSources.Add(i);

            }
        }

        // **If only one possible source, return it**
        if (possibleSources.Count == 1) return possibleSources[0];

        // **If multiple sources, disambiguate**
        foreach (int square in possibleSources)
        {
            string coord = Globals.SquareToCoordinates[square];

            // If disambiguation is a rank (1-8)
            if (disambiguation.Length == 1 && char.IsDigit(disambiguation[0]) && coord[1] == disambiguation[0])
                return square;

            // If disambiguation is a file (a-h)
            if (disambiguation.Length == 1 && char.IsLetter(disambiguation[0]) && coord[0] == disambiguation[0])
                return square;

            // If full square name (like "Nbd2")
            if (disambiguation.Length == 2 && coord == disambiguation)
                return square;
        }

        return -1; // No valid source found
    }

    // **Find which piece is at a given square**
    private static int GetPieceFromBitboards(int square)
    {
        for (int i = 0; i < Boards.Bitboards.Length; i++)
        {
            if (Globals.GetBit(Boards.Bitboards[i], square))
            {
                return i; // Found piece type
            }
        }
        return -1; // No piece found
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
