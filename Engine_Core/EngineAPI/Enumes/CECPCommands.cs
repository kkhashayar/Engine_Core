namespace Engine_API.Enumes;

public enum CECPCommands
{
    None =       0,  // No command
    xboard =     1,  // Initialize CECP mode
    protover =   2,  // Protocol version request
    accepted =   3,  // Command accepted
    rejected =   4,  // Command rejected
    newGame =    5,  // Start a new game
    variant =    6,  // Set game variant
    quit =       7,  // Terminate engine
    random =     8,  // Enable/disable random moves
    force =      9,  // Enter force mode (manual moves)
    go =         10, // Start engine play
    playother =  11, // Switch sides and continue
    white =      12, // Set White to move
    black =      13, // Set Black to move
    level =      14, // Set game time control
    st =         15, // Set search time per move
    sd =         16, // Set search depth
    nps =        17, // Set nodes per second limit
    time =       18, // Remaining time for player
    otim =       19, // Remaining time for opponent
    move =       20, // Make a move
    usermove =   21, // Player makes a move
    ping =       22, // Check engine response
    draw =       23, // Offer/accept a draw
    result =     24, // Report game result
    setboard =   25, // Set up a custom position
    edit =       26, // Edit board manually
    hint =       27, // Request best move hint
    bk =         28, // Book move suggestion
    undo =       29, // Undo last move
    remove =     30, // Undo last two moves
    hard =       31, // Enable ponder (thinking on opponent’s time)
    easy =       32, // Disable ponder
    post =       33, // Show thinking output
    nopost =     34, // Hide thinking output
    analyze =    35, // Enter analysis mode
    name =       36, // Set player name
    rating =     37, // Set player rating
    computer =   38, // Opponent is a computer
    pause =      39, // Pause the game
    resume =     40, // Resume the game
    memory =     41, // Set memory usage
    cores =      42, // Set number of CPU cores
    egtpath =    43, // Set tablebase path
    option =     44  // Engine-specific options
}
