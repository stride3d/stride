using System;
using System.Runtime.InteropServices;
using System.Text;

//wchar_t is not working on linux, see https://github.com/swig/swig/issues/1233

namespace Stride.TextureConverter
{
    public class UTC32StringMarshaller : ICustomMarshaler
    {
        static UTC32StringMarshaller singleton;

        public IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
                return IntPtr.Zero;
            if (!(managedObj is string))
                throw new MarshalDirectiveException("UTC32StringMarshaller must be used on a string.");

            var nullTerminated = $"{(string)managedObj}{Convert.ToChar(0x0).ToString()}";
            byte[] strbuf = Encoding.UTF32.GetBytes(nullTerminated);
            IntPtr buffer = Marshal.AllocHGlobal(strbuf.Length);
            Marshal.Copy(strbuf, 0, buffer, strbuf.Length);
            return buffer;
        }

        public unsafe object MarshalNativeToManaged(IntPtr pNativeData)
        {
            int pos = 0;
            while (Marshal.ReadInt32(pNativeData, pos) != 0)
                pos += 4;

            byte[] strbuf = new byte[pos];
            Marshal.Copy((IntPtr)pNativeData, strbuf, 0, pos);
            return Encoding.UTF32.GetString(strbuf);
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            Marshal.FreeHGlobal(pNativeData);
        }

        public void CleanUpManagedData(object managedObj)
        {
        }

        public int GetNativeDataSize()
        {
            return -1;
        }

        public static ICustomMarshaler GetInstance(string cookie)
        {
            if (singleton == null)
                return singleton = new UTC32StringMarshaller();

            return singleton;
        }
    }
}