using HalloGames.Architecture.Saves.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace HalloGames.Architecture.Saves
{
    internal class SaveRegistry
    {
        private readonly Dictionary<Type, BaseSave> _saves = new Dictionary<Type, BaseSave>();

        internal IReadOnlyDictionary<Type, BaseSave> Saves => _saves;

        internal SaveRegistry()
        {
            RegisterAll();
        }

        private void RegisterAll()
        {
            var saveTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(GetTypesSafe)
                .Where(IsSaveType);

            foreach (var saveType in saveTypes)
            {
                var save = (BaseSave)Activator.CreateInstance(saveType);
                _saves.Add(saveType, save);
            }
        }

        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x != null);
            }
        }

        private static bool IsSaveType(Type type)
        {
            return typeof(BaseSave).IsAssignableFrom(type)
                && !type.IsAbstract
                && !type.IsInterface;
        }
    }
}
