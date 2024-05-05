using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Camera
{
    [ExecuteAlways]
    public class FocalLengthLink : MonoBehaviour
    {
        public CinemachineVirtualCamera VirtualCamera = default;
        public VolumeProfile volumeProfile = default;

        private DepthOfField depthOfField = default;

        private void OnEnable()
        {
            if (VirtualCamera == null || volumeProfile == null)
            {
                enabled = false;
                return;
            }

            if (!volumeProfile.TryGet(out depthOfField))
            {
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            depthOfField.focalLength.value = VirtualCamera.m_Lens.FieldOfView;
            depthOfField.focusDistance.value = VirtualCamera.m_Lens.FocusDistance;
        }
    }
}