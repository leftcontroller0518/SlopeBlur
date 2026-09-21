using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Player.Video.Effects;

namespace SlopeBlur
{
    internal sealed class SlopeBlurEffectProcessor(IGraphicsDevicesAndContext devices, SlopeBlurEffect item) : VideoEffectProcessorBase(devices)
    {
        readonly SlopeBlurEffect item = item;
        SlopeBlurCustomEffect? effect;

        bool isFirst = true;
        float amount;
        float direction;
        float slopeRadius;
        int iterations;
        int reference;

        public override DrawDescription Update(EffectDescription effectDescription)
        {
            if (IsPassThroughEffect || effect is null)
                return effectDescription.DrawDescription;

            var frame = effectDescription.ItemPosition.Frame;
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;

            var newAmount = Sanitize(item.Amount.GetValue(frame, length, fps), 0f, SlopeBlurCustomEffect.MaxAmount);
            var newDirection = Sanitize(item.Direction.GetValue(frame, length, fps), -3600f, 3600f) * MathF.PI / 180f;
            var newRadius = Sanitize(item.SlopeRadius.GetValue(frame, length, fps), 0.5f, SlopeBlurCustomEffect.MaxSlopeRadius);
            var newIterations = Math.Clamp(item.Iterations, 1, SlopeBlurCustomEffect.MaxIterations);
            var newReference = (int)item.Reference;

            if (isFirst || amount != newAmount) effect.Amount = newAmount;
            if (isFirst || direction != newDirection) effect.Direction = newDirection;
            if (isFirst || slopeRadius != newRadius) effect.SlopeRadius = newRadius;
            if (isFirst || iterations != newIterations) effect.Iterations = newIterations;
            if (isFirst || reference != newReference) effect.Reference = newReference;

            isFirst = false;
            amount = newAmount;
            direction = newDirection;
            slopeRadius = newRadius;
            iterations = newIterations;
            reference = newReference;

            return effectDescription.DrawDescription;
        }

        static float Sanitize(double value, float minimum, float maximum)
        {
            if (!double.IsFinite(value))
                return minimum;
            return (float)Math.Clamp(value, minimum, maximum);
        }

        protected override ID2D1Image? CreateEffect(IGraphicsDevicesAndContext devices)
        {
            effect = new SlopeBlurCustomEffect(devices);
            if (!effect.IsEnabled)
            {
                effect.Dispose();
                effect = null;
                return null;
            }
            disposer.Collect(effect);

            var output = effect.Output;
            disposer.Collect(output);
            return output;
        }

        protected override void setInput(ID2D1Image? input)
        {
            effect?.SetInput(0, input, true);
        }

        protected override void ClearEffectChain()
        {
            effect?.SetInput(0, null, true);
            isFirst = true;
        }
    }
}
