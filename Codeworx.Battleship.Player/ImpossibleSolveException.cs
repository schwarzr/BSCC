using System;
using System.Runtime.Serialization;

namespace Codeworx.Battleship.Player
{
    [Serializable]
    internal class ImpossibleSolveException : Exception
    {
        public ImpossibleSolveException()
        {
        }

        public ImpossibleSolveException(string message) : base(message)
        {
        }

        public ImpossibleSolveException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ImpossibleSolveException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}