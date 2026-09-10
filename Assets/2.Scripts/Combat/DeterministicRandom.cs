using UnityEngine;

/// <summary>
/// 시드로 완전히 재현 가능한 난수입니다.
///
/// UnityEngine.Random은 전역 상태라 다른 코드가 소비하면 순서가 흐트러집니다.
/// 전투 판정은 이 인스턴스만 쓰므로, 같은 시드 + 같은 조작 = 같은 결과가 보장됩니다.
/// 미션 가이드의 "재현 기능 — 난수는 시드로 관리하며, 버그 재현과 밸런스 검증에 사용" 항목입니다.
///
/// 알고리즘은 xorshift32입니다. 게임 판정에 충분한 품질이면서
/// 구현이 짧아 플랫폼 간 결과가 반드시 동일합니다.
/// </summary>
public sealed class DeterministicRandom
{
    private uint state;

    /// <summary>이 난수열을 만든 시드입니다. 버그 재현 시 이 값을 다시 넣으면 됩니다.</summary>
    public uint Seed { get; }

    /// <summary>지금까지 소비한 난수 개수입니다. 로그에서 판정 순서를 추적할 때 씁니다.</summary>
    public int CallCount { get; private set; }

    public DeterministicRandom(uint seed)
    {
        // xorshift는 상태 0에서 영원히 0을 뱉으므로 막아 둡니다.
        Seed = seed == 0u ? 1u : seed;
        state = Seed;
    }

    /// <summary>시간 기반 시드로 새 난수열을 만듭니다. 시드 값은 반드시 로그에 남기세요.</summary>
    public static DeterministicRandom FromTime()
    {
        uint seed = (uint)System.DateTime.Now.Ticks;
        return new DeterministicRandom(seed);
    }

    public uint NextUInt()
    {
        CallCount++;
        uint x = state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        state = x;
        return x;
    }

    /// <summary>[minInclusive, maxExclusive) 범위의 정수입니다.</summary>
    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) return minInclusive;
        uint span = (uint)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt() % span);
    }

    /// <summary>1~100 사이의 값입니다. 확률 판정에 씁니다.</summary>
    public int Roll100() => Range(1, 101);

    /// <summary>percent 확률로 true입니다. 0 이하면 항상 false, 100 이상이면 항상 true입니다.</summary>
    public bool Chance(int percent)
    {
        if (percent <= 0) return false;
        if (percent >= 100) return true;
        return Roll100() <= percent;
    }

    /// <summary>리스트에서 하나를 무작위로 고릅니다.</summary>
    public T Pick<T>(System.Collections.Generic.IList<T> list)
    {
        if (list == null || list.Count == 0) return default;
        return list[Range(0, list.Count)];
    }

    /// <summary>현재 내부 상태입니다. 전투 중 저장·복구 시 이 값을 함께 저장하면 이어서 재현됩니다.</summary>
    public uint State => state;

    /// <summary>저장된 상태로 되돌립니다.</summary>
    public void Restore(uint savedState, int savedCallCount)
    {
        state = savedState == 0u ? 1u : savedState;
        CallCount = savedCallCount;
    }

    public override string ToString()
        => $"seed={Seed} calls={CallCount}";
}
