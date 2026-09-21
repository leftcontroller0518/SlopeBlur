using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;


namespace SlopeBlur
{
    public enum SlopeBlurSource
    {
        [Display(Name = "輝度")]
        Luminance = 0,
        [Display(Name = "アルファ")]
        Alpha = 1,
        [Display(Name = "赤")]
        Red = 2,
        [Display(Name = "緑")]
        Green = 3,
        [Display(Name = "青")]
        Blue = 4,
    }

    [VideoEffect("Slope Blur", ["ぼかし"], ["slope", "blur", "スロープ", "ブラー"], isAviUtlSupported: false)]
    internal class SlopeBlurEffect : VideoEffectBase
    {
        public override string Label => "Slope Blur";

        [Display(GroupName = "Slope Blur", Name = "強さ", Description = "勾配に沿って辿る最大距離（片側）")]
        [AnimationSlider("F1", "px", 0, 300)]
        public Animation Amount { get; } = new Animation(30, 0, 2048);

        [Display(GroupName = "Slope Blur", Name = "方向", Description = "0°で勾配に沿い、90°で等高線に沿って流れます")]
        [AnimationSlider("F1", "°", -180, 180)]
        public Animation Direction { get; } = new Animation(0, -3600, 3600);

        [Display(GroupName = "Slope Blur", Name = "参照", Description = "勾配の元にする値")]
        [EnumComboBox]
        public SlopeBlurSource Reference { get => reference; set => Set(ref reference, value); }
        SlopeBlurSource reference = SlopeBlurSource.Luminance;

        [Display(GroupName = "Slope Blur", Name = "勾配の範囲", Description = "勾配を測る距離。大きいほど滑らかな傾斜にも反応します")]
        [AnimationSlider("F1", "px", 0.5, 30)]
        public Animation SlopeRadius { get; } = new Animation(4, 0.5, 200);

        [Display(GroupName = "Slope Blur", Name = "品質（反復回数）", Description = "片側のサンプル数。大きいほど滑らかですが重くなります")]
        [TextBoxSlider("F0", "回", 1, 32)]
        [DefaultValue(12)]
        [Range(1, 32)]
        public int Iterations { get => iterations; set => Set(ref iterations, value); }
        int iterations = 12;

        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        {
            return [];
        }

        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        {
            return new SlopeBlurEffectProcessor(devices, this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => [Amount, Direction, SlopeRadius];
    }
}
