
using static Engine_Core.Enumes;

namespace Engine_Core;

public static class IO
{
    public static string FenWriter()
    {
        string fen = "";

        for (int rank = 0; rank < 8; rank++)
        {
            int emptySquares = 0;
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;
                int piece = -1;

                for (int bbPiece = (int)Pieces.P; bbPiece <= (int)Pieces.k; bbPiece++)
                {
                    if (Globals.GetBit(Boards.Bitboards[bbPiece], square))
                    {
                        piece = bbPiece;
                        break;
                    }
                }

                if (piece == -1)
                {
                    emptySquares++;
                }
                else
                {
                    if (emptySquares > 0)
                    {
                        fen += emptySquares.ToString();
                        emptySquares = 0;
                    }
                    fen += AsciiPieces[0][piece];
                }
            }
            if (emptySquares > 0) fen += emptySquares.ToString();
            if (rank < 7) fen += "/";
        }

        fen += (Boards.Side == (int)Colors.white) ? " w " : " b ";

        // Castling rights
        string castling = "";
        if ((Boards.CastlePerm & (int)Castling.WKCA) != 0) castling += "K";
        if ((Boards.CastlePerm & (int)Castling.WQCA) != 0) castling += "Q";
        if ((Boards.CastlePerm & (int)Castling.BKCA) != 0) castling += "k";
        if ((Boards.CastlePerm & (int)Castling.BQCA) != 0) castling += "q";
        fen += castling.Length > 0 ? castling : "-";

        fen += " " + (Boards.EnpassantSquare != (int)Squares.NoSquare ? Globals.SquareToCoordinates[Boards.EnpassantSquare] : "-");

        return fen;
    }

    public static void FenReader(string fen)
    {
        if(string.IsNullOrEmpty(fen)) fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
       
        Array.Clear(Boards.Bitboards, 0, Boards.Bitboards.Length);
        Array.Clear(Boards.OccupanciesBitBoards, 0, Boards.OccupanciesBitBoards.Length);
        
        // reset game state variables
        Boards.Side = 0;
        Boards.EnpassantSquare = (int)Enumes.Squares.NoSquare;
        Boards.CastlePerm = 0;

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;
                if (char.IsLetter(fen[0]))
                {
                    int piece = Enumes.charPieces[fen[0]];

                    Globals.SetBit(ref Boards.Bitboards[piece], square);

                    fen = fen.Substring(1);
                }

                
                if (char.IsDigit(fen[0]))
                {
                    int offset = fen[0] - '0';
                    int piece = -1;

                    for (int bbPiece = (int)Enumes.Pieces.P; bbPiece <= (int)Enumes.Pieces.k; bbPiece++)
                    {
                        if (Globals.GetBit(Boards.Bitboards[bbPiece], square))
                            piece = bbPiece;
                    }
                    if (piece == -1)
                        file--;


                    file += offset;
                    fen = fen.Substring(1);
                }

                // match rank separator
                if (fen[0] == '/')
                    fen = fen.Substring(1);
            }
        }

        // Side to move
        fen = fen.Substring(1);
        if (fen[0] == 'w')
        {
            Boards.Side = (int)Enumes.Colors.white;
        }
        else
        {
            Boards.Side = (int)Enumes.Colors.black;
        }


        // Getting castle rights
        fen = fen.Substring(2);
        while (fen[0] != ' ')
        {
            switch (fen[0])
            {
                case 'K': Boards.CastlePerm |= (int)Enumes.Castling.WKCA; break;
                case 'Q': Boards.CastlePerm |= (int)Enumes.Castling.WQCA; break;
                case 'k': Boards.CastlePerm |= (int)Enumes.Castling.BKCA; break;
                case 'q': Boards.CastlePerm |= (int)Enumes.Castling.BQCA; break;
                case '-': break;
            }
            fen = fen.Substring(1);
        }

        // parse enpassant square  --> not sure if works 
        fen = fen.Substring(1);
        if (fen[0] != '-')
        {
            int file = fen[0] - 'a';
            int rank = 8 - (fen[1] - '0');
            // init enpassant square
            Boards.EnpassantSquare = rank * 8 + file;
        }
        else
        {
            Boards.EnpassantSquare = (int)Enumes.Squares.NoSquare;
        }

        for (int piece = (int)Enumes.Pieces.P; piece <= (int)Enumes.Pieces.K; piece++)
            // populate white occupancy bitboard
            Boards.OccupanciesBitBoards[(int)Enumes.Colors.white] |= Boards.Bitboards[piece];

        for (int piece = (int)Enumes.Pieces.p; piece <= (int)Enumes.Pieces.k; piece++)
            // populate black occupancy bitboard
            Boards.OccupanciesBitBoards[(int)Enumes.Colors.black] |= Boards.Bitboards[piece];

        // init all occupancies
        Boards.OccupanciesBitBoards[(int)Enumes.Colors.both] |= Boards.OccupanciesBitBoards[(int)Enumes.Colors.white];
        Boards.OccupanciesBitBoards[(int)Enumes.Colors.both] |= Boards.OccupanciesBitBoards[(int)Enumes.Colors.black];


        Search.positionHashKey = 0; 
        Search.GeneratepositionHashKey(); 
    }
}
