
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using SCPE;

using Gradient = SCPE.Gradient;

namespace PageCreator.Functions
{
    public class MenuEffects : MonoBehaviour
    {
		public static MenuEffects inst;

        void Awake()
        {
			if (inst != null)
				Destroy(gameObject);
			else
				inst = this;

			//Chroma
			{
				chroma = ScriptableObject.CreateInstance<ChromaticAberration>();
				chroma.enabled.Override(true);
				chroma.intensity.Override(0f);
				chroma.fastMode.Override(true);
			}

			//Bloom
			{
				bloom = ScriptableObject.CreateInstance<Bloom>();
				bloom.enabled.Override(true);
				bloom.intensity.Override(0f);
				bloom.anamorphicRatio.Override(0f);
				bloom.diffusion.Override(7f);
				bloom.threshold.Override(1f);
				bloom.fastMode.Override(true);
				bloom.color.Override(Color.white);
			}

			//Vignette
			{
				vignette = ScriptableObject.CreateInstance<Vignette>();
				vignette.enabled.Override(true);
				vignette.intensity.Override(0f);
				vignette.center.Override(new Vector2(0.5f, 0.5f));
				vignette.smoothness.Override(0f);
				vignette.rounded.Override(false);
				vignette.roundness.Override(0f);
				vignette.color.Override(Color.black);
			}

			//Lens
			{
				lensDistort = ScriptableObject.CreateInstance<LensDistortion>();
				lensDistort.enabled.Override(true);
				lensDistort.intensity.Override(0f);
				lensDistort.centerX.Override(0f);
				lensDistort.centerY.Override(0f);
				lensDistort.intensityX.Override(1f);
				lensDistort.intensityY.Override(1f);
				lensDistort.scale.Override(1f);
			}

			//Grain
			{
				grain = ScriptableObject.CreateInstance<Grain>();
				grain.enabled.Override(true);
				grain.intensity.Override(0f);
			}

			//ColorGrading
			{
				colorGrading = ScriptableObject.CreateInstance<ColorGrading>();
				colorGrading.enabled.Override(true);
				colorGrading.hueShift.Override(0f);
				colorGrading.contrast.Override(0f);
				colorGrading.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
				colorGrading.saturation.Override(0f);
				colorGrading.temperature.Override(0f);
				colorGrading.tint.Override(0f);
			}

			//Ripples
			{
				ripples = ScriptableObject.CreateInstance<Ripples>();
				ripples.enabled.Override(true);
				ripples.strength.Override(0f);
				ripples.speed.Override(0f);
				ripples.distance.Override(1f);
				ripples.height.Override(1f);
				ripples.width.Override(1f);
			}

            //RadialBlur
            {
				radialBlur = ScriptableObject.CreateInstance<RadialBlur>();
				radialBlur.enabled.Override(true);
				radialBlur.amount.Override(0f);
				radialBlur.iterations.Override(6);
			}

			//Color Split
			{
				colorSplit = ScriptableObject.CreateInstance<ColorSplit>();
				colorSplit.enabled.Override(true);
				colorSplit.offset.Override(0f);
				colorSplit.mode.Override(ColorSplit.SplitMode.Single);
			}

            //Gradient
            {
				gradient = ScriptableObject.CreateInstance<Gradient>();
				gradient.enabled.Override(true);
				gradient.intensity.Override(0f);
				gradient.color1.Override(new Color(0f, 0.8f, 0.56f, 0.5f));
				gradient.color2.Override(new Color(0.81f, 0.37f, 1f, 0.5f));
				gradient.rotation.Override(0f);
				gradient.blendMode.Override(Gradient.BlendMode.Linear);
			}

			//DoubleVision
			{
				doubleVision = ScriptableObject.CreateInstance<DoubleVision>();
				doubleVision.enabled.Override(true);
				doubleVision.intensity.Override(0f);
				doubleVision.mode.Override(DoubleVision.Mode.FullScreen);
			}

            //ScanLines
            {
				scanlines = ScriptableObject.CreateInstance<Scanlines>();
				scanlines.enabled.Override(true);
				scanlines.intensity.Override(0f);
				scanlines.amountHorizontal.Override(10f);
				scanlines.speed.Override(1f);
			}

			//Blur
			{
				blur = ScriptableObject.CreateInstance<Blur>();
				blur.enabled.Override(true);
				blur.amount.Override(0f);
			}

			//Pixelize
			{
				pixelize = ScriptableObject.CreateInstance<Pixelize>();
				pixelize.enabled.Override(true);
				pixelize.amount.Override(0f);
			}

			ppvolume = PostProcessManager.instance.QuickVolume(5, 100f, new PostProcessEffectSettings[]
			{
				chroma,
				bloom,
				vignette,
				lensDistort,
				grain,
				colorGrading,
				ripples,
				blur,
				pixelize,
			});
			ppvolume.isGlobal = true;
		}

		public void SetEffects(string type, string value)
        {
			switch (type.ToLower().Replace(" ", ""))
            {
				case "chroma":
                    {
						chroma.intensity.Override(float.Parse(value));
						break;
                    }
				case "bloomintensity":
                    {
						bloom.intensity.Override(float.Parse(value));
						break;
                    }
				case "bloomdiffusion":
                    {
						bloom.diffusion.Override(float.Parse(value));
						break;
                    }
				case "bloomthreshold":
                    {
						bloom.threshold.Override(float.Parse(value));
						break;
                    }
				case "bloomanamorphicratio":
                    {
						bloom.anamorphicRatio.Override(float.Parse(value));
						break;
                    }
				case "bloomcolor":
                    {
						var channels = value.Split(',');
						float[] array = new float[4];
						
						for (int i = 0; i < array.Length; i++)
                        {
							if (i < channels.Length)
								array[i] = float.Parse(channels[i]);
							else
								array[i] = 1f;
                        }

						bloom.color.Override(new Color(array[0], array[1], array[2], array[3]));
						break;
                    }
				case "vignetteintensity":
                    {
						vignette.intensity.Override(float.Parse(value));
						break;
                    }
				case "vignettesmoothness":
                    {
						vignette.smoothness.Override(float.Parse(value));
						break;
                    }
				case "vignetterounded":
                    {
						vignette.rounded.Override(bool.Parse(value));
						break;
                    }
				case "vignetteroundness":
                    {
						vignette.roundness.Override(float.Parse(value));
						break;
					}
				case "vignettecenter":
					{
						var channels = value.Split(',');
						float[] array = new float[2];

						for (int i = 0; i < array.Length; i++)
						{
							if (i < channels.Length)
								array[i] = float.Parse(channels[i]);
							else
								array[i] = 0.5f;
						}
						vignette.center.Override(new Vector2(array[0], array[1]));
						break;
					}
				case "vignettecolor":
					{
						var channels = value.Split(',');
						float[] array = new float[4];

						for (int i = 0; i < array.Length; i++)
						{
							if (i < channels.Length)
								array[i] = float.Parse(channels[i]);
							else
								array[i] = 1f;
						}

						vignette.color.Override(new Color(array[0], array[1], array[2], array[3]));
						break;
					}
			}
        }

		private PostProcessVolume ppvolume;

		public ChromaticAberration chroma;

		public Bloom bloom;

		public Vignette vignette;

		public LensDistortion lensDistort;

		public Grain grain;

		public ColorGrading colorGrading;

		public Ripples ripples;

		public RadialBlur radialBlur;

		public ColorSplit colorSplit;

		public Gradient gradient;

		public DoubleVision doubleVision;

		public Scanlines scanlines;

		public Blur blur;

		public Pixelize pixelize;
	}
}
