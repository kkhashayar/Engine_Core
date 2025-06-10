using Engine_Core;
using static Engine_Core.Enumes;

namespace Engine;

public static class Evaluators
{

    private static readonly int[] materialScore = new int[]
    {
        100,    // P
        300,    // N
        350,    // B
        500,    // R
        1000,   // Q
        10000,  // K
        -100,   // p
        -300,   // n
        -350,   // b
        -500,   // r
        -1000,  // q
        -10000  // k
    };

    private static readonly int[] pawnScore = new int[]
    {
        90,  90,  90,  90,  90,  90,  90,  90,
        30,  30,  30,  40,  40,  30,  30,  30,
        20,  20,  20,  30,  30,  30,  20,  20,
        10,  10,  10,  20,  20,  10,  10,  10,
         5,   5,  10,  20,  20,   5,   5,   5,
         0,   0,   0,   5,   5,   0,   0,   0,
         0,   0,   0, -10, -10,   0,   0,   0,
         0,   0,   0,   0,   0,   0,   0,   0
    };

    private static readonly int[] knightScore = new int[]
    {
        -5,   0,   0,   0,   0,   0,   0,  -5,
        -5,   0,   0,  10,  10,   0,   0,  -5,
        -5,   5,  20,  20,  20,  20,   5,  -5,
        -5,  10,  20,  30,  30,  20,  10,  -5,
        -5,  10,  20,  30,  30,  20,  10,  -5,
        -5,   5,  20,  10,  10,  20,   5,  -5,
        -5,   0,   0,   0,   0,   0,   0,  -5,
        -5, -10,   0,   0,   0,   0, -10,  -5
    };

    private static readonly int[] bishopScore = new int[]
    {
         0,   0,   0,   0,   0,   0,   0,   0,
         0,   0,   0,   0,   0,   0,   0,   0,
         0,   0,   0,  10,  10,   0,   0,   0,
         0,   0,  10,  20,  20,  10,   0,   0,
         0,   0,  10,  20,  20,  10,   0,   0,
         0,  10,   0,   0,   0,   0,  10,   0,
         0,  30,   0,   0,   0,   0,  30,   0,
         0,   0, -10,   0,   0, -10,   0,   0
    };

    private static readonly int[] rookScore = new int[]
    {
        50,  50,  50,  50,  50,  50,  50,  50,
        50,  50,  50,  50,  50,  50,  50,  50,
         0,   0,  10,  20,  20,  10,   0,   0,
         0,   0,  10,  20,  20,  10,   0,   0,
         0,   0,  10,  20,  20,  10,   0,   0,
         0,   0,  10,  20,  20,  10,   0,   0,
         0,   0,  10,  20,  20,  10,   0,   0,
         0,   0,   0,  20,  20,   0,   0,   0
    };

    private static readonly int[] kingScore = new int[]
    {
         0,   0,   0,   0,   0,   0,   0,   0,
         0,   0,   5,   5,   5,   5,   0,   0,
         0,   5,   5,  10,  10,   5,   5,   0,
         0,   5,  10,  20,  20,  10,   5,   0,
         0,   5,  10,  20,  20,  10,   5,   0,
         0,   0,   5,  10,  10,   5,   0,   0,
         0,   5,   5,  -5,  -5,   0,   5,   0,
         0,   0,   5,   0, -15,   0,  10,   0
    };

    public static int GetByMaterialAndPosition(ulong[] bitboards)
    {

        int score = 0;

        // ===== Existing material + position evaluation =====
        for (int bbPiece = (int)Pieces.P; bbPiece <= (int)Pieces.k; bbPiece++)
        {
            ulong bitboard = bitboards[bbPiece];
           
            while (bitboard != 0)
            {
                int square = Globals.GetLs1bIndex(bitboard);
                Globals.PopBit(ref bitboard, square);

                score += materialScore[bbPiece];
                switch (bbPiece)
                {
                    case (int)Pieces.P: score += pawnScore[square]; break;
                    case (int)Pieces.N: score += knightScore[square]; break;
                    case (int)Pieces.B: score += bishopScore[square]; break;
                    case (int)Pieces.R: score += rookScore[square]; break;
                    case (int)Pieces.K: score += kingScore[square]; break;

                    case (int)Pieces.p: score -= pawnScore[63 - square]; break;
                    case (int)Pieces.n: score -= knightScore[63 - square]; break;
                    case (int)Pieces.b: score -= bishopScore[63 - square]; break;
                    case (int)Pieces.r: score -= rookScore[63 - square]; break;
                    case (int)Pieces.k: score -= kingScore[63 - square]; break;
                }
            }
        }

        // ===== Mobility bonus/penalty for restricting opponent’s moves =====
        int currentSide = Boards.Side;
        int opponentSide;

        if (currentSide == (int)Colors.white)
        {
            opponentSide = (int)Colors.black;
        }
        else
        {
            opponentSide = (int)Colors.white;
        }

        Boards.Side = opponentSide;

        MoveObjects opponentMoveList = new MoveObjects();
        MoveGenerator.GenerateMoves(opponentMoveList);

        int opponentMovesCount = 0;
        for (int i = 0; i < opponentMoveList.counter; i++)
        {
            if (MoveGenerator.IsLegal(opponentMoveList.moves[i], false))
            {
                opponentMovesCount++;
            }
        }

        // Fewer opponent moves = better for current side
        int mobilityFactor = 8;
        score -= opponentMovesCount * mobilityFactor;

        // Restore side
        Boards.Side = currentSide;

        // Final perspective
        if (currentSide == (int)Colors.white)
        {
            return score;
        }
        else
        {
            return -score;
        }
    }

}




/*
* // most valuable victim & less valuable attacker

/*
                      
(Victims) Pawn Knight Bishop   Rook  Queen   King
(Attackers)
    Pawn   105    205    305    405    505    605
  Knight   104    204    304    404    504    604
  Bishop   103    203    303    403    503    603
    Rook   102    202    302    402    502    602
   Queen   101    201    301    401    501    601
    King   100    200    300    400    500    600

*/

// MVV LVA [attacker][victim]
//static int mvv_lva[12][12] = {
//    105, 205, 305, 405, 505, 605,  105, 205, 305, 405, 505, 605,
//	104, 204, 304, 404, 504, 604,  104, 204, 304, 404, 504, 604,
//	103, 203, 303, 403, 503, 603,  103, 203, 303, 403, 503, 603,
//	102, 202, 302, 402, 502, 602,  102, 202, 302, 402, 502, 602,
//	101, 201, 301, 401, 501, 601,  101, 201, 301, 401, 501, 601,
//	100, 200, 300, 400, 500, 600,  100, 200, 300, 400, 500, 600,

//	105, 205, 305, 405, 505, 605,  105, 205, 305, 405, 505, 605,
//	104, 204, 304, 404, 504, 604,  104, 204, 304, 404, 504, 604,
//	103, 203, 303, 403, 503, 603,  103, 203, 303, 403, 503, 603,
//	102, 202, 302, 402, 502, 602,  102, 202, 302, 402, 502, 602,
//	101, 201, 301, 401, 501, 601,  101, 201, 301, 401, 501, 601,
//	100, 200, 300, 400, 500, 600,  100, 200, 300, 400, 500, 600
//};


//