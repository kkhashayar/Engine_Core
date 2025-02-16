import chess.pgn

# Open the PGN file (Change 'your_file.pgn' to your actual file name)
pgn_file = "your_file.pgn"

with open(pgn_file, "r", encoding="utf-8") as pgn:
    game = chess.pgn.read_game(pgn)  # Read first game

    # Print game info
    print("Event:", game.headers.get("Event"))
    print("White:", game.headers.get("White"))
    print("Black:", game.headers.get("Black"))
    print("Result:", game.headers.get("Result"))

    # Print first 10 moves
    board = game.board()
    for move_number, move in enumerate(game.mainline_moves()):
        board.push(move)
        print(f"Move {move_number+1}: {board.san(move)}")

        if move_number == 9:  # Stop after 10 moves
            break
