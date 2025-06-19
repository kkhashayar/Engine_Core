
namespace Engine_Core;

public static class Enumes
{
    public enum Squares
    {
        a8, b8, c8, d8, e8, f8, g8, h8,
        a7, b7, c7, d7, e7, f7, g7, h7,
        a6, b6, c6, d6, e6, f6, g6, h6,
        a5, b5, c5, d5, e5, f5, g5, h5,
        a4, b4, c4, d4, e4, f4, g4, h4,
        a3, b3, c3, d3, e3, f3, g3, h3,
        a2, b2, c2, d2, e2, f2, g2, h2,
        a1, b1, c1, d1, e1, f1, g1, h1, NoSquare
    }
    public enum Pieces
    {
        P, N, B, R, Q, K, 
        p, n, b, r, q, k
    }
    public static string[] AsciiPieces = new string[] { "PNBRQKpnbrqk" };

    public static char[] UnicodePieces = new char[12] { '\u2659', '\u2658', '\u2657', '\u2656', 
                                                        '\u2655', '\u2654', '\u265F', '\u265E', 
                                                        '\u265D', '\u265C', '\u265B', '\u265A' };

    // Convert ASCII character pieces to encoded constants
    public static Dictionary<char, int> charPieces = new Dictionary<char, int>
    {
        ['P'] = (int)Pieces.P,
        ['N'] = (int)Pieces.N,
        ['B'] = (int)Pieces.B,
        ['R'] = (int)Pieces.R,
        ['Q'] = (int)Pieces.Q,
        ['K'] = (int)Pieces.K,
        ['p'] = (int)Pieces.p,
        ['n'] = (int)Pieces.n,
        ['b'] = (int)Pieces.b,
        ['r'] = (int)Pieces.r,
        ['q'] = (int)Pieces.q,
        ['k'] = (int)Pieces.k,
    };
    // Sides to move (colors)
    public enum Colors
    {
        white,
        black,
        both
    }

    public enum RookOrBishop
    {
        rook,
        bishop,
        Queen // Using both rook and bishop to generate the magic number for the queen.
    }
    public enum Castling
    {
        WKCA = 1,
        WQCA = 2,
        BKCA = 4,
        BQCA = 8
    }


    public enum MoveFlags
    {
        AllMoves,
        Captures,
    }


    public enum GamePhase
    {
        None,
        Opening,
        MiddleGame,
        EndGame,
        KPvK,
        KBvK,
        KBNvK,
        K2BvK
    }


    public enum NodeType
    {
        Exact, 
        Alpha,
        Beta
    }
    

    /*
        0001 -> 1 -> WKCA
        0010 -> 2 -> WQCA
        0100 -> 4 -> BKCA
        1000 -> 8 -> BQCA

        0000 -> 0 -> No castling

        Examples:
        
        1111 -> 15 -> WKCA | WQCA | BKCA | BQCA
        1010 -> 10 -> WKCA | BKCA   
        1001 -> 9  -> WKCA | BQCA
     */
    
    
}
