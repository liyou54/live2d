using System;
using System.Collections.Generic;
using UnityEngine;

namespace VertexEditor
{
    [System.Serializable]
    public class BoneAnimationAsset
    {
        [System.Serializable]
        public class AnimationClipData
        {
            public Vector2 Position
            {
                get { return new Vector2(x, y); }
                set
                {
                    x = value.x;
                    y = value.y;
                }
            }

            private float x;
            private float y;
            public float Time;
        }

        public string MeshPath;
        public List<List<AnimationClipData>> AnimationPointsClips;
        public List<AnimationClipData> BonePosClips;
        public List<AnimationClipData> BoneRotationClips;
    }
}