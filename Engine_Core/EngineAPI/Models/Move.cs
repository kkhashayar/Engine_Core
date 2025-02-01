namespace Engine_API.Models
{
    public class Move
    {
        public string StartSquare { get; private set; }
        public string TargetSquare { get; private set; }

        public Move(string moveInput)
        {

        }


        private bool IsValidMoveFormat(string moveInput)
        {
            // Exactly 4 characters, in correct board coordinats

            return moveInput.Length == 4 &&
                moveInput[0] >= 'a' && moveInput[0] <= 'h' &&
                moveInput[1] >= '1' && moveInput[1] <= '8' &&
                moveInput[2] >= 'a' && moveInput[2] <= 'h' &&
                moveInput[3] >= '1' && moveInput[3] <= '8';
        }

        public override string ToString()
        {
            return $"{StartSquare}{TargetSquare}";
        }
    }
}



