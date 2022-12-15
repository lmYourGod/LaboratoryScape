// Designed by Kinemation, 2022

using System.Collections.Generic;
using Kinemation.FPSFramework.Runtime.Core;
using UnityEngine;

namespace Kinemation.FPSFramework.Runtime.Layers
{
    public class LookLayer : AnimLayer
    {
        [Header("Layer Blending")] [SerializeField, Range(0f, 1f)]
        private float layerAlpha;
        [SerializeField] private float layerInterpSpeed;
        [SerializeField, Range(0f, 1f)] private float handsLayerAlpha;
        [SerializeField] private float handsLerpSpeed;

        [Header("Offsets")] 
        [SerializeField] private Vector3 pelvisOffset;

        [Header("Aim Offset")] [SerializeField]
        private AimOffset lookUpOffset;

        [SerializeField] private AimOffset lookRightOffset;

        [SerializeField] private bool enableAutoDistribution;
        [SerializeField] private bool enableManualSpineControl;

        [SerializeField, Range(-90f, 90f)] private float aimUp;
        [SerializeField, Range(-90f, 90f)] private float aimRight;

        [SerializeField] private float smoothAim;
        [SerializeField] protected bool detectZeroFrames = true;

        [SerializeField] [Range(-1, 1)] private int leanDirection;
        [SerializeField] private float leanAmount = 45f;
        [SerializeField] private float leanSpeed;

        private float leanInput;
        
        private float _interpHands;
        private float _interpLayer;
        private Vector2 _smoothAim;

        // Used to detect zero key frames
        [SerializeField] [HideInInspector] private CachedBones cachedBones;
        [SerializeField] [HideInInspector] private CachedBones cachedLocal;
        [SerializeField] [HideInInspector] private CachedBones cacheRef;
        
        public override void OnPreAnimUpdate()
        {
            if (Application.isEditor && detectZeroFrames)
            {
                CheckZeroFrames();
            }
        }

        public override void OnAnimUpdate()
        {
            ApplySpineLayer();
        }

        public override void OnPostIK()
        {
            if (Application.isEditor && detectZeroFrames)
            {
                CacheBones();
            }
        }

        public void SetAimRotation(Vector2 newAimRot)
        {
            if (!enableManualSpineControl)
            {
                aimUp += newAimRot.y;
                aimRight += newAimRot.x;

                aimUp = Mathf.Clamp(aimUp, -90f, 90f);
                aimRight = Mathf.Clamp(aimRight, -90f, 90f);
            }
        }

        public void SetLookWeight(float weight)
        {
            layerAlpha = Mathf.Clamp01(weight);
        }

        public void SetLeanInput(int direction)
        {
            leanDirection = direction;
        }

        private void Awake()
        {
            lookUpOffset.Init();
            lookRightOffset.Init();
        }

        private void OnValidate()
        {
            if (cachedBones.lookUp == null)
            {
                cachedBones.lookUp ??= new List<Quaternion>();
                cachedLocal.lookUp ??= new List<Quaternion>();
                cacheRef.lookUp ??= new List<Quaternion>();
            }

            if (!lookUpOffset.IsValid() || lookUpOffset.IsChanged())
            {
                lookUpOffset.Init();

                cachedBones.lookUp.Clear();
                cachedLocal.lookUp.Clear();
                cacheRef.lookUp.Clear();

                for (int i = 0; i < lookUpOffset.bones.Count; i++)
                {
                    cachedBones.lookUp.Add(Quaternion.identity);
                    cachedLocal.lookUp.Add(Quaternion.identity);
                    cacheRef.lookUp.Add(Quaternion.identity);
                }
            }

            if (!lookRightOffset.IsValid() || lookRightOffset.IsChanged())
            {
                lookRightOffset.Init();
            }

            void Distribute(ref AimOffset aimOffset)
            {
                if (enableAutoDistribution)
                {
                    bool enable = false;
                    int divider = 1;
                    float sum = 0f;

                    for (int i = 0; i < aimOffset.bones.Count - aimOffset.indexOffset; i++)
                    {
                        if (enable)
                        {
                            var bone = aimOffset.bones[i];
                            bone.maxAngle.x = (90f - sum) / divider;
                            aimOffset.bones[i] = bone;
                            continue;
                        }

                        if (!Mathf.Approximately(aimOffset.bones[i].maxAngle.x, aimOffset.angles[i].x))
                        {
                            divider = aimOffset.bones.Count - aimOffset.indexOffset - (i + 1);
                            enable = true;
                        }

                        sum += aimOffset.bones[i].maxAngle.x;
                    }
                }

                if (enableAutoDistribution)
                {
                    bool enable = false;
                    int divider = 1;
                    float sum = 0f;

                    for (int i = 0; i < aimOffset.bones.Count - aimOffset.indexOffset; i++)
                    {
                        if (enable)
                        {
                            var bone = aimOffset.bones[i];
                            bone.maxAngle.y = (90f - sum) / divider;
                            aimOffset.bones[i] = bone;
                            continue;
                        }

                        if (!Mathf.Approximately(aimOffset.bones[i].maxAngle.y, aimOffset.angles[i].y))
                        {
                            divider = aimOffset.bones.Count - aimOffset.indexOffset - (i + 1);
                            enable = true;
                        }

                        sum += aimOffset.bones[i].maxAngle.y;
                    }
                }

                for (int i = 0; i < aimOffset.bones.Count - aimOffset.indexOffset; i++)
                {
                    aimOffset.angles[i] = aimOffset.bones[i].maxAngle;
                }
            }

            if (lookUpOffset.bones.Count > 0)
            {
                Distribute(ref lookUpOffset);
            }

            if (lookRightOffset.bones.Count > 0)
            {
                Distribute(ref lookRightOffset);
            }
        }
        
        private void CheckZeroFrames()
        {
            if (cachedBones.pelvis.Item1 == rigData.pelvis.localPosition)
            {
                rigData.pelvis.localPosition = cachedLocal.pelvis.Item1;
            }

            bool bZeroSpine = false;
            for (int i = 0; i < cachedBones.lookUp.Count; i++)
            {
                var bone = lookUpOffset.bones[i].bone;
                if (bone == null)
                {
                    continue;
                }

                if (cachedBones.lookUp[i] == bone.localRotation)
                {
                    bZeroSpine = true;
                    bone.localRotation = cachedLocal.lookUp[i];
                }
            }
            
            if (bZeroSpine)
            {
                rigData.masterDynamic.Retarget();
                rigData.rightHand.Retarget();
                rigData.leftHand.Retarget();
                rigData.rightFoot.Retarget();
                rigData.leftFoot.Retarget();
            }
            
            cacheRef.pelvis.Item1 = rigData.pelvis.localPosition;

            for (int i = 0; i < lookUpOffset.bones.Count; i++)
            {
                var bone = lookUpOffset.bones[i].bone;
                if (bone == null)
                {
                    continue;
                }
                
                cacheRef.lookUp[i] = bone.localRotation;
            }
        }
        
        private void CacheBones()
        {
            cachedBones.pelvis.Item1 = rigData.pelvis.localPosition;
            cachedLocal.pelvis.Item1 = cacheRef.pelvis.Item1;

            for (int i = 0; i < lookUpOffset.bones.Count; i++)
            {
                var bone = lookUpOffset.bones[i].bone;
                if (bone == null)
                {
                    continue;
                }

                cachedBones.lookUp[i] = bone.localRotation;
                cachedLocal.lookUp[i] = cacheRef.lookUp[i];
            }
        }

        private bool BlendLayers()
        {
            _interpLayer = CoreToolkitLib.GlerpLayer(_interpLayer, layerAlpha, layerInterpSpeed);
            return Mathf.Approximately(_interpLayer, 0f);
        }

        private void ApplySpineLayer()
        {
            if (BlendLayers())
            {
                return;
            }
            
            Vector3 pelvisFinal = Vector3.Lerp(Vector3.zero, pelvisOffset, _interpLayer);
            CoreToolkitLib.MoveInBoneSpace(rigData.rootBone, rigData.pelvis, pelvisFinal);

            _smoothAim.y = CoreToolkitLib.GlerpLayer(_smoothAim.y, aimUp, smoothAim);
            _smoothAim.x = CoreToolkitLib.GlerpLayer(_smoothAim.x, aimRight, smoothAim);

            leanInput = CoreToolkitLib.Glerp(leanInput, leanDirection, leanSpeed);

            foreach (var bone in lookRightOffset.bones)
            {
                if (!Application.isPlaying && bone.bone == null)
                {
                    continue;
                }

                float angleFraction = _smoothAim.x >= 0f ? bone.maxAngle.y : bone.maxAngle.x;
                CoreToolkitLib.RotateInBoneSpace(rigData.rootBone.rotation, bone.bone,
                    new Vector3(0f, _smoothAim.x * _interpLayer / (90f / angleFraction),0f));
            }
            
            foreach (var bone in lookRightOffset.bones)
            {
                if (!Application.isPlaying && bone.bone == null)
                {
                    continue;
                }

                float angleFraction = _smoothAim.x >= 0f ? bone.maxAngle.y : bone.maxAngle.x;
                CoreToolkitLib.RotateInBoneSpace(rigData.rootBone.rotation * Quaternion.Euler(0f, _smoothAim.x, 0f), bone.bone,
                    new Vector3(0f, 0f,leanAmount * leanInput * _interpLayer / (90f / angleFraction)));
            }
            
            Vector3 rightHandLoc = rigData.rightHand.obj.transform.position;
            Quaternion rightHandRot = rigData.rightHand.obj.transform.rotation;

            Vector3 leftHandLoc = rigData.leftHand.obj.transform.position;
            Quaternion leftHandRot = rigData.leftHand.obj.transform.rotation;

            foreach (var bone in lookUpOffset.bones)
            {
                if (!Application.isPlaying && bone.bone == null)
                {
                    continue;
                }

                float angleFraction = _smoothAim.y >= 0f ? bone.maxAngle.y : bone.maxAngle.x;

                CoreToolkitLib.RotateInBoneSpace(rigData.rootBone.rotation * Quaternion.Euler(0f, _smoothAim.x, 0f),
                    bone.bone,
                    new Vector3(_smoothAim.y * _interpLayer / (90f / angleFraction), 0f, 0f));
            }

            _interpHands = CoreToolkitLib.GlerpLayer(_interpHands, handsLayerAlpha, handsLerpSpeed);

            rigData.rightHand.obj.transform.position = Vector3.Lerp(rightHandLoc,
                rigData.rightHand.obj.transform.position,
                _interpHands);
            rigData.rightHand.obj.transform.rotation = Quaternion.Slerp(rightHandRot,
                rigData.rightHand.obj.transform.rotation,
                _interpHands);

            rigData.leftHand.obj.transform.position = Vector3.Lerp(leftHandLoc, rigData.leftHand.obj.transform.position,
                _interpHands);
            rigData.leftHand.obj.transform.rotation = Quaternion.Slerp(leftHandRot,
                rigData.leftHand.obj.transform.rotation,
                _interpHands);
        }
    }
}