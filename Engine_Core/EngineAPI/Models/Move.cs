namespace Engine_API.Models
{
    public class Move
    {
        public string StartSquare { get; set; }
        public string TargetSquare { get; set; }

        public Move()
        {
            // Parameterless constructor for JSON deserialization
        }

        public Move(string moveInput)
        {
            if (!IsValidMoveFormat(moveInput))
                throw new ArgumentException("Invalid move format. Must be 4 chars like 'e2e4'.");

            StartSquare = moveInput.Substring(0, 2);
            TargetSquare = moveInput.Substring(2, 2);
        }

        private bool IsValidMoveFormat(string moveInput)
        {
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
