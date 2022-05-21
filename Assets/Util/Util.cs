using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

namespace Util
{
    public static class Util
    {
        public static void SaveMesh(Mesh mesh,string path, string name, bool makeNewInstance, bool optimizeMesh)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fpath = path + name + ".asset";
            if (string.IsNullOrEmpty(fpath)) return;
 
 
            Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;
 
            if (optimizeMesh)
                MeshUtility.Optimize(meshToSave);
 
            AssetDatabase.CreateAsset(meshToSave, fpath);
            AssetDatabase.SaveAssets();
        }

        public static void SaveSerializeData(string path, string name, object data)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fpath = path + name;
            if (string.IsNullOrEmpty(fpath)) return;
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = File.Create(fpath);
            bf.Serialize(fs, data);
            fs.Close();
            AssetDatabase.Refresh();
        }
        public static T BinaryDeserilize<T>(string f) 
        {
            if (!File.Exists(f)) return default(T);
            var bytes = File.ReadAllBytes(f);
            MemoryStream memoryStream = new MemoryStream(bytes);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            T testSerilize = (T)binaryFormatter.Deserialize(memoryStream);
            memoryStream.Close();
            return testSerilize;
        }
    }
}