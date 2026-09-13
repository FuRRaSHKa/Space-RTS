using System;

namespace HalloGames.Architecture.Exceptions
{
    public class TypeMismatchException : Exception
    {
        private readonly Type _expectedType;
        private readonly Type _providedType;

        public override string Message => $"Save type mismatch: expected type '{_expectedType}', provided type '{_providedType}'";

        public TypeMismatchException(Type expectedType, Type providedType) 
        {
            _expectedType = expectedType;
            _providedType = providedType;
        }
    }
}