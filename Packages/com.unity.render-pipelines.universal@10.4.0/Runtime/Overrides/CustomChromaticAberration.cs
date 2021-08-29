using System;
namespace UnityEngine.Rendering.Universal
{
     [VolumeComponentMenu("Addition-post-processing/CustomChromaticAberration")]
    public class CustomChromaticAberration : VolumeComponent, IPostProcessComponent
    {
        //Post processing custom parameter
        public ClampedFloatParameter Intensity = new ClampedFloatParameter(0, 0, 0.2f);

        //Override function
        public bool IsActive()
        {
            return active && Intensity.value != 0;
        }

        public bool IsTileCompatible()
        {
            return false;
        }

    }
}