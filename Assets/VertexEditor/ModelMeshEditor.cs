using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;


[Serializable]
public class PointData
{
    [ReadOnly] private Vector3 _position;
    [ReadOnly] public GameObject gb;

    public PointData(Vector3 position, GameObject gb)
    {
        _position = position;
        this.gb = gb;
    }

    public Vector3 Position
    {
        get
        {
            _position = gb.transform.localPosition;
            return _position;
        }
        set
        {
            _position = value;
            gb.transform.localPosition = value;
        }
    }

    public bool IsDel()
    {
        return gb == null;
    }
}

namespace VertexEditor
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(Animation))]
    public class ModelMeshEditor : SerializedMonoBehaviour
    {
        private static readonly string pointTag = "EditorPoint";
        [ReadOnly, SerializeField] bool isInit = false;
        [ReadOnly, SerializeField] string basePath = "Assets/Resources/Live2d/Data";

        // 控制点预制体
        [AssetsOnly, SerializeField] private GameObject pointPrefab;

        // 背景参考贴图
        [SerializeField] private Texture2D backGroundTex;

        // 贴图大小
        [SerializeField] private Vector2 backGroundSize;

        // 背景prefab
        [AssetsOnly, SerializeField] private GameObject backGroundPrefab;

        // 控制Point数据
        [SerializeField] private List<PointData> pointDatas = new List<PointData>();

        private Mesh _mesh;

        //TODO 自己写容器管理内存
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Vector2> _uv = new List<Vector2>();

        private GameObject _parentGo;
        private List<AnimationClip> _clips;
        private int _pointIndex;
        private bool isEditor;
        private String EditorBtnText = "保存";

        private bool IsEditor
        {
            set
            {
                if (value)
                {
                    EditorBtnText = "保存";
                }
                else
                {
                    EditorBtnText = "编辑";
                }

                isEditor = value;
            }
            get { return isEditor; }
        }


        private string GetMeshPath()
        {
            return $"{basePath}/{dataName}/{dataName}.asset";
        }

        private string GetDataPath()
        {
            return $"{basePath}/{dataName}/{dataName}.data";
        }

        private string GetDataDirPath()
        {
            return $"{basePath}/{dataName}/";
        }

        public void InitDataWithPrefab(GameObject prefab, string resName)
        {
            this.pointPrefab = prefab;
            dataName = resName;
            InitData();
        }

        #region 编辑器按钮

        [InlineButton("SaveOrEditor", "$EditorBtnText")] [InlineButton("InitData", "初始化数据")] [SerializeField]
        private string dataName;

        public void InitData()
        {
            isInit = true;
            IsEditor = true;
            _pointIndex = 0;
            _clips = new List<AnimationClip>();
            _clips.Add(new AnimationClip());
            // 清除原始数据
            var children = GetComponentsInChildren<Transform>(true);
            for (int i = children.Length - 1; i >= 1; i--)
            {
                DestroyImmediate(children[i].gameObject);
            }

            _parentGo = new GameObject(gameObject.name + "_parent");
            _parentGo.transform.parent = transform;
            _parentGo.transform.localPosition = Vector3.zero;
            _parentGo.transform.localRotation = Quaternion.identity;

            //添加动画组件
            Animation animationComponent = GetComponent<Animation>();
            if (animationComponent == null)
            {
                animationComponent = gameObject.AddComponent<Animation>();
            }

            // 初始化二进制数据
            var boneAnimationAsset = Util.Util.BinaryDeserilize<BoneAnimationAsset>(GetDataPath());
            if (boneAnimationAsset == null)
            {
                _mesh = new Mesh();
                boneAnimationAsset = new BoneAnimationAsset();
                var path = GetDataDirPath();
                boneAnimationAsset.MeshPath = GetMeshPath();
                Util.Util.SaveMesh(_mesh, path, dataName, true, false);
                Util.Util.SaveSerializeData(path, dataName + ".data", boneAnimationAsset);
            }
            else
            {
                _mesh = Resources.Load<Mesh>(GetMeshPath().Replace("Assets/Resources/", "").Replace(".asset", ""));
            }

            CreateEditorPoint();
            SetAnimationCurve(animationComponent, boneAnimationAsset);
        }

        [ButtonGroup("Init"), Button("计算UV")]
        private void CreateUV()
        {
            _uv = new List<Vector2>(pointDatas.Count);
            for (int i = 0; i < pointDatas.Count; i++)
            {
                _uv.Add(CalacUv(pointDatas[i].Position));
            }

            _mesh.uv = _uv.ToArray();
        }

        [ButtonGroup, Button("增加顶点")]
        private void AddPoint()
        {
            Transform[] transforms = Selection.transforms;
            var selectNum = 0;
            var pos1 = Vector3.zero;
            var pos2 = Vector3.zero;
            if (transforms.Length > 0)
            {
                foreach (var trans in transforms)
                {
                    if (trans.CompareTag(pointTag) && trans.parent == _parentGo.transform)
                    {
                        selectNum++;
                        if (selectNum == 1)
                        {
                            pos1 = trans.localPosition;
                        }
                        else
                        {
                            pos2 = trans.localPosition;
                        }
                    }
                }
            }

            var pos = Vector3.zero;
            if (selectNum == 2)
            {
                pos = (pos1 + pos2) / 2f;
            }

            _vertices.Add(pos);
            _uv.Add(CalacUv(pos));
            var point = Instantiate(pointPrefab, _parentGo.transform, false);
            point.transform.position = pos;
            point.name = gameObject.name + "_" + _pointIndex;
            point.tag = pointTag;
            pointDatas.Add(new PointData(pos, point));
            var path = $"{_parentGo.name}/{point.name}";
            EditorCurveBinding bingx = new EditorCurveBinding()
            {
                path = path,
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            EditorCurveBinding bingy = new EditorCurveBinding()
            {
                path = path,
                type = typeof(Transform),
                propertyName = "m_LocalPosition.y"
            };
            AnimationUtility.SetEditorCurve(_clips[0], bingx, new AnimationCurve());
            AnimationUtility.SetEditorCurve(_clips[0], bingy, new AnimationCurve());
            _pointIndex++;
            _mesh.vertices = _vertices.ToArray();
            _mesh.uv = _uv.ToArray();
        }

        [ButtonGroup, Button("删除点")]
        private void DeletePoint()
        {
            Transform[] transforms = Selection.transforms;
            if (transforms.Length == 0)
            {
                return;
            }

            GameObject gb = null;

            foreach (var trans in transforms)
            {
                if (trans.CompareTag(pointTag) && trans.parent == _parentGo.transform)
                {
                    for (int i = pointDatas.Count - 1; i >= 0; i--)
                    {
                        if (trans.GameObject() == pointDatas[i].gb)
                        {
                            DeletePoint(pointDatas[i], ref gb);
                        }
                    }
                }
            }

            if (gb != null)
            {
                DestroyImmediate(gb);
                _pointIndex--;
            }
        }

        [ButtonGroup, Button("增加面")]
        private void AddFace()
        {
            Transform[] transforms = Selection.transforms;
            var selectNum = 0;
            var index1 = -1;
            var index2 = -1;
            var index3 = -1;
            if (transforms.Length == 0)
            {
                Debug.Log("请选择2-3个顶点");
                return;
            }

            foreach (var trans in transforms)
            {
                if (!trans.CompareTag(pointTag) || trans.parent != _parentGo.transform)
                {
                    continue;
                }

                for (int i = 0; i < pointDatas.Count; i++)
                {
                    if (trans == pointDatas[i].gb.transform)
                    {
                        selectNum++;
                        if (selectNum == 1)
                        {
                            index1 = i;
                        }
                        else if (selectNum == 2)
                        {
                            index2 = i;
                        }
                        else if (selectNum == 3)
                        {
                            index3 = i;
                        }
                    }
                }
            }

            if (selectNum < 2 || selectNum > 3)
            {
                Debug.Log("请选择2-3个顶点");
                return;
            }

            if (selectNum == 2)
            {
                AddPoint();
                index3 = pointDatas.Count - 1;
            }

            float toward = Vector3.Cross(
                pointDatas[index1].Position - pointDatas[index2].Position,
                pointDatas[index1].Position - pointDatas[index3].Position).z;

            bool isCw = toward > 0 || Mathf.Abs(toward) < 0.001f;

            if (!isCw)
            {
                _triangles.Add(index1);
                _triangles.Add(index2);
                _triangles.Add(index3);
            }
            else
            {
                _triangles.Add(index1);
                _triangles.Add(index3);
                _triangles.Add(index2);
            }

            _mesh.triangles = _triangles.ToArray();
        }

        [ButtonGroup, Button("翻转面")]
        private void RevertFace()
        {
            Transform[] transforms = Selection.transforms;
            var selectNum = 0;
            var index1 = -1;
            var index2 = -1;
            var index3 = -1;
            if (transforms.Length == 0)
            {
                Debug.Log("请选择2-3个顶点");
                return;
            }

            foreach (var trans in transforms)
            {
                if (!trans.CompareTag(pointTag) || trans.parent != _parentGo.transform)
                {
                    continue;
                }

                for (int i = 0; i < pointDatas.Count; i++)
                {
                    if (trans.localPosition == pointDatas[i].Position)
                    {
                        selectNum++;
                        if (selectNum == 1)
                        {
                            index1 = i;
                        }
                        else if (selectNum == 2)
                        {
                            index2 = i;
                        }
                        else if (selectNum == 3)
                        {
                            index3 = i;
                        }
                    }
                }
            }

            if (selectNum != 3)
            {
                Debug.Log("请选择3个顶点");
                return;
            }

            for (int tri = _triangles.Count / 3 - 1; tri >= 0; tri--)
            {
                if
                (
                    (_triangles[tri * 3 + 0] == index1 ||
                     _triangles[tri * 3 + 1] == index1 ||
                     _triangles[tri * 3 + 2] == index1) &&
                    (_triangles[tri * 3 + 0] == index2 ||
                     _triangles[tri * 3 + 1] == index2 ||
                     _triangles[tri * 3 + 2] == index2) &&
                    (_triangles[tri * 3 + 0] == index3 ||
                     _triangles[tri * 3 + 1] == index3 ||
                     _triangles[tri * 3 + 2] == index3)
                )
                {
                    var temp = _triangles[tri * 3 + 1];
                    _triangles[tri * 3 + 1] = _triangles[tri * 3 + 2];
                    _triangles[tri * 3 + 2] = temp;
                    _mesh.triangles = _triangles.ToArray();
                    return;
                }
            }

            Debug.Log("没有找到面");
        }

        private void SaveOrEditor()
        {
            if (IsEditor)
            {
                SaveData();
                _parentGo.SetActive(false);
                IsEditor = false;
            }
            else
            {
                _parentGo.SetActive(true);
                IsEditor = true;
            }
        }

        [InlineButton("AddNextNode", "添加子节点")] [SerializeField]
        private string nodeName;

        private void AddNextNode()
        {
            if (string.IsNullOrEmpty(nodeName))
            {
                return;
            }

            var node = new GameObject(nodeName);
            node.transform.SetParent(transform);
            node.transform.localPosition = Vector3.zero;
            node.transform.localRotation = Quaternion.identity;

            var editor = node.AddComponent<ModelMeshEditor>();
            editor.InitDataWithPrefab(pointPrefab, nodeName);
            var clips = AnimationUtility.GetAnimationClips(node);
            _clips.AddRange(clips);
            var cop = GetComponent<Animation>();
            AnimationUtility.SetAnimationClips(cop, _clips.ToArray());
        }

        [Button("test")]
        void test()
        {
            Animation animation = GetComponent<Animation>();
            // var a = animation.GetClipCount();
            // Debug.Log(a);
            // animation.AddClip(_clips[0], _clips[0].name);
            animation.Play(_clips[0].name);
        }

        #endregion

        private void SaveData()
        {
            var asset = GetSerializeData();
            var path = GetDataDirPath();
            asset.MeshPath = GetMeshPath();
            Util.Util.SaveMesh(_mesh, path, dataName, true, false);
            Util.Util.SaveSerializeData(path, dataName + ".data", asset);
        }

        private Vector2 CalacUv(Vector3 pos)
        {
            return pos / backGroundSize + Vector2.one * 0.5f;
        }

        private void SetAnimationCurve(Animation animationComp, BoneAnimationAsset boneAnimationAsset)
        {
            _clips[0] = new AnimationClip();
            _clips[0].name = dataName;
            AnimationCurve curvePosX = new AnimationCurve();
            AnimationCurve curvePosY = new AnimationCurve();
            AnimationCurve curveRotX = new AnimationCurve();
            AnimationCurve curveRotZ = new AnimationCurve();
            if (boneAnimationAsset.BonePosClips != null)
            {
                foreach (var bonePosClip in boneAnimationAsset.BonePosClips)
                {
                    curvePosX.AddKey(bonePosClip.Time, bonePosClip.Position.x);
                    curvePosY.AddKey(bonePosClip.Time, bonePosClip.Position.y);
                }
            }

            if (boneAnimationAsset.BoneRotationClips != null)
            {
                foreach (var boneRotClip in boneAnimationAsset.BoneRotationClips)
                {
                    curveRotX.AddKey(boneRotClip.Time, boneRotClip.Position.x);
                    curveRotZ.AddKey(boneRotClip.Time, boneRotClip.Position.y);
                }
            }

            _clips[0].SetCurve("", typeof(Transform), "localPosition.x", curvePosX);
            _clips[0].SetCurve("", typeof(Transform), "localPosition.y", curvePosY);
            _clips[0].SetCurve("", typeof(Transform), "localEulerAnglesRaw.x", curveRotX);
            _clips[0].SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", curveRotZ);

            for (int i = 0; i < pointDatas.Count; i++)
            {
                var pointGb = pointDatas[i].gb;
                AnimationCurve curveX = new AnimationCurve();
                AnimationCurve curveY = new AnimationCurve();
                if (boneAnimationAsset.AnimationPointsClips.Count > i)
                {
                    var pointClip = boneAnimationAsset.AnimationPointsClips[i];
                    if (pointClip?.Count > 0)
                    {
                        for (int key = 0; key < pointClip.Count; key++)
                        {
                            curveX.AddKey(pointClip[key].Time, pointClip[key].Position.x);
                            curveY.AddKey(pointClip[key].Time, pointClip[key].Position.y);
                        }
                    }
                }

                var path = $"{_parentGo.name}/{pointGb.name}";
                _clips[0].SetCurve(path, typeof(Transform), "localPosition.x", curveX);
                _clips[0].SetCurve(path, typeof(Transform), "localPosition.y", curveY);
            }

            AnimationUtility.SetAnimationClips(animationComp, _clips.ToArray());
        }

        private void CreateEditorPoint()
        {
            var meshFilter = GetComponent<MeshFilter>();
            _vertices = new List<Vector3>(_mesh.vertices);
            _triangles = new List<int>(_mesh.triangles);
            _uv = new List<Vector2>(_mesh.uv);
            meshFilter.sharedMesh = _mesh;

            pointDatas = new List<PointData>(_vertices.Count);
            for (int i = 0; i < _vertices.Count; i++)
            {
                var point = Instantiate(pointPrefab, _parentGo.transform, false);
                point.transform.localPosition = _vertices[i];
                point.name = gameObject.name + "_" + i;
                point.tag = pointTag;
                pointDatas.Add(new PointData(_vertices[i], point));
                _pointIndex++;
            }

            var backGameObject = Instantiate(backGroundPrefab, _parentGo.transform, false);
            backGameObject.transform.localScale = new Vector3(backGroundSize.x, backGroundSize.y, 1);
            backGameObject.SetActive(false);
        }

        private BoneAnimationAsset GetSerializeData()
        {
            var boneAnimationAsset = new BoneAnimationAsset();
            var animaltion = GetComponent<Animation>();
            var clips = AnimationUtility.GetAnimationClips(animaltion);
            Assert.IsFalse(clips.Length == 0, "clips.Length == 0");
            var binds = AnimationUtility.GetCurveBindings(clips[0]);
            SetAnimationClipRoot(clips[0], binds, boneAnimationAsset);
            SetAnimationClipPoints(clips[0], binds, boneAnimationAsset);
            return boneAnimationAsset;
        }

        private void SetAnimationClipRoot(AnimationClip clip, EditorCurveBinding[] binds, BoneAnimationAsset res)
        {
            AnimationCurve curvePosX = null, curvePosY = null, curveRotX = null, curveRotZ = null;
            foreach (var bind in binds)
            {
                if (bind.path == "" && bind.propertyName == "m_LocalPosition.x")
                {
                    curvePosX = AnimationUtility.GetEditorCurve(clip, bind);
                }
                else if (bind.path == "" && bind.propertyName == "m_LocalPosition.y")
                {
                    curvePosY = AnimationUtility.GetEditorCurve(clip, bind);
                }
                else if (bind.path == "" && bind.propertyName == "localEulerAnglesRaw.x")
                {
                    curveRotX = AnimationUtility.GetEditorCurve(clip, bind);
                }
                else if (bind.path == "" && bind.propertyName == "localEulerAnglesRaw.z")
                {
                    curveRotZ = AnimationUtility.GetEditorCurve(clip, bind);
                }
            }

            Assert.IsFalse(curvePosX == null || curvePosY == null || curveRotX == null || curveRotZ == null,
                "curvePosX==null || curvePosY==null || curveRotX==null || curveRotZ==null");
            var PosClips = new List<BoneAnimationAsset.AnimationClipData>();
            for (int i = 0; i < curvePosY.keys.Length; i++)
            {
                var keyX = curvePosX.keys[i];
                var keyY = curvePosY.keys[i];
                var temp = new BoneAnimationAsset.AnimationClipData()
                {
                    Time = keyX.time,
                    Position = new Vector2(keyX.value, keyY.value)
                };
                PosClips.Add(temp);
            }

            var rotClips = new List<BoneAnimationAsset.AnimationClipData>();
            for (int i = 0; i < curveRotX.keys.Length; i++)
            {
                var keyX = curveRotX.keys[i];
                var keyY = curveRotZ.keys[i];
                var temp = new BoneAnimationAsset.AnimationClipData()
                {
                    Time = keyX.time,
                    Position = new Vector2(keyX.value, keyY.value)
                };
                rotClips.Add(temp);
            }

            res.BonePosClips = PosClips;
            res.BoneRotationClips = rotClips;
        }

        private static string GetGameObjectRelativePath(GameObject child, GameObject parent)
        {
            var res = "";
            if (child == parent)
            {
                return res;
            }

            res = "/" + child.name;
            while (child.transform.parent != null && child.transform.parent != parent.transform)
            {
                child = child.transform.parent.gameObject;
                res = "/" + child.name + res;
            }

            return res;
        }

        private void SetAnimationClipPoints(AnimationClip clip, EditorCurveBinding[] binds, BoneAnimationAsset res)
        {
            var AllPoinstData = new List<List<BoneAnimationAsset.AnimationClipData>>();
            res.AnimationPointsClips = AllPoinstData;
            foreach (var point in pointDatas)
            {
                var data = new List<BoneAnimationAsset.AnimationClipData>();
                AllPoinstData.Add(data);
                AnimationCurve curvex = null, curvey = null;
                var relativePath = GetGameObjectRelativePath(point.gb, gameObject);
                if (relativePath == "")
                {
                    continue;
                }

                foreach (var bind in binds)
                {
                    if ("/" + bind.path == relativePath)
                    {
                        if (bind.propertyName == "m_LocalPosition.x")
                        {
                            curvex = AnimationUtility.GetEditorCurve(clip, bind);
                        }

                        if (bind.propertyName == "m_LocalPosition.y")
                        {
                            curvey = AnimationUtility.GetEditorCurve(clip, bind);
                        }

                        if (curvex != null && curvey != null)
                        {
                            break;
                        }
                    }
                }

                if (curvex == null || curvex.keys.Length == 0)
                {
                    var temp = new BoneAnimationAsset.AnimationClipData
                    {
                        Position = new Vector2(point.Position.x, point.Position.y),
                        Time = 0
                    };
                    data.Add(temp);
                }
                else
                {
                    for (int i = 0; i < curvex.keys.Length; i++)
                    {
                        var keyX = curvex.keys[i];
                        var keyY = curvey.keys[i];
                        var temp = new BoneAnimationAsset.AnimationClipData()
                        {
                            Time = keyX.time,
                            Position = new Vector2(keyX.value, keyY.value)
                        };
                        data.Add(temp);
                    }
                }
            }
        }

        //TODO 重写删除点
        private void DeletePoint(PointData pointData, ref GameObject needDelGO)
        {
            int delFace = 0;
            int index = 0;
            for (int i = 0; i < pointDatas.Count; i++)
            {
                if (pointDatas[i] == pointData)
                {
                    index = i;
                    _vertices.RemoveAt(i);
                    _uv.RemoveAt(i);
                    if (pointData.gb != null)
                    {
                        var path = $"{_parentGo.name}/{pointData.gb.name}";
                        EditorCurveBinding bingx = new EditorCurveBinding()
                        {
                            path = path,
                            type = typeof(Transform),
                            propertyName = "m_LocalPosition.x"
                        };
                        EditorCurveBinding bingy = new EditorCurveBinding()
                        {
                            path = path,
                            type = typeof(Transform),
                            propertyName = "m_LocalPosition.y"
                        };
                        EditorCurveBinding bingz = new EditorCurveBinding()
                        {
                            path = path,
                            type = typeof(Transform),
                            propertyName = "m_LocalPosition.z"
                        };
                        AnimationUtility.SetEditorCurve(_clips[0], bingx, null);
                        AnimationUtility.SetEditorCurve(_clips[0], bingy, null);
                        AnimationUtility.SetEditorCurve(_clips[0], bingz, null);
                        needDelGO = pointData.gb;
                    }

                    pointDatas.Remove(pointData);
                }
            }

            for (int tri = _triangles.Count / 3 - 1; tri >= 0; tri--)
            {
                if (_triangles[tri * 3] == index || _triangles[tri * 3 + 1] == index ||
                    _triangles[tri * 3 + 2] == index)
                {
                    delFace++;
                    _triangles.RemoveAt(tri * 3);
                    _triangles.RemoveAt(tri * 3);
                    _triangles.RemoveAt(tri * 3);
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var offset = tri * 3 + i;
                        if (_triangles[offset] > index)
                        {
                            _triangles[offset]--;
                        }
                    }
                }
            }

            _mesh.triangles = _triangles.ToArray();
            _mesh.vertices = _vertices.ToArray();
            _mesh.uv = _uv.ToArray();

            Debug.Log($"Remove {index} vert ; Remove {delFace} faces");
        }

        private bool PointMove(PointData pointData)
        {
            var index = pointDatas.IndexOf(pointData);
            if (pointData.Position != _vertices[index])
            {
                _vertices[index] = pointData.Position;
                return true;
            }

            return false;
        }

        // Update is called once per frame
        void Update()
        {
            if (isInit == false)
            {
                return;
            }

            for (int i = pointDatas.Count - 1; i >= 0; i--)
            {
                if (pointDatas[i].IsDel())
                {
                    GameObject gameObj = null;
                    DeletePoint(pointDatas[i], ref gameObj);
                }
            }

            var isDirty = false;
            for (int i = 0; i < pointDatas.Count; i++)
            {
                isDirty |= PointMove(pointDatas[i]);
            }

            if (isDirty)
            {
                _mesh.vertices = _vertices.ToArray();
            }
        }
    }
}