// Slope Blur - Direct2D custom pixel shader (ps_5_0)
// 画像の勾配（スロープ）に沿ってサンプルを辿りながら平均化する。

Texture2D<float4> InputTexture : register(t0);
SamplerState InputSampler : register(s0);

cbuffer Constants : register(b0)
{
    float Amount;       // 片側の最大移動量 [px]
    float CosA;         // 方向（回転角）の cos
    float SinA;         // 方向（回転角）の sin
    float SlopeRadius;  // 勾配を測る距離 [px]
    int   Iterations;   // 片側のステップ数
    int   Source;       // 0:輝度 1:アルファ 2:赤 3:緑 4:青
    float2 Padding;
};

float Height(float2 uv)
{
    float4 c = InputTexture.SampleLevel(InputSampler, uv, 0); // premultiplied
    if (Source == 1) return c.a;
    if (Source == 2) return c.r;
    if (Source == 3) return c.g;
    if (Source == 4) return c.b;
    return dot(c.rgb, float3(0.2126, 0.7152, 0.0722));
}

float4 main(
    float4 pos      : SV_POSITION,
    float4 scenePos : SCENE_POSITION,
    float4 uv0      : TEXCOORD0) : SV_Target
{
    float2 uv = uv0.xy;

    uint w, h;
    InputTexture.GetDimensions(w, h);
    float2 texel = 1.0 / float2(max(w, 1u), max(h, 1u));

    float4 center = InputTexture.SampleLevel(InputSampler, uv, 0);
    if (Amount <= 0.0 || Iterations < 1)
        return center;

    float stepLen = Amount / Iterations;
    float2 d = max(SlopeRadius, 0.1) * texel;

    float4 sum = center;
    float count = 1.0;

    // 勾配方向と逆方向の両側へ辿る
    [loop]
    for (int side = 0; side < 2; side++)
    {
        float sgn = (side == 0) ? 1.0 : -1.0;
        float2 offsetPx = float2(0.0, 0.0);

        [loop]
        for (int i = 0; i < Iterations; i++)
        {
            float2 p = uv + offsetPx * texel;

            float2 g;
            g.x = Height(p + float2(d.x, 0.0)) - Height(p - float2(d.x, 0.0));
            g.y = Height(p + float2(0.0, d.y)) - Height(p - float2(0.0, d.y));

            // 方向: 勾配ベクトルを回転（0°=勾配に沿う, 90°=等高線に沿う）
            g = float2(g.x * CosA - g.y * SinA, g.x * SinA + g.y * CosA);

            float len = length(g);
            if (len > 1.0) g /= len;

            offsetPx += sgn * g * stepLen;

            sum += InputTexture.SampleLevel(InputSampler, uv + offsetPx * texel, 0);
            count += 1.0;
        }
    }

    return sum / count;
}
