
namespace Engine_Core;

public static class Masks
{
    public static ulong MaskPawnAttacks(int side, int square)
    {
        ulong attacks = 0UL;
        ulong bitboard = 0UL;

        Globals.SetBit(ref bitboard, square);

        if (side == (int)Enumes.Colors.white)
        {
            if (((bitboard >> 7) & Boards.NotAFile) != 0)
                attacks |= (bitboard >> 7);
            if (((bitboard >> 9) & Boards.NotHFile) != 0)
                attacks |= (bitboard >> 9);
        }
        else // black
        {
            if (((bitboard << 7) & Boards.NotHFile) != 0)
                attacks |= (bitboard << 7);
            if (((bitboard << 9) & Boards.NotAFile) != 0)
                attacks |= (bitboard << 9);
        }

        return attacks;
    }

    public static ulong MaskKnightAttacks(int square)
    {
        ulong attacks = 0UL;
        ulong bitboard = 0UL;

        Globals.SetBit(ref bitboard, square);

        if (((bitboard >> 17) & Boards.NotHFile) != 0)
            attacks |= (bitboard >> 17);
        if (((bitboard >> 15) & Boards.NotAFile) != 0)
            attacks |= (bitboard >> 15);
        if (((bitboard >> 10) & Boards.NotHgFile) != 0)
            attacks |= (bitboard >> 10);
        if (((bitboard >> 6) & Boards.NotAbFile) != 0)
            attacks |= (bitboard >> 6);
        if (((bitboard << 17) & Boards.NotAFile) != 0)
            attacks |= (bitboard << 17);
        if (((bitboard << 15) & Boards.NotHFile) != 0)
            attacks |= (bitboard << 15);
        if (((bitboard << 10) & Boards.NotAbFile) != 0)
            attacks |= (bitboard << 10);
        if (((bitboard << 6) & Boards.NotHgFile) != 0)
            attacks |= (bitboard << 6);

        return attacks;
    }

    // Generate king attacks
    public static ulong MaskKingAttacks(int square)
    {
        ulong attacks = 0UL;
        ulong bitboard = 0UL;

        Globals.SetBit(ref bitboard, square);

        if ((bitboard >> 8) != 0)
            attacks |= (bitboard >> 8);
        if (((bitboard >> 9) & Boards.NotHFile) != 0)
            attacks |= (bitboard >> 9);
        if (((bitboard >> 7) & Boards.NotAFile) != 0) 
            attacks |= (bitboard >> 7);
        if (((bitboard >> 1) & Boards.NotHFile) != 0)
            attacks |= (bitboard >> 1);
        if ((bitboard << 8) != 0)
            attacks |= (bitboard << 8);
        if (((bitboard << 9) & Boards.NotAFile) != 0)
            attacks |= (bitboard << 9);
        if (((bitboard << 7) & Boards.NotHFile) != 0)
            attacks |= (bitboard << 7);
        if (((bitboard << 1) & Boards.NotAFile) != 0)
            attacks |= (bitboard << 1);

        return attacks;
    }

    // Mask bishop attacks
    public static ulong MaskBishopAttacks(int square)
    {
        ulong attacks = 0UL;

        int tr = square / 8;
        int tf = square % 8;

        // Diagonally up-right
        for (int r = tr + 1, f = tf + 1; r <= 6 && f <= 6; r++, f++)
            attacks |= (1UL << (r * 8 + f));

        // Diagonally up-left
        for (int r = tr - 1, f = tf + 1; r >= 1 && f <= 6; r--, f++)
            attacks |= (1UL << (r * 8 + f));

        // Diagonally down-right
        for (int r = tr + 1, f = tf - 1; r <= 6 && f >= 1; r++, f--)
            attacks |= (1UL << (r * 8 + f));

        // Diagonally down-left
        for (int r = tr - 1, f = tf - 1; r >= 1 && f >= 1; r--, f--)
            attacks |= (1UL << (r * 8 + f));

        return attacks;
    }

    // Mask rook attacks
    public static ulong MaskRookAttacks(int square)
    {
        ulong attacks = 0UL;

        int tr = square / 8;
        int tf = square % 8;

        // Vertically up
        for (int r = tr + 1; r <= 6; r++)
            attacks |= (1UL << (r * 8 + tf));

        // Vertically down
        for (int r = tr - 1; r >= 1; r--)
            attacks |= (1UL << (r * 8 + tf));

        // Horizontally right
        for (int f = tf + 1; f <= 6; f++)
            attacks |= (1UL << (tr * 8 + f));

        // Horizontally left
        for (int f = tf - 1; f >= 1; f--)
            attacks |= (1UL << (tr * 8 + f));

        return attacks;
    }
}
