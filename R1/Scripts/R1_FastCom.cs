using AOT;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;   // Import / Call C++ functions

public class R1_FastCom
{
    //--------------------------------------------------------------------------------
    // Init / Dispose

    [DllImport(
    "FastComUnity",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern int FastCom_Init_Reader(bool blockWaitForWriter);


    /// <summary> Initialize the FastCom reader, allowing you to read data thorugh Shared Memory. Might hang! </summary>
    /// <returns> < 0 if failed, else it succeeded </returns>
    public static int Init_Reader(bool blockWaitForWriter)
    {
        try
        {
            int result = FastCom_Init_Reader(blockWaitForWriter);
            return result;
        }
        catch (System.DllNotFoundException)
        {
            UnityEngine.Debug.LogError("Init_Reader: SGConnect.dll not found");
        }
        return -1;
    }


    [DllImport(
    "FastComUnity",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern int FastCom_Init_Writer(byte[] path, int pathLength);

    /// <summary> Initialize the FastCom writer, allowing you so send data through shared Memory. Might Hang! </summary>
    /// <param name="configPath"> < 0 if failed, else it succeeded </param>
    /// <returns></returns>
    public static int Init_Writer(string configPath)
    {
        try
        {
            if (string.IsNullOrEmpty(configPath))
                return -2;

            byte[] utf8Path = System.Text.Encoding.UTF8.GetBytes(configPath);
            int result = FastCom_Init_Writer(utf8Path, utf8Path.Length);
            return result;
        }
        catch (System.DllNotFoundException)
        {
            UnityEngine.Debug.LogError("Init_Writer: SGConnect.dll not found");
        }
        return -1;
    }


    [DllImport(
        "FastComUnity",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern int FastCom_Dispose();


    /// <summary> Disposes of any active FastCom Readers / Writers that are present. </summary>
    /// <returns></returns>
    public static int Dispose()
    {
        try
        {
            int result = FastCom_Dispose();
            return result;
        }
        catch (System.DllNotFoundException)
        {
            UnityEngine.Debug.LogError("Dispose: SGConnect.dll not found");
        }
        return -1;
    }





    //--------------------------------------------------------------------------------
    // Overarching Functions


    [DllImport(
        "FastComUnity",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern int FastCom_GetActiveDeviceCount();


    /// <summary> Returns the amount of active devices as published by FastCom </summary>
    /// <returns></returns>
    public static int GetActiveDeviceCount()
    {
        try
        {
            int result = FastCom_GetActiveDeviceCount();
            return result;
        }
        catch (System.DllNotFoundException)
        {
            UnityEngine.Debug.LogError("GetActiveDeviceCount: SGConnect.dll not found");
        }
        return -1;
    }



    [DllImport(
        "FastComUnity",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern int FastCom_GetDeviceIds(
        [Out] int[] ids,
        int capacity
    );

    /// <summary> Returns a list of active deviceIds </summary>
    /// <returns></returns>
    public static int[] GetDeviceIds()
    {
        try
        {
            int size = GetActiveDeviceCount();
            if (size > 0)
            {
                int[] res = new int[size];
                FastCom_GetDeviceIds(res, size);
                return res;
            }
        }
        catch (System.DllNotFoundException)
        {
            UnityEngine.Debug.LogError("GetDeviceIds: SGConnect.dll not found");
        }
        return new int[0];
    }



    [DllImport(
        "FastComUnity",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern int FastCom_GetRightHandDeviceId();

    /// <summary> Returns the DeviceID of a glove that has its DeviceInfo set to 'Right' </summary>
    /// <returns> -1 if no Right Glove is Connected. Otherwise returns a valid deviceId </returns>
    public static int GetRightHandDeviceId()
    {
        try
        {
            int result = FastCom_GetRightHandDeviceId();
            return result;
        }
        catch (System.DllNotFoundException)
        {
            UnityEngine.Debug.LogError("GetRightHandDeviceId: SGConnect.dll not found");
        }
        return -1;
    }

    [DllImport(
        "FastComUnity",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern int FastCom_GetLeftHandDeviceId();

    /// <summary> Returns the DeviceID of a glove that has its DeviceInfo set to 'left' </summary>
    /// <returns> -1 if no Left Glove is Connected. Otherwise returns a valid deviceId </returns>
    public static int GetLeftHandDeviceId()
    {
        try
        {
            int result = FastCom_GetLeftHandDeviceId();
            return result;
        }
        catch (System.DllNotFoundException)
        {
            UnityEngine.Debug.LogError("GetLeftHandDeviceId: SGConnect.dll not found");
        }
        return -1;
    }





    //-------------------------------------------------------------------------------------------
    // Generic Read  Function(s)


    private const string DllName = "FastComUnity";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FastCom_GetFirmwareVersion(
        int deviceId,
        [Out] byte[] firmwareString,
        int nameCapacity
    );

    /// <summary> Retireve FirmwareVersion as a string from a particular device. </summary>
    /// <param name="deviceId"></param>
    /// <param name="firmwareString"></param>
    /// <param name="bufferSize"></param>
    /// <returns></returns>
    public static int GetFirmwareVersion(int deviceId, out string firmwareString, int bufferSize = 32)
    {
        try
        {
            byte[] fwBuffer = new byte[bufferSize];
            int actualFwSize = FastCom_GetFirmwareVersion(deviceId, fwBuffer, bufferSize);
            if (actualFwSize < 0) //can return < 0 values if an error has occured (no paramaters, not inited, value does not yet exist.)
            {
                UnityEngine.Debug.LogError("Error readinf Firmware Value; code " + actualFwSize);
                firmwareString = "";
                return actualFwSize;
            }
            if (actualFwSize > bufferSize)
            {
                //Not enough size given. Warn the use.
                UnityEngine.Debug.LogError($"FastCom_GetFirmwareVersion for deviceID {deviceId}: Buffer size {bufferSize} is too small to contain Firmware string of length {actualFwSize}." +
                    $" Please try again with a larger bufferSize or accept a truncated version");
                firmwareString = System.Text.Encoding.UTF8.GetString(fwBuffer, 0, bufferSize);
            }
            else //otherwise the buffer is big enough to contain the FWName, so we can just use that particular value
                firmwareString = System.Text.Encoding.UTF8.GetString(fwBuffer, 0, actualFwSize);
            return actualFwSize;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("FastCom_GetFirmwareVersion: " + ex.Message);
        }
        firmwareString = "";
        return -1;
    }



    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FastCom_GetDeviceInfo_Int32(
        int deviceId,
        [In] byte[] fieldName,
        int fieldNameCapacity
    );


    /// <summary> Read a particular field from a DeviceIds DeviceInfo </summary>
    /// <param name="deviceId"></param>
    /// <param name="fieldName"></param>
    /// <returns>Returns value < 0 if the field does not exist</returns>
    public static int GetDeviceInfo_Int32(int deviceId, string fieldName)
    {
        try
        {
            byte[] utf8Field = System.Text.Encoding.UTF8.GetBytes(fieldName);
            int result = FastCom_GetDeviceInfo_Int32(deviceId, utf8Field, utf8Field.Length);
            return result;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("GetDeviceInfo_Int32: " + ex.Message);
        }
        return -9000;
    }



    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FastCom_ReadFast_Int32Array(
        int deviceId,
        [In] byte[] fieldName,
        int fieldNameCapacity,
        [Out] int[] output,
        int outputCapacity
    );

    /// <summary> Read a single integer Point Array from a deviceID's field. Specify a buffer to contain the data.  </summary>
    /// <param name="deviceId"></param>
    /// <param name="fieldName"></param>
    /// <param name="bufferSize"></param>
    /// <param name="array"></param>
    /// <returns>Value < 0 if failed, otherwise the length of Arrray </returns>
    public static int ReadFast_Int32Array(int deviceId, string fieldName, int bufferSize, out int[] array)
    {
        try
        {
            int[] buffer = new int[bufferSize];
            byte[] utf8Field = System.Text.Encoding.UTF8.GetBytes(fieldName);
            int arraySize = FastCom_ReadFast_Int32Array(deviceId, utf8Field, utf8Field.Length, buffer, bufferSize);
            if (arraySize < 0) //errror occured with parameters etc
            {
                array = new int[0];
                return arraySize;
            }
            if (arraySize > bufferSize) //we got an array but it's larger than our buffer!
            {   //Not enough size given. Warn the user
                UnityEngine.Debug.LogError($"ReadFast_Int32Array for deviceID {deviceId}: Buffer size {bufferSize} is too small to contain int[] of length {arraySize} in {fieldName}." +
                    $" Please try again with a larger bufferSize or accept a truncated version");
                array = new int[bufferSize];
            }
            else
                array = new int[arraySize];

            for (int i=0; i<array.Length; i++)
                array[i] = buffer[i];
            return array.Length; //we return how many items were safely copied over.
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("GetDeviceInfo_Int32: " + ex.Message);
        }
        array = new int[0];
        return -9000;
    }



    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FastCom_ReadFast_FloatArray(
        int deviceId,
        [In] byte[] fieldName,
        int fieldNameCapacity,
        [Out] float[] output,
        int outputCapacity
    );

    /// <summary> Read a single Floating Point Array from a deviceID's field. Specify a buffer to contain the data. </summary>
    /// <param name="deviceId"></param>
    /// <param name="fieldName"></param>
    /// <param name="bufferSize"></param>
    /// <param name="array"></param>
    /// <returns> Value < 0 if failed, otherwise the length of Arrray </returns>
    public static int ReadFast_FloatArray(int deviceId, string fieldName, int bufferSize, out float[] array)
    {
        try
        {
            float[] buffer = new float[bufferSize];
            byte[] utf8Field = System.Text.Encoding.UTF8.GetBytes(fieldName);
            int arraySize = FastCom_ReadFast_FloatArray(deviceId, utf8Field, utf8Field.Length, buffer, bufferSize);
            if (arraySize < 0) //errror occured with parameters etc
            {
                array = new float[0];
                return arraySize;
            }
            if (arraySize > bufferSize) //we got an array but it's larger than our buffer!
            {   //Not enough size given. Warn the user
                UnityEngine.Debug.LogError($"ReadFast_Int32Array for deviceID {deviceId}: Buffer size {bufferSize} is too small to contain int[] of length {arraySize} in {fieldName}." +
                    $" Please try again with a larger bufferSize or accept a truncated version");
                array = new float[bufferSize];
            }
            else
                array = new float[arraySize];

            for (int i = 0; i < array.Length; i++)
                array[i] = buffer[i];
            return array.Length; //we return how many items were safely copied over.
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("GetDeviceInfo_Int32: " + ex.Message);
        }
        array = new float[0];
        return -9000;
    }






    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FastCom_ReadFast_Float2DArray(
        int deviceId,
        [In] byte[] fieldName,
        int fieldNameCapacity,
        [Out] float[] output,
        int outputCapacity,
        out int out_Rows,
        out int out_Columns
    );



    /// <summary> Read a Floating POint array flat - useful when I want to do some post-processing </summary>
    /// <param name="deviceId"></param>
    /// <param name="fieldName"></param>
    /// <param name="bufferSize"></param>
    /// <param name="array"></param>
    /// <param name="rows"></param>
    /// <param name="columns"></param>
    /// <returns> value < 0 if something went wrong, otherwise an array and the rows / columns. (Fixed size) </returns>
    public static int ReadFast_FlatFloat2DArray(int deviceId, string fieldName, int bufferSize, out float[] array, out int rows, out int columns)
    {
        rows = 0;
        columns = 0;
        try
        {
            float[] buffer = new float[bufferSize];
            byte[] utf8Field = System.Text.Encoding.UTF8.GetBytes(fieldName);
            int arraySize = FastCom_ReadFast_Float2DArray(deviceId, utf8Field, utf8Field.Length, buffer, bufferSize, out rows, out columns);
            if (arraySize < 0) //errror occured with parameters etc
            {
                array = new float[0];
                return arraySize;
            }
            if (arraySize > bufferSize) //we got an array but it's larger than our buffer!
            {   //Not enough size given. Warn the user
                UnityEngine.Debug.LogError($"ReadFast_Float2DArray for deviceID {deviceId}: Buffer size {bufferSize} is too small to contain float[] of length {arraySize} ({rows}x{columns}) in {fieldName}." +
                    $" Please try again with a larger bufferSize or accept a truncated version");
                array = new float[bufferSize];
            }
            else
                array = new float[arraySize];

            for (int i = 0; i < array.Length; i++)
                array[i] = buffer[i];
            return array.Length; //we return how many items were safely copied over.
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("ReadFast_FlatFloat2DArray: " + ex.Message);
        }
        array = new float[0];
        return -9000;
    }



    /// <summary> Reads as a 2D array. Slightly slower than FlatFloat2DArray if you're looking to do some post processing on the values. </summary>
    /// <param name="deviceId"></param>
    /// <param name="fieldName"></param>
    /// <param name="bufferSize"></param>
    /// <param name="array"></param>
    /// <returns> value < 0 if something went wrong, otherwise an array </returns>
    public static int ReadFast_Float2DArray(int deviceId, string fieldName, int bufferSize, out float[][] array)
    {
        float[] flatArray;
        int rows, columns;
        int result = ReadFast_FlatFloat2DArray(deviceId, fieldName, bufferSize, out flatArray, out rows, out columns);
        if (result > 0)
        {
            array = new float[rows][];
            for (int r = 0; r < rows; r++)
            {
                array[r] = new float[columns];
                for (int c = 0; c < columns; c++)
                {
                    array[r][c] = flatArray[r * columns + c];
                }
            }
        }
        else
            array = new float[0][];
        return result;
    }




    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FastCom_ReadFast_Float3DArray(
        int deviceId,
        [In] byte[] fieldName,
        int fieldNameCapacity,
        [Out] float[] output,
        int outputCapacity,
        out int out_Rows,
        out int out_Columns,
        out int out_Depth
    );


    public static int ReadFast_FlatFloat3DArray(int deviceId, string fieldName, int bufferSize, out float[] array, out int rows, out int columns, out int depth)
    {
        rows = 0;
        columns = 0;
        depth = 0;
        try
        {
            float[] buffer = new float[bufferSize];
            byte[] utf8Field = System.Text.Encoding.UTF8.GetBytes(fieldName);
            int arraySize = FastCom_ReadFast_Float3DArray(deviceId, utf8Field, utf8Field.Length, buffer, bufferSize, out rows, out columns, out depth);
            if (arraySize < 0) //errror occured with parameters etc
            {
                array = new float[0];
                return arraySize;
            }
            if (arraySize > bufferSize) //we got an array but it's larger than our buffer!
            {   //Not enough size given. Warn the user
                UnityEngine.Debug.LogError($"ReadFast_FlatFloat3DArray for deviceID {deviceId}: Buffer size {bufferSize} is too small to contain float[] of length {arraySize} ({rows}x{columns}x{depth}) in {fieldName}." +
                    $" Please try again with a larger bufferSize or accept a truncated version");
                array = new float[bufferSize];
            }
            else
                array = new float[arraySize];

            for (int i = 0; i < array.Length; i++)
                array[i] = buffer[i];
            return array.Length; //we return how many items were safely copied over.
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("ReadFast_FlatFloat2DArray: " + ex.Message);
        }
        array = new float[0];
        return -9000;
    }


    public static int ReadFast_Float3DArray(int deviceId, string fieldName, int bufferSize, out float[][][] array)
    {
        float[] flatArray;
        int rows, columns, depth;
        int result = ReadFast_FlatFloat3DArray(deviceId, fieldName, bufferSize, out flatArray, out rows, out columns, out depth);
        if (result > 0)
        {
            array = new float[rows][][];
            for (int i = 0; i < rows; i++)
            {
                array[i] = new float[columns][];
                for (int j = 0; j < columns; j++)
                {
                    array[i][j] = new float[depth];
                    for (int k = 0; k < depth; k++)
                    {
                        int index = i * (columns * depth) + j * depth + k;
                        array[i][j][k] = flatArray[index];
                    }
                }
            }
        }
        else
            array = new float[0][][];
        return result;
    }


    //-------------------------------------------------------------------------------------------
    // Generic Write Function(s)


    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FastCom_Writer_RegisterDevice(
        int deviceId
    );

    public static int Writer_RegisterDevice(int deviceId)
    {
        try
        {
            return FastCom_Writer_RegisterDevice(deviceId);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Writer_RegisterDevice: " + ex.Message);
        }
        return -9000;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FastCom_Writer_UnregisterDevice(
        int deviceId
    );

    public static int Writer_UnregisterDevice(int deviceId)
    {
        try
        {
            return FastCom_Writer_UnregisterDevice(deviceId);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Writer_UnregisterDevice: " + ex.Message);
        }
        return -9000;
    }




    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FastCom_WriteBytes_FloatArray(
        int deviceId,
        [In] byte[] fieldName,
        int fieldNameCapacity,
        [In] float[] values,
        int valuesSize
    );

    /// <summary> Write a floating point array to a particular field of a Device with DeviceId. </summary>
    /// <param name="deviceId"></param>
    /// <param name="fieldName"></param>
    /// <param name="values"></param>
    /// <returns> Value < 0 if an error occurs, the values written if succeeded. </returns>
    public static int WriteBytes_FloatArray(int deviceId, string fieldName, float[] values)
    {
        try
        {
            byte[] utf8Field = System.Text.Encoding.UTF8.GetBytes(fieldName);
            return FastCom_WriteBytes_FloatArray(deviceId, utf8Field, utf8Field.Length, values, values.Length);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("ReadFast_FlatFloat2DArray: " + ex.Message);
        }
        return -9000;
    }


    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FastCom_WriteFast_Int32Array(
        int deviceId,
        [In] byte[] fieldName,
        int fieldNameCapacity,
        [In] int[] values,
        int valuesSize
    );

    /// <summary> Write a int array to a particular field of a Device with DeviceId. </summary>
    /// <param name="deviceId"></param>
    /// <param name="fieldName"></param>
    /// <param name="values"></param>
    /// <returns> Value < 0 if an error occurs, the values written if succeeded. </returns>
    public static int WriteFast_Int32Array(int deviceId, string fieldName, int[] values)
    {
        try
        {
            byte[] utf8Field = System.Text.Encoding.UTF8.GetBytes(fieldName);
            return FastCom_WriteFast_Int32Array(deviceId, utf8Field, utf8Field.Length, values, values.Length);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("ReadFast_FlatFloat2DArray: " + ex.Message);
        }
        return -9000;
    }



    //-------------------------------------------------------------------------------------------
    // Debug Logs from the back-end


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FastComLogCallback(
        IntPtr message,
        IntPtr userData);

    [DllImport("FastComUnity", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FastCom_SetLogCallback(
        FastComLogCallback callback,
        IntPtr userData);

    private static FastComLogCallback _callback;

    private static readonly ConcurrentQueue<string> _messages = new();

    public static void InitializeDebugger()
    {
        _callback = OnNativeLog;

        FastCom_SetLogCallback(
            _callback,
            IntPtr.Zero);
    }

    public static void DisposeDebugger()
    {
        FastCom_SetLogCallback(null, IntPtr.Zero);
        _callback = null;
    }

    [MonoPInvokeCallback(typeof(FastComLogCallback))]
    private static void OnNativeLog(
        IntPtr message,
        IntPtr userData)
    {
        if (message == IntPtr.Zero)
            return;

        string text = Marshal.PtrToStringUTF8(message);

        if (!string.IsNullOrEmpty(text))
        {
            _messages.Enqueue(text);
        }
    }


    /// <summary> Clears the list! </summary>
    /// <returns></returns>
    public static List<string> GetQueuedMessages()
    {
        List<string> res = new List<string>(_messages.Count);
        while (_messages.TryDequeue(out string message))
        {
            res.Add(message);
        }
        return res;
    }


    public static void PrintDebugMessages()
    {
        while (_messages.TryDequeue(out string message))
        {
            UnityEngine.Debug.Log(message);
        }
    }
}
