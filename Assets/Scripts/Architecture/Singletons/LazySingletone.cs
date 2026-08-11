namespace HalloGames.Architecture.Singletones
{
    public class LazySingletone<TInstance> where TInstance : class, new()
    {
        private static TInstance _instance;

        public static TInstance Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TInstance();

                return _instance;
            }
        }
    }
}
