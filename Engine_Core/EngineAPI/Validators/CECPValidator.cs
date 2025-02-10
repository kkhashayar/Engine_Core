using Engine_API.Enumes;

namespace Engine_API.Validators;

public static class CECPValidator
{
    // Basically I am going to use list of commands allowed in post and get 
    public static List<CECPCommands> GetCommands = new List<CECPCommands>
    {
        CECPCommands.status,
        CECPCommands.stop,
        CECPCommands.white,
        CECPCommands.black,
        CECPCommands.protover,
        CECPCommands.quit,
        CECPCommands.go,
        CECPCommands.newGame,
        CECPCommands.xboard,
      
    };

    public static List<CECPCommands> PostCommands = new List<CECPCommands>
    {
        CECPCommands.status,
        CECPCommands.move,
        CECPCommands.usermove,
        CECPCommands.setboard
    };
}
