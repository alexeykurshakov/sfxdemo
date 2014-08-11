using System;
using System.Runtime.Serialization;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class ByteSerializer 
{
	[Serializable]
    private class SerializableVector3 : ISerializable
	{
	    public SerializableVector3(Vector3 v)
	    {
	        this.V = v;
	    }

        public SerializableVector3(SerializationInfo info, StreamingContext context)
        {                        
            var x = info.GetDouble("x");
            var y = info.GetDouble("y");
            var z = info.GetDouble("z");
            this.V = new Vector3((float)x, (float)y, (float)z);
        }

	    public Vector3 V;

	    public void GetObjectData(SerializationInfo info, StreamingContext context)
	    {
	        info.AddValue("x", this.V.x);
            info.AddValue("y", this.V.y);
            info.AddValue("z", this.V.z);
	    }	 
	}

	[Serializable]
    private class SerializableQuaternion : ISerializable
	{
        public SerializableQuaternion(Quaternion v)
	    {
	        this.Q = v;
	    }

        public SerializableQuaternion(SerializationInfo info, StreamingContext context)
        {
            var x = info.GetDouble("x");
            var y = info.GetDouble("y");
            var z = info.GetDouble("z");
            var w = info.GetDouble("w");
            this.Q = new Quaternion((float)x, (float)y, (float)z, (float)w);
        }

	    public Quaternion Q;

	    public void GetObjectData(SerializationInfo info, StreamingContext context)
	    {            
	        info.AddValue("x", this.Q.x);
            info.AddValue("y", this.Q.y);
            info.AddValue("z", this.Q.z);
            info.AddValue("w", this.Q.w);
	    }	 
	}

	public static byte[] Serialize(Vector3 val)
	{
	    var sV = new SerializableVector3(val);
		var bf = new BinaryFormatter();
		var memStr = new MemoryStream();

        bf.Serialize(memStr, sV);
		memStr.Position = 0;

		return memStr.ToArray();
	}

	public static byte[] Serialize(Quaternion val)
	{
        var sQ = new SerializableQuaternion(val);

		var bf = new BinaryFormatter();
		var memStr = new MemoryStream();

        bf.Serialize(memStr, sQ);
		memStr.Position = 0;
		
		return memStr.ToArray();
	}

	public static Vector3 DeserializeV(byte[] dataStream)
	{
		var memStr = new MemoryStream(dataStream);
		memStr.Position = 0;
		var bf = new BinaryFormatter();

        return ((SerializableVector3)bf.Deserialize(memStr)).V;
	}

	public static Quaternion DeserializeQ(byte[] dataStream)
	{
		var memStr = new MemoryStream(dataStream);
		memStr.Position = 0;
		var bf = new BinaryFormatter();

	    return ((SerializableQuaternion) bf.Deserialize(memStr)).Q;
	}
}
