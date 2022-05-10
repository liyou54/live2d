using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VertexEditor;

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
}

namespace VertexEditor
{
    [ExecuteInEditMode]
    public class ModelMeshEditor : SerializedMonoBehaviour
    {
        private static readonly string pointTag = "EditorPoint";
        [ReadOnly, SerializeField] bool isInit = false;

        [SerializeField] Mesh mesh;
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

        //TODO 自己写容器管理内存
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Vector2> _uv = new List<Vector2>();

        void Start()
        {
            var meshFilter = GetComponent<MeshFilter>();
            mesh = meshFilter.mesh;
            meshFilter.sharedMesh = mesh;
            CreateEditorPoint();
        }


        [ButtonGroup("Init"), Button("初始化")]
        public void CreateEditorPoint()
        {
            isInit = true;
            var children = GetComponentsInChildren<Transform>();
            for (int i = children.Length - 1; i >= 1; i--)
            {
                DestroyImmediate(children[i].gameObject);
            }
            var meshFilter = GetComponent<MeshFilter>();
            var meshFilterMesh = meshFilter.sharedMesh;
            mesh = new Mesh();
            _vertices = new List<Vector3>(meshFilterMesh.vertices);
            _triangles = new List<int>(meshFilterMesh.triangles);
            // _uv = new List<Vector2>(meshFilterMesh.uv);
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            // mesh.uv = _uv.ToArray();
            meshFilter.sharedMesh = mesh;


            pointDatas = new List<PointData>(_vertices.Count);
            for (int i = 0; i < _vertices.Count; i++)
            {
                var point = Instantiate(pointPrefab, transform, false);
                point.transform.position = _vertices[i];
                point.tag = pointTag;
                pointDatas.Add(new PointData(_vertices[i], point));
            }
            var backGameObject = Instantiate(backGroundPrefab, transform, false);
            backGameObject.transform.localScale = new Vector3(backGroundSize.x, backGroundSize.y, 1);
            CreateUV();
        }

        [ButtonGroup("Init"), Button("计算UV")]
        private void CreateUV()
        {
            _uv = new List<Vector2>(pointDatas.Count);
            for (int i = 0; i < pointDatas.Count; i++)
            {
                _uv.Add(CalacUv(pointDatas[i].Position));
            }
            mesh.uv = _uv.ToArray();
        }

        private Vector2 CalacUv(Vector3 pos)
        {
            return pos / backGroundSize + Vector2.one * 0.5f;
        }
        
        [ButtonGroup, Button("增加顶点")]
        public void AddPoint()
        {
            Transform[] transforms = Selection.transforms;
            var selectNum = 0;
            var pos1 = Vector3.zero;
            var pos2 = Vector3.zero;
            if (transforms.Length > 0)
            {
                foreach (var trans in transforms)
                {
                    if (trans.CompareTag(pointTag))
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
            var point = Instantiate(pointPrefab, transform, false);
            point.transform.position = pos;
            point.tag = pointTag;
            pointDatas.Add(new PointData(pos, point));
            mesh.vertices = _vertices.ToArray();
            mesh.uv = _uv.ToArray();
        }

        [ButtonGroup, Button("删除点")]
        public void DeletePoint()
        {
            Transform[] transforms = Selection.transforms;
            if (transforms.Length == 0)
            {
                return;
            }

            foreach (var trans in transforms)
            {
                if (trans.CompareTag(pointTag))
                {
                    for (int i = pointDatas.Count - 1; i >= 0; i--)
                    {
                        if (trans.GameObject() == pointDatas[i].gb)
                        {
                            DeletePoint(pointDatas[i]);
                        }
                    }
                }
            }
        }

        [ButtonGroup, Button("增加面")]
        public void AddFace()
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
                if (!trans.CompareTag(pointTag))
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

            mesh.triangles = _triangles.ToArray();
        }

        [ButtonGroup, Button("翻转面")]
        public void RevertFace()
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
                if (!trans.CompareTag(pointTag))
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
                    mesh.triangles = _triangles.ToArray();
                    return;
                }
            }
            
            Debug.Log("没有找到面");
        }

        //TODO 重写删除点
        public void DeletePoint(PointData pointData)
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
                        DestroyImmediate(pointData.gb);
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

            mesh.triangles = _triangles.ToArray();
            mesh.vertices = _vertices.ToArray();
            // mesh.uv = _uv.ToArray();

            Debug.Log($"Remove {index} vert ; Remove {delFace} faces");
        }

        public bool PointMove(PointData pointData)
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

            var isDirty = false;
            for (int i = 0; i < pointDatas.Count; i++)
            {
                isDirty |= PointMove(pointDatas[i]);
            }

            if (isDirty)
            {
                mesh.vertices = _vertices.ToArray();
            }
        }
    }
}