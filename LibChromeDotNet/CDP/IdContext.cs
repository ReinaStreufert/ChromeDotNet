using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP
{
    // int-32s are actually not that large. there are 4294967296 random values possible so doing it incrementally, i could have 4294967296 pending messages
    // at once if chrome stopped replying to messages. at around ~86 million packets sent the probability that in 50 or so pending packets out at once, two will
    // eventually collide reaches 1.0 which adds up fairly quickly when you hook mousemove
    public class IdContext
    {
        private int _Id = int.MinValue;

        public int Next()
        {
            for (; ;)
            {
                var current = _Id;
                if (current == int.MaxValue)
                {
                    if (Interlocked.CompareExchange(ref _Id, current, int.MinValue) == current)
                        return current;
                    continue;
                }
                return Interlocked.Increment(ref _Id);
            }
        }
    }
}
