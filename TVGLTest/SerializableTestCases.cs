using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using TVGL;

namespace TVGLTest.SerializableTestCases
{
    [DataContract]
    public class PathsSerializableClass
    {
        [DataMember]
        public List<List<Point>> Points;

        public PathsSerializableClass(List<List<Point>> points)
        {
            Points = points;
        }

        #region Serialization Methods
        public static void Serialize(PathsSerializableClass obj, string filename)
        {
            using (var writer = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                var ser = new DataContractSerializer(typeof(PathsSerializableClass));
                ser.WriteObject(writer, obj);
            }
        }

        public static PathsSerializableClass Deserialize(string filename)
        {
            using (var reader = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                var ser = new DataContractSerializer(typeof(PathsSerializableClass));
                var output = (PathsSerializableClass)ser.ReadObject(reader);
                return output;
            }
        }
        #endregion
    }

    [DataContract]
    public class PathSerializableClass
    {
        [DataMember]
        public List<Point> Points;

        public PathSerializableClass(List<Point> points)
        {
            Points = points;
        }

        #region Serialization Methods
        public static void Serialize(PathSerializableClass obj, string filename)
        {
            using (var writer = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                var ser = new DataContractSerializer(typeof(PathSerializableClass));
                ser.WriteObject(writer, obj);
            }
        }

        public static PathSerializableClass Deserialize(string filename)
        {
            using (var reader = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                var ser = new DataContractSerializer(typeof(PathSerializableClass));
                var output = (PathSerializableClass)ser.ReadObject(reader);
                return output;
            }
        }
        #endregion
    }
}
