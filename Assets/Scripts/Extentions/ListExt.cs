using System.Collections.Generic;

namespace HalloGames.Extensions.Collections
{
    public static class ListExt
    {
        public static void Compact<T>(this List<T> list) where T : class
        {
            if (list == null)
                return;

            var write = 0;
            var count = list.Count;

            for (int read = 0; read < count; read++)
            {
                var item = list[read];
                if (item == null)
                    continue;

                if (write != read)
                    list[write] = item;

                write++;
            }

            list.RemoveRange(write, count - write);
        }
    }
}
