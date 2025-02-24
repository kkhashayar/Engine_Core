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

    public static readonly Dictionary<string, int> CoordinateToSquare = new Dictionary<string, int>
{
    { "a8", 0 },  { "b8", 1 },  { "c8", 2 },  { "d8", 3 },  { "e8", 4 },  { "f8", 5 },  { "g8", 6 },  { "h8", 7 },
    { "a7", 8 },  { "b7", 9 },  { "c7", 10 }, { "d7", 11 }, { "e7", 12 }, { "f7", 13 }, { "g7", 14 }, { "h7", 15 },
    { "a6", 16 }, { "b6", 17 }, { "c6", 18 }, { "d6", 19 }, { "e6", 20 }, { "f6", 21 }, { "g6", 22 }, { "h6", 23 },
    { "a5", 24 }, { "b5", 25 }, { "c5", 26 }, { "d5", 27 }, { "e5", 28 }, { "f5", 29 }, { "g5", 30 }, { "h5", 31 },
    { "a4", 32 }, { "b4", 33 }, { "c4", 34 }, { "d4", 35 }, { "e4", 36 }, { "f4", 37 }, { "g4", 38 }, { "h4", 39 },
    { "a3", 40 }, { "b3", 41 }, { "c3", 42 }, { "d3", 43 }, { "e3", 44 }, { "f3", 45 }, { "g3", 46 }, { "h3", 47 },
    { "a2", 48 }, { "b2", 49 }, { "c2", 50 }, { "d2", 51 }, { "e2", 52 }, { "f2", 53 }, { "g2", 54 }, { "h2", 55 },
    { "a1", 56 }, { "b1", 57 }, { "c1", 58 }, { "d1", 59 }, { "e1", 60 }, { "f1", 61 }, { "g1", 62 }, { "h1", 63 }
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
    private static int FindPieceSourceSquare(int piece, int targetSquare, string disambiguation)
    {
        List<int> possibleSources = new List<int>();

        // Loop through all squares on the board
        for (int square = 0; square < 64; square++)
        {
            if (Globals.GetBit(Boards.Bitboards[piece], square))
            {
                // If piece can legally move to the target, store it as a candidate

                possibleSources.Add(square);

            }
        }

        // No valid source square found
        if (possibleSources.Count == 0) return -1;

        // If only one valid source, return it
        if (possibleSources.Count == 1) return possibleSources[0];

        // **Disambiguation Handling (e.g., R8d7, R1d7)**
        foreach (int candidate in possibleSources)
        {
            string candidateCoord = Globals.SquareToCoordinates[candidate];

            if (disambiguation.Length == 1)
            {
                // File disambiguation (e.g., "Rae1")
                if (char.IsLetter(disambiguation[0]) && candidateCoord[0] == disambiguation[0])
                    return candidate;

                // Rank disambiguation (e.g., "R8d7")
                if (char.IsDigit(disambiguation[0]) && candidateCoord[1] == disambiguation[0])
                    return candidate;
            }
            else if (disambiguation.Length == 2)
            {
                // Full coordinate disambiguation (e.g., "Qh4h5")
                if (candidateCoord == disambiguation) return candidate;
            }
        }

        // No match found
        return -1;
    }


    public static int ConvertUciMoveToBitcode(string uciMove)
    {
        if (string.IsNullOrEmpty(uciMove) || uciMove.Length < 2) return 0; // Invalid UCI move

        // ============================
        // 1️ Handle Castling Moves (O-O, O-O-O)
        // ============================
        if (uciMove == "O-O" || uciMove == "O-O-O")
        {
            return HandleCastlingMove(uciMove);
        }

        // ============================
        // 2️ Preprocess Move Notation
        // ============================
        string sanitizedMove = uciMove.Replace("+", "").Replace("#", "").Replace("x", "").Replace("=", "");

        // Extract the last two characters as the target square
        string targetSquareStr = sanitizedMove.Substring(sanitizedMove.Length - 2, 2);
        if (!Globals.CoordinateToSquare.ContainsKey(targetSquareStr)) return 0; // Invalid target square
        int targetSquare = Globals.CoordinateToSquare[targetSquareStr];

        int sourceSquare = -1;
        int piece = -1;
        int promoted = 0;

        // ============================
        // 3️ Handle Standard 4-Digit UCI Moves (e2e4, g1f3)
        // ============================

        // Check if it is standard format or not 
        bool IsUciFormat = Globals.CoordinateToSquare.ContainsKey(sanitizedMove.Substring(0, 2));

        if (sanitizedMove.Length == 4  && IsUciFormat)
        {
            sourceSquare = Globals.CoordinateToSquare[sanitizedMove.Substring(0, 2)];
            piece = GetPieceFromBitboards(sourceSquare);
        }
        else if(!IsUciFormat)
        {
            // **It's not a standard move, so assume the first character is a piece**
            char pieceChar = sanitizedMove[0];

            if (!Enumes.charPieces.ContainsKey(pieceChar)) return 0; // Invalid piece notation

            piece = Enumes.charPieces[pieceChar];

            // **Extract Disambiguation (if present)**
            string disambiguation = "";

            if (sanitizedMove.Length == 4) disambiguation = sanitizedMove[1].ToString(); // R8d7
            if (sanitizedMove.Length == 5) disambiguation = sanitizedMove.Substring(1, 2); // Rae1

            // **Find Correct Source Square for Ambiguous Moves**
            sourceSquare = FindPieceSourceSquare(piece, targetSquare, disambiguation);
        }
        // ============================
        // 4️ Handle 3-Digit Algebraic Notation (Nc3, Bf2, etc.)
        // ============================
        else if (sanitizedMove.Length == 3)
        {
            char pieceChar = sanitizedMove[0];
            if (!Enumes.charPieces.ContainsKey(pieceChar)) return 0; // Invalid piece notation

            piece = Enumes.charPieces[pieceChar];
            sourceSquare = FindPieceSourceSquare(piece, targetSquare, ""); // No disambiguation
        }
        // ============================
        // 5️ Handle 4-5 Digit Disambiguation Moves (R8d7, Rae1, etc.)
        // ============================
        else if (sanitizedMove.Length >= 4)
        {
            char pieceChar = sanitizedMove[0];
            if (!Enumes.charPieces.ContainsKey(pieceChar)) return 0; // Invalid piece notation

            piece = Enumes.charPieces[pieceChar];

            // Extract disambiguation if present
            string disambiguation = "";
            if (sanitizedMove.Length == 4) disambiguation = sanitizedMove[1].ToString();
            if (sanitizedMove.Length == 5) disambiguation = sanitizedMove.Substring(1, 2);

            sourceSquare = FindPieceSourceSquare(piece, targetSquare, disambiguation);
        }

        // ============================
        // 6️ Validate and Handle Promotions
        // ============================
        if (sourceSquare == -1 || piece == -1) return 0; // Invalid move

        if (sanitizedMove.Length == 5 && char.IsLetter(sanitizedMove[4]))
        {
            char promotedChar = sanitizedMove[4];
            promoted = Enumes.charPieces.ContainsKey(promotedChar) ? Enumes.charPieces[promotedChar] : 0;
        }

        // ============================
        // 7️ Determine Move Flags
        // ============================
        bool isCapture = Globals.GetBit(Boards.OccupanciesBitBoards[(int)Enumes.Colors.both], targetSquare);
        bool isDoublePush = Math.Abs(sourceSquare - targetSquare) == 16 && (piece == (int)Enumes.Pieces.P || piece == (int)Enumes.Pieces.p);
        bool isEnPassant = (piece == (int)Enumes.Pieces.P || piece == (int)Enumes.Pieces.p) && targetSquare == Boards.EnpassantSquare;
        bool isCastling = (piece == (int)Enumes.Pieces.K || piece == (int)Enumes.Pieces.k) && Math.Abs(sourceSquare - targetSquare) == 2;

        // ============================
        // 8️ Encode and Return Move
        // ============================
        return MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, promoted, isCapture, isDoublePush, isEnPassant, isCastling);
    }


    private static int HandleCastlingMove(string uciMove)
    {
        int sourceSquare, targetSquare, piece;

        if (Boards.Side == (int)Enumes.Colors.white)
        {
            sourceSquare = Globals.CoordinateToSquare["e1"];
            piece = (int)Enumes.Pieces.K;

            targetSquare = uciMove == "O-O" ? Globals.CoordinateToSquare["g1"] : Globals.CoordinateToSquare["c1"];
        }
        else
        {
            sourceSquare = Globals.CoordinateToSquare["e8"];
            piece = (int)Enumes.Pieces.k;

            targetSquare = uciMove == "O-O" ? Globals.CoordinateToSquare["g8"] : Globals.CoordinateToSquare["c8"];
        }

        bool isCastling = true;

        return MoveGenerator.EncodeMove(sourceSquare, targetSquare, piece, 0, false, false, false, isCastling);
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
