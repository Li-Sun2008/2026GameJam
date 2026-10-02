using System;
using Spotlight.Contracts;
namespace Spotlight.Core
{
    public sealed class RandomService : IRandomService
    {
        readonly SessionStateStore store;public RandomService(SessionStateStore store){this.store=store;}public RandomState GetState(RandomStream stream){uint value;if(!store.RandomStates.TryGetValue(stream,out value))throw new ArgumentOutOfRangeException("stream");return new RandomState(stream,value);}public IRandomCursor CreateCursor(RandomState state){if(state.State==0 || !Enum.IsDefined(typeof(RandomStream),state.Stream))throw new ArgumentException("Invalid random state");return new Cursor(state);}
        sealed class Cursor : IRandomCursor
        {
            uint state;readonly RandomStream stream;public Cursor(RandomState value){state=value.State;stream=value.Stream;}uint Next(){uint x=state;x^=x<<13;x^=x>>17;x^=x<<5;state=x;return x;}
            public int NextInt(int minInclusive,int maxExclusive){if(minInclusive>=maxExclusive)throw new ArgumentOutOfRangeException("maxExclusive");ulong bound=(ulong)((long)maxExclusive-minInclusive); // xorshift visits 1..uint.MaxValue; shift to a zero-based uniform domain.
                ulong domain=uint.MaxValue,limit=domain-domain%bound,value;do{value=(ulong)Next()-1;}while(value>=limit);return (int)(minInclusive+(long)(value%bound));}
            public float NextUnitFloat(){return (Next()>>8)*(1f/16777216f);}public RandomState Capture(){return new RandomState(stream,state);}
        }
    }
}
