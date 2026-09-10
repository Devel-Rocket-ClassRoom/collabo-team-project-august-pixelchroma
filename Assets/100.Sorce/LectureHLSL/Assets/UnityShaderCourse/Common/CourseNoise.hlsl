#ifndef COURSE_NOISE_INCLUDED
#define COURSE_NOISE_INCLUDED

// -----------------------------------------------------------------------------
// 수업용 공용 유틸 함수 모음
// 텍스처 없이 절차적 패턴을 만들 때 사용합니다.
// 포함 방법: #include "Assets/UnityShaderCourse/Common/CourseNoise.hlsl"
//   (경로는 프로젝트에 넣은 실제 위치에 맞춰 수정)
// -----------------------------------------------------------------------------

// 해시 (의사난수). GPU에는 rand()가 없으므로 결정적 해시 함수를 씁니다.
float Hash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

float Hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 Hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

// Value noise (격자 꼭짓점 값 보간)
float ValueNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    // smoothstep 보간 -> 격자 경계가 부드러워짐
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = Hash21(i + float2(0, 0));
    float b = Hash21(i + float2(1, 0));
    float c = Hash21(i + float2(0, 1));
    float d = Hash21(i + float2(1, 1));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Gradient(Perlin 유사) noise
float GradientNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = f * f * (3.0 - 2.0 * f);

    float2 g00 = Hash22(i + float2(0, 0)) * 2.0 - 1.0;
    float2 g10 = Hash22(i + float2(1, 0)) * 2.0 - 1.0;
    float2 g01 = Hash22(i + float2(0, 1)) * 2.0 - 1.0;
    float2 g11 = Hash22(i + float2(1, 1)) * 2.0 - 1.0;

    float v00 = dot(g00, f - float2(0, 0));
    float v10 = dot(g10, f - float2(1, 0));
    float v01 = dot(g01, f - float2(0, 1));
    float v11 = dot(g11, f - float2(1, 1));

    return lerp(lerp(v00, v10, u.x), lerp(v01, v11, u.x), u.y) * 0.5 + 0.5;
}

// FBM: 옥타브를 겹쳐 자연스러운 디테일 생성
float FBM(float2 uv, int octaves, float lacunarity, float gain)
{
    float sum = 0.0;
    float amp = 0.5;
    float freq = 1.0;
    [unroll(8)]
    for (int i = 0; i < octaves; i++)
    {
        sum += GradientNoise(uv * freq) * amp;
        freq *= lacunarity;
        amp *= gain;
    }
    return sum;
}

// 값 범위 재매핑
float Remap(float v, float inMin, float inMax, float outMin, float outMax)
{
    return outMin + (v - inMin) * (outMax - outMin) / max(1e-5, inMax - inMin);
}

// 프레넬 근사 (Schlick)
float FresnelSchlick(float3 normalWS, float3 viewDirWS, float power)
{
    return pow(saturate(1.0 - saturate(dot(normalize(normalWS), normalize(viewDirWS)))), power);
}

#endif // COURSE_NOISE_INCLUDED
