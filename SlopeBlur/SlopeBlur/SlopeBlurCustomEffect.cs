using System.Runtime.InteropServices;
using Vortice;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace SlopeBlur
{
    internal sealed class SlopeBlurCustomEffect(IGraphicsDevicesAndContext devices) : D2D1CustomShaderEffectBase(Create<EffectImpl>(devices))
    {
        public const float MaxAmount = 2048f;
        public const float MaxSlopeRadius = 200f;
        public const int MaxIterations = 32;

        public float Amount { get => GetFloatValue((int)EffectImpl.Properties.Amount); set => SetValue((int)EffectImpl.Properties.Amount, value); }
        /// <summary>ラジアン</summary>
        public float Direction { get => GetFloatValue((int)EffectImpl.Properties.Direction); set => SetValue((int)EffectImpl.Properties.Direction, value); }
        public float SlopeRadius { get => GetFloatValue((int)EffectImpl.Properties.SlopeRadius); set => SetValue((int)EffectImpl.Properties.SlopeRadius, value); }
        public int Iterations { get => GetIntValue((int)EffectImpl.Properties.Iterations); set => SetValue((int)EffectImpl.Properties.Iterations, value); }
        public int Reference { get => GetIntValue((int)EffectImpl.Properties.Reference); set => SetValue((int)EffectImpl.Properties.Reference, value); }

        [CustomEffect(1)]
        private sealed class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
        {
            ConstantBuffer constants;
            float directionRadians;

            [CustomEffectProperty(PropertyType.Float, (int)Properties.Amount)]
            public float Amount
            {
                get => constants.Amount;
                set
                {
                    constants.Amount = float.IsFinite(value) ? Math.Clamp(value, 0f, MaxAmount) : 0f;
                    UpdateConstants();
                }
            }

            [CustomEffectProperty(PropertyType.Float, (int)Properties.Direction)]
            public float Direction
            {
                get => directionRadians;
                set
                {
                    directionRadians = float.IsFinite(value) ? value : 0f;
                    constants.CosA = MathF.Cos(directionRadians);
                    constants.SinA = MathF.Sin(directionRadians);
                    UpdateConstants();
                }
            }

            [CustomEffectProperty(PropertyType.Float, (int)Properties.SlopeRadius)]
            public float SlopeRadius
            {
                get => constants.SlopeRadius;
                set
                {
                    constants.SlopeRadius = float.IsFinite(value) ? Math.Clamp(value, 0.5f, MaxSlopeRadius) : 0.5f;
                    UpdateConstants();
                }
            }

            [CustomEffectProperty(PropertyType.Int32, (int)Properties.Iterations)]
            public int Iterations
            {
                get => constants.Iterations;
                set
                {
                    constants.Iterations = Math.Clamp(value, 1, MaxIterations);
                    UpdateConstants();
                }
            }

            [CustomEffectProperty(PropertyType.Int32, (int)Properties.Reference)]
            public int Reference
            {
                get => constants.Source;
                set
                {
                    constants.Source = Math.Clamp(value, 0, 4);
                    UpdateConstants();
                }
            }

            public EffectImpl() : base(ShaderResourceUri.Get("SlopeBlur"))
            {
                constants.CosA = 1f;
                constants.SinA = 0f;
                constants.SlopeRadius = 4f;
                constants.Iterations = 12;
            }

            protected override void UpdateConstants()
            {
                if (drawInformation is null)
                    return;

                Span<byte> buffer = stackalloc byte[Marshal.SizeOf<ConstantBuffer>()];
                MemoryMarshal.Write(buffer, in constants);
                drawInformation.SetPixelShaderConstantBuffer(buffer);
            }

            public override void MapInputRectsToOutputRect(RawRect[] inputRects, RawRect[] inputOpaqueSubRects, out RawRect outputRect, out RawRect outputOpaqueSubRect)
            {
                inputRect = ClampInputRect(inputRects[0]);
                UpdateConstants();

                outputRect = Inflate(inputRect, GetMargin());
                outputOpaqueSubRect = default;
            }

            public override void MapOutputRectToInputRects(RawRect outputRect, RawRect[] inputRects)
            {
                if (inputRects.Length > 0)
                    inputRects[0] = ClampInputRect(Inflate(outputRect, GetMargin() + (int)Math.Ceiling(constants.SlopeRadius) + 1));
            }

            int GetMargin()
            {
                if (constants.Amount <= 0f || constants.Iterations < 1)
                    return 0;
                return (int)Math.Ceiling(constants.Amount);
            }

            static RawRect Inflate(RawRect rect, int margin)
            {
                if (margin <= 0)
                    return rect;

                return new RawRect(
                    Saturate((long)rect.Left - margin),
                    Saturate((long)rect.Top - margin),
                    Saturate((long)rect.Right + margin),
                    Saturate((long)rect.Bottom + margin));
            }

            static int Saturate(long value) => (int)Math.Clamp(value, int.MinValue, int.MaxValue);

            // HLSL の cbuffer と一致させる（32 bytes）
            [StructLayout(LayoutKind.Sequential)]
            struct ConstantBuffer
            {
                public float Amount;
                public float CosA;
                public float SinA;
                public float SlopeRadius;
                public int Iterations;
                public int Source;
                public float Pad0;
                public float Pad1;
            }

            public enum Properties
            {
                Amount,
                Direction,
                SlopeRadius,
                Iterations,
                Reference,
            }
        }
    }
}
