using System;
using System.Collections.Generic;
using UnityEngine;

namespace VertexEditor
{
    [Serializable]
    public class BoneAnimationAsset
    {
        public class AnimationClip
        {
            public Vector2 Position;
            public float Time;
        }
        public Mesh Mesh;
        public List<List<AnimationClip>> AnimationPointsClips;
        public List<AnimationClip> BoneClips;
    }
}
