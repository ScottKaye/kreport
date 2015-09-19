using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace kReport.Infrastructure
{
	// Thank you, http://stackoverflow.com/a/10502856/382456
	public static class ByteArrayConverter
    {
		/// <summary>
		/// Serializes any object into a byte array, with metadata.  Not particularily efficient, but it wins in terms of simplicity.
		/// </summary>
		/// <param name="obj">Object to serialize</param>
		/// <returns>Serialized object as a byte array</returns>
		public static byte[] GeneralSerialize(this object obj)
		{
			BinaryFormatter bf = new BinaryFormatter();
			using (var ms = new MemoryStream())
			{
				bf.Serialize(ms, obj);
				return ms.ToArray();
			}
		}

		/// <summary>
		/// Attempts to restore a byte array back into its original object.
		/// </summary>
		/// <param name="arrBytes">Byte array to deserialize</param>
		/// <returns>Resulting complex object</returns>
		public static T GeneralDeserialize<T>(this byte[] arrBytes)
		{
			using (var memStream = new MemoryStream())
			{
				var binForm = new BinaryFormatter();
				memStream.Write(arrBytes, 0, arrBytes.Length);
				memStream.Seek(0, SeekOrigin.Begin);
				var obj = binForm.Deserialize(memStream);
				return (T)obj;
			}
		}
	}
}
