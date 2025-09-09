#define PRETTY		//Comment out when you no longer need to read JSON to disable pretty Print system-wide
//Using doubles will cause errors in VectorTemplates.cs; Unity speaks floats
#define USEFLOAT	//Use floats for numbers instead of doubles	(enable if you're getting too many significant digits in string output)
//#define POOLING	//Currently using a build setting for this one (also it's experimental)
namespace AniEventTool
{
    using UnityEngine;
    using System;
    using System.IO;
    using System.Text;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Collections.Generic;
    using Debug = UnityEngine.Debug;

    public class JsonObject : IEnumerable
    {
#if POOLING
	const int MAX_POOL_SIZE = 10000;
	public static Queue<JsonObject> releaseQueue = new Queue<JsonObject>();
#endif

        const int MAX_DEPTH = 100;
        const string INFINITY = "\"INFINITY\"";
        const string NEGINFINITY = "\"NEGINFINITY\"";
        const string NaN = "\"NaN\"";
        public static readonly char[] WHITESPACE = { ' ', '\r', '\n', '\t', '\uFEFF', '\u0009' };
        public enum Type { NULL, STRING, NUMBER, OBJECT, ARRAY, BOOL, BAKED }
        public bool isContainer { get { return (type == Type.ARRAY || type == Type.OBJECT); } }
        public Type type = Type.NULL;
        public int Count
        {
            get
            {
                if (list == null)
                    return -1;
                return list.Count;
            }
        }
        public List<JsonObject> list;
        public List<string> keys;
        public string str;
#if USEFLOAT
        public float n;
        public float f
        {
            get
            {
                return n;
            }
        }
#else
        public double n;
        public float f
        {
            get
            {
                return (float)n;
            }
        }
#endif
        public bool useInt;
        public long i;
        public bool b;
        public delegate void AddJSONContents(JsonObject self);

        public static JsonObject nullJO { get { return Create(Type.NULL); } }   //an empty, null object
        public static JsonObject obj { get { return Create(Type.OBJECT); } }        //an empty object
        public static JsonObject arr { get { return Create(Type.ARRAY); } }     //an empty array

        public JsonObject(Type t)
        {
            type = t;
            switch (t)
            {
                case Type.ARRAY:
                    list = new List<JsonObject>();
                    break;
                case Type.OBJECT:
                    list = new List<JsonObject>();
                    keys = new List<string>();
                    break;
            }
        }
        public JsonObject(bool b)
        {
            type = Type.BOOL;
            this.b = b;
        }
#if USEFLOAT
        public JsonObject(float f)
        {
            type = Type.NUMBER;
            n = f;
        }
#else
        public JsonObject(double d)
        {
            type = Type.NUMBER;
            n = d;
        }
#endif
        public JsonObject(int i)
        {
            type = Type.NUMBER;
            this.i = i;
            useInt = true;
            n = i;
        }
        public JsonObject(long l)
        {
            type = Type.NUMBER;
            i = l;
            useInt = true;
            n = l;
        }
        public JsonObject(Dictionary<string, string> dic)
        {
            type = Type.OBJECT;
            keys = new List<string>();
            list = new List<JsonObject>();
            //Not sure if it's worth removing the foreach here
            foreach (KeyValuePair<string, string> kvp in dic)
            {
                keys.Add(kvp.Key);
                list.Add(CreateStringObject(kvp.Value));
            }
        }
        public JsonObject(Dictionary<string, JsonObject> dic)
        {
            type = Type.OBJECT;
            keys = new List<string>();
            list = new List<JsonObject>();
            //Not sure if it's worth removing the foreach here
            foreach (KeyValuePair<string, JsonObject> kvp in dic)
            {
                keys.Add(kvp.Key);
                list.Add(kvp.Value);
            }
        }
        public JsonObject(AddJSONContents content)
        {
            content.Invoke(this);
        }
        public JsonObject(JsonObject[] objs)
        {
            type = Type.ARRAY;
            list = new List<JsonObject>(objs);
        }
        //Convenience function for creating a JsonObject containing a string.  This is not part of the constructor so that malformed JSON data doesn't just turn into a string object
        public static JsonObject StringObject(string val) { return CreateStringObject(val); }
        public void Absorb(JsonObject obj)
        {
            list.AddRange(obj.list);
            keys.AddRange(obj.keys);
            str = obj.str;
            n = obj.n;
            useInt = obj.useInt;
            i = obj.i;
            b = obj.b;
            type = obj.type;
        }
        public static JsonObject Create()
        {
#if POOLING
		JsonObject result = null;
		while(result == null && releaseQueue.Count > 0) {
			result = releaseQueue.Dequeue();
#if DEV
			//The following cases should NEVER HAPPEN (but they do...)
			if(result == null)
				Debug.WriteLine("wtf " + releaseQueue.Count);
			else if(result.list != null)
				Debug.WriteLine("wtflist " + result.list.Count);
#endif
		}
		if(result != null)
			return result;
#endif
            return new JsonObject();
        }
        public static JsonObject Create(Type t)
        {
            JsonObject obj = Create();
            obj.type = t;
            switch (t)
            {
                case Type.ARRAY:
                    obj.list = new List<JsonObject>();
                    break;
                case Type.OBJECT:
                    obj.list = new List<JsonObject>();
                    obj.keys = new List<string>();
                    break;
            }
            return obj;
        }
        public static JsonObject Create(bool val)
        {
            JsonObject obj = Create();
            obj.type = Type.BOOL;
            obj.b = val;
            return obj;
        }
        public static JsonObject Create(float val)
        {
            JsonObject obj = Create();
            obj.type = Type.NUMBER;
            obj.n = val;
            return obj;
        }
        public static JsonObject Create(int val)
        {
            JsonObject obj = Create();
            obj.type = Type.NUMBER;
            obj.n = val;
            obj.useInt = true;
            obj.i = val;
            return obj;
        }
        public static JsonObject Create(long val)
        {
            JsonObject obj = Create();
            obj.type = Type.NUMBER;
            obj.n = val;
            obj.useInt = true;
            obj.i = val;
            return obj;
        }
        public static JsonObject CreateStringObject(string val)
        {
            JsonObject obj = Create();
            obj.type = Type.STRING;
            obj.str = val;
            return obj;
        }
        public static JsonObject CreateBakedObject(string val)
        {
            JsonObject bakedObject = Create();
            bakedObject.type = Type.BAKED;
            bakedObject.str = val;
            return bakedObject;
        }

        /// <summary>
        /// Create a JsonObject by parsing string data
        /// </summary>
        /// <param name="val">The string to be parsed</param>
        /// <param name="maxDepth">The maximum depth for the parser to search.  Set this to to 1 for the first level, 
        /// 2 for the first 2 levels, etc.  It defaults to -2 because -1 is the depth value that is parsed (see below)</param>
        /// <param name="storeExcessLevels">Whether to store levels beyond maxDepth in baked JSONObjects</param>
        /// <param name="strict">Whether to be strict in the parsing. For example, non-strict parsing will successfully 
        /// parse "a string" into a string-type </param>
        /// <returns></returns>
        public static JsonObject Create(string val, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false)
        {
            JsonObject obj = Create();
            obj.Parse(val, maxDepth, storeExcessLevels, strict);
            return obj;
        }
        public static JsonObject Create(AddJSONContents content)
        {
            JsonObject obj = Create();
            content.Invoke(obj);
            return obj;
        }
        public static JsonObject Create(Dictionary<string, string> dic)
        {
            JsonObject obj = Create();
            obj.type = Type.OBJECT;
            obj.keys = new List<string>();
            obj.list = new List<JsonObject>();
            //Not sure if it's worth removing the foreach here
            foreach (KeyValuePair<string, string> kvp in dic)
            {
                obj.keys.Add(kvp.Key);
                obj.list.Add(CreateStringObject(kvp.Value));
            }
            return obj;
        }
        public JsonObject() { }
        #region PARSE
        public JsonObject(string str, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false)
        {   //create a new JsonObject from a string (this will also create any children, and parse the whole string)
            Parse(str, maxDepth, storeExcessLevels, strict);
        }
        void Parse(string str, int maxDepth = -2, bool storeExcessLevels = false, bool strict = false)
        {
            if (!string.IsNullOrEmpty(str))
            {
                str = str.Trim(WHITESPACE);
                if (strict)
                {
                    if (str[0] != '[' && str[0] != '{')
                    {
                        type = Type.NULL;
                        //#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5
                        Debug.LogWarning
                            /*#else
                                                Debug.WriteLine
                            #endif*/
                            ("Improper (strict) JSON formatting.  First character must be [ or {");
                        return;
                    }
                }
                if (str.Length > 0)
                {
#if UNITY_WP8 || UNITY_WSA
				if (str == "true") {
					type = Type.BOOL;
					b = true;
				} else if (str == "false") {
					type = Type.BOOL;
					b = false;
				} else if (str == "null") {
					type = Type.NULL;
#else
                    if (string.Compare(str, "true", true) == 0)
                    {
                        type = Type.BOOL;
                        b = true;
                    }
                    else if (string.Compare(str, "false", true) == 0)
                    {
                        type = Type.BOOL;
                        b = false;
                    }
                    else if (string.Compare(str, "null", true) == 0)
                    {
                        type = Type.NULL;
#endif
#if USEFLOAT
                    }
                    else if (str == INFINITY)
                    {
                        type = Type.NUMBER;
                        n = float.PositiveInfinity;
                    }
                    else if (str == NEGINFINITY)
                    {
                        type = Type.NUMBER;
                        n = float.NegativeInfinity;
                    }
                    else if (str == NaN)
                    {
                        type = Type.NUMBER;
                        n = float.NaN;
#else
                    }
                    else if (str == INFINITY)
                    {
                        type = Type.NUMBER;
                        n = double.PositiveInfinity;
                    }
                    else if (str == NEGINFINITY)
                    {
                        type = Type.NUMBER;
                        n = double.NegativeInfinity;
                    }
                    else if (str == NaN)
                    {
                        type = Type.NUMBER;
                        n = double.NaN;
#endif
                    }
                    else if (str[0] == '"')
                    {
                        type = Type.STRING;
                        this.str = str.Substring(1, str.Length - 2);
                    }
                    else
                    {
                        int tokenTmp = 1;
                        /*
                         * Checking for the following formatting (www.json.org)
                         * object - {"field1":value,"field2":value}
                         * array - [value,value,value]
                         * value - string	- "string"
                         *		 - number	- 0.0
                         *		 - bool		- true -or- false
                         *		 - null		- null
                         */
                        int offset = 0;
                        switch (str[offset])
                        {
                            case '{':
                                type = Type.OBJECT;
                                keys = new List<string>();
                                list = new List<JsonObject>();
                                break;
                            case '[':
                                type = Type.ARRAY;
                                list = new List<JsonObject>();
                                break;
                            default:
                                try
                                {
#if USEFLOAT
                                    n = System.Convert.ToSingle(str);
#else
                                    n = System.Convert.ToDouble(str);
#endif
                                    if (!str.Contains("."))
                                    {
                                        i = System.Convert.ToInt64(str);
                                        useInt = true;
                                    }
                                    type = Type.NUMBER;
                                }
                                catch (System.FormatException)
                                {
                                    type = Type.NULL;
                                    //#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5
                                    Debug.LogWarning
                                    /*#else
                                                                    Debug.WriteLine
                                    #endif*/
                                    ("improper JSON formatting:" + str);
                                }
                                return;
                        }
                        string propName = "";
                        bool openQuote = false;
                        bool inProp = false;
                        int depth = 0;
                        while (++offset < str.Length)
                        {
                            if (System.Array.IndexOf(WHITESPACE, str[offset]) > -1)
                                continue;
                            if (str[offset] == '\\')
                            {
                                offset += 1;
                                continue;
                            }
                            if (str[offset] == '"')
                            {
                                if (openQuote)
                                {
                                    if (!inProp && depth == 0 && type == Type.OBJECT)
                                        propName = str.Substring(tokenTmp + 1, offset - tokenTmp - 1);
                                    openQuote = false;
                                }
                                else
                                {
                                    if (depth == 0 && type == Type.OBJECT)
                                        tokenTmp = offset;
                                    openQuote = true;
                                }
                            }
                            if (openQuote)
                                continue;
                            if (type == Type.OBJECT && depth == 0)
                            {
                                if (str[offset] == ':')
                                {
                                    tokenTmp = offset + 1;
                                    inProp = true;
                                }
                            }

                            if (str[offset] == '[' || str[offset] == '{')
                            {
                                depth++;
                            }
                            else if (str[offset] == ']' || str[offset] == '}')
                            {
                                depth--;
                            }
                            //if  (encounter a ',' at top level)  || a closing ]/}
                            if ((str[offset] == ',' && depth == 0) || depth < 0)
                            {
                                inProp = false;
                                string inner = str.Substring(tokenTmp, offset - tokenTmp).Trim(WHITESPACE);
                                if (inner.Length > 0)
                                {
                                    if (type == Type.OBJECT)
                                        keys.Add(propName);
                                    if (maxDepth != -1)                                                         //maxDepth of -1 is the end of the line
                                        list.Add(Create(inner, (maxDepth < -1) ? -2 : maxDepth - 1));
                                    else if (storeExcessLevels)
                                        list.Add(CreateBakedObject(inner));

                                }
                                tokenTmp = offset + 1;
                            }
                        }
                    }
                }
                else type = Type.NULL;
            }
            else type = Type.NULL;  //If the string is missing, this is a null
                                    //Profiler.EndSample();
        }
        #endregion
        public bool IsNumber { get { return type == Type.NUMBER; } }
        public bool IsNull { get { return type == Type.NULL; } }
        public bool IsString { get { return type == Type.STRING; } }
        public bool IsBool { get { return type == Type.BOOL; } }
        public bool IsArray { get { return type == Type.ARRAY; } }
        public bool IsObject { get { return type == Type.OBJECT || type == Type.BAKED; } }
        public void Add(bool val)
        {
            Add(Create(val));
        }
        public void Add(float val)
        {
            Add(Create(val));
        }
        public void Add(int val)
        {
            Add(Create(val));
        }
        public void Add(string str)
        {
            Add(CreateStringObject(str));
        }
        public void Add(AddJSONContents content)
        {
            Add(Create(content));
        }
        public void Add(JsonObject obj)
        {
            if (obj)
            {       //Don't do anything if the object is null
                if (type != Type.ARRAY)
                {
                    type = Type.ARRAY;      //Congratulations, son, you're an ARRAY now
                    if (list == null)
                        list = new List<JsonObject>();
                }
                list.Add(obj);
            }
        }
        public void AddField(string name, bool val)
        {
            AddField(name, Create(val));
        }
        public void AddField(string name, float val)
        {
            AddField(name, Create(val));
        }
        public void AddField(string name, int val)
        {
            AddField(name, Create(val));
        }
        public void AddField(string name, long val)
        {
            AddField(name, Create(val));
        }
        public void AddField(string name, AddJSONContents content)
        {
            AddField(name, Create(content));
        }
        public void AddField(string name, string val)
        {
            AddField(name, CreateStringObject(val));
        }
        public void AddField(string name, JsonObject obj)
        {
            if (obj)
            {       //Don't do anything if the object is null
                if (type != Type.OBJECT)
                {
                    if (keys == null)
                        keys = new List<string>();
                    if (type == Type.ARRAY)
                    {
                        for (int i = 0; i < list.Count; i++)
                            keys.Add(i + "");
                    }
                    else
                        if (list == null)
                        list = new List<JsonObject>();
                    type = Type.OBJECT;     //Congratulations, son, you're an OBJECT now
                }
                keys.Add(name);
                list.Add(obj);
            }
        }
        public void SetField(string name, string val) { SetField(name, CreateStringObject(val)); }
        public void SetField(string name, bool val) { SetField(name, Create(val)); }
        public void SetField(string name, float val) { SetField(name, Create(val)); }
        public void SetField(string name, int val) { SetField(name, Create(val)); }
        public void SetField(string name, JsonObject obj)
        {
            if (HasField(name))
            {
                list.Remove(this[name]);
                keys.Remove(name);
            }
            AddField(name, obj);
        }
        public void RemoveField(string name)
        {
            if (keys.IndexOf(name) > -1)
            {
                list.RemoveAt(keys.IndexOf(name));
                keys.Remove(name);
            }
        }
        public delegate void FieldNotFound(string name);
        public delegate void GetFieldResponse(JsonObject obj);
        public bool GetField(out bool field, string name, bool fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }
        public bool GetField(ref bool field, string name, FieldNotFound fail = null)
        {
            if (type == Type.OBJECT)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = list[index].b;
                    return true;
                }
            }
            if (fail != null) fail.Invoke(name);
            return false;
        }
#if USEFLOAT
        public bool GetField(out float field, string name, float fallback)
        {
#else
        public bool GetField(out double field, string name, double fallback)
        {
#endif
            field = fallback;
            return GetField(ref field, name);
        }
#if USEFLOAT
        public bool GetField(ref float field, string name, FieldNotFound fail = null)
        {
#else
        public bool GetField(ref double field, string name, FieldNotFound fail = null)
        {
#endif
            if (type == Type.OBJECT)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = list[index].n;
                    return true;
                }
            }
            if (fail != null) fail.Invoke(name);
            return false;
        }
        public bool GetField(out int field, string name, int fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }
        public bool GetField(ref int field, string name, FieldNotFound fail = null)
        {
            if (IsObject)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = (int)list[index].n;
                    return true;
                }
            }
            if (fail != null) fail.Invoke(name);
            return false;
        }
        public bool GetField(out long field, string name, long fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }
        public bool GetField(ref long field, string name, FieldNotFound fail = null)
        {
            if (IsObject)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = (long)list[index].n;
                    return true;
                }
            }
            if (fail != null) fail.Invoke(name);
            return false;
        }
        public bool GetField(out uint field, string name, uint fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }
        public bool GetField(ref uint field, string name, FieldNotFound fail = null)
        {
            if (IsObject)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = (uint)list[index].n;
                    return true;
                }
            }
            if (fail != null) fail.Invoke(name);
            return false;
        }
        public bool GetField(out string field, string name, string fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }
        public bool GetField(ref string field, string name, FieldNotFound fail = null)
        {
            if (IsObject)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = list[index].str;
                    return true;
                }
            }
            if (fail != null) fail.Invoke(name);
            return false;
        }
        public void GetField(string name, GetFieldResponse response, FieldNotFound fail = null)
        {
            if (response != null && IsObject)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    response.Invoke(list[index]);
                    return;
                }
            }
            if (fail != null) fail.Invoke(name);
        }
        public JsonObject GetField(string name)
        {
            if (IsObject)
                for (int i = 0; i < keys.Count; i++)
                    if (keys[i] == name)
                        return list[i];
            return null;
        }
        public bool HasFields(string[] names)
        {
            if (!IsObject)
                return false;
            for (int i = 0; i < names.Length; i++)
                if (!keys.Contains(names[i]))
                    return false;
            return true;
        }
        public bool HasField(string name)
        {
            if (!IsObject)
                return false;
            for (int i = 0; i < keys.Count; i++)
                if (keys[i] == name)
                    return true;
            return false;
        }
        public void Clear()
        {
            type = Type.NULL;
            if (list != null)
                list.Clear();
            if (keys != null)
                keys.Clear();
            str = "";
            n = 0;
            b = false;
        }
        /// <summary>
        /// Copy a JsonObject. This could probably work better
        /// </summary>
        /// <returns></returns>
        public JsonObject Copy()
        {
            return Create(Print());
        }
        /*
         * The Merge function is experimental. Use at your own risk.
         */
        public void Merge(JsonObject obj)
        {
            MergeRecur(this, obj);
        }
        /// <summary>
        /// Merge object right into left recursively
        /// </summary>
        /// <param name="left">The left (base) object</param>
        /// <param name="right">The right (new) object</param>
        static void MergeRecur(JsonObject left, JsonObject right)
        {
            if (left.type == Type.NULL)
                left.Absorb(right);
            else if (left.type == Type.OBJECT && right.type == Type.OBJECT)
            {
                for (int i = 0; i < right.list.Count; i++)
                {
                    string key = right.keys[i];
                    if (right[i].isContainer)
                    {
                        if (left.HasField(key))
                            MergeRecur(left[key], right[i]);
                        else
                            left.AddField(key, right[i]);
                    }
                    else
                    {
                        if (left.HasField(key))
                            left.SetField(key, right[i]);
                        else
                            left.AddField(key, right[i]);
                    }
                }
            }
            else if (left.type == Type.ARRAY && right.type == Type.ARRAY)
            {
                if (right.Count > left.Count)
                {
                    //#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5
                    Debug.LogError
                    /*#else
                                    Debug.WriteLine
                    #endif*/
                    ("Cannot merge arrays when right object has more elements");
                    return;
                }
                for (int i = 0; i < right.list.Count; i++)
                {
                    if (left[i].type == right[i].type)
                    {           //Only overwrite with the same type
                        if (left[i].isContainer)
                            MergeRecur(left[i], right[i]);
                        else
                        {
                            left[i] = right[i];
                        }
                    }
                }
            }
        }
        public void Bake()
        {
            if (type != Type.BAKED)
            {
                str = Print();
                type = Type.BAKED;
            }
        }
        public IEnumerable BakeAsync()
        {
            if (type != Type.BAKED)
            {
                foreach (string s in PrintAsync())
                {
                    if (s == null)
                        yield return s;
                    else
                    {
                        str = s;
                    }
                }
                type = Type.BAKED;
            }
        }
#pragma warning disable 219
        public string Print(bool pretty = false)
        {
            StringBuilder builder = new StringBuilder();
            Stringify(0, builder, pretty);
            return builder.ToString();
        }
        public IEnumerable<string> PrintAsync(bool pretty = false)
        {
            StringBuilder builder = new StringBuilder();
            printWatch.Reset();
            printWatch.Start();
            foreach (IEnumerable e in StringifyAsync(0, builder, pretty))
            {
                yield return null;
            }
            yield return builder.ToString();
        }
#pragma warning restore 219
        #region STRINGIFY
        const float maxFrameTime = 0.008f;
        static readonly Stopwatch printWatch = new Stopwatch();
        IEnumerable StringifyAsync(int depth, StringBuilder builder, bool pretty = false)
        {   //Convert the JsonObject into a string
            //Profiler.BeginSample("JSONprint");
            if (depth++ > MAX_DEPTH)
            {
                //#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5
                Debug.Log
                /*#else
                            Debug.WriteLine
                #endif*/
                ("reached max depth!");
                yield break;
            }
            if (printWatch.Elapsed.TotalSeconds > maxFrameTime)
            {
                printWatch.Reset();
                yield return null;
                printWatch.Start();
            }
            switch (type)
            {
                case Type.BAKED:
                    builder.Append(str);
                    break;
                case Type.STRING:
                    builder.AppendFormat("\"{0}\"", str);
                    break;
                case Type.NUMBER:
                    if (useInt)
                    {
                        builder.Append(i.ToString());
                    }
                    else
                    {
#if USEFLOAT
                        if (float.IsInfinity(n))
                            builder.Append(INFINITY);
                        else if (float.IsNegativeInfinity(n))
                            builder.Append(NEGINFINITY);
                        else if (float.IsNaN(n))
                            builder.Append(NaN);
#else
                        if (double.IsInfinity(n))
                            builder.Append(INFINITY);
                        else if (double.IsNegativeInfinity(n))
                            builder.Append(NEGINFINITY);
                        else if (double.IsNaN(n))
                            builder.Append(NaN);
#endif
                        else
                            builder.Append(n.ToString());
                    }
                    break;
                case Type.OBJECT:
                    builder.Append("{");
                    if (list.Count > 0)
                    {
#if (PRETTY)        //for a bit more readability, comment the define above to disable system-wide                                                                                  
                        if (pretty)
                            builder.Append("\n");
#endif
                        for (int i = 0; i < list.Count; i++)
                        {
                            string key = keys[i];
                            JsonObject obj = list[i];
                            if (obj)
                            {
#if (PRETTY)
                                if (pretty)
                                    for (int j = 0; j < depth; j++)
                                        builder.Append("\t"); //for a bit more readability
#endif
                                builder.AppendFormat("\"{0}\":", key);
                                foreach (IEnumerable e in obj.StringifyAsync(depth, builder, pretty))
                                    yield return e;
                                builder.Append(",");
#if (PRETTY)
                                if (pretty)
                                    builder.Append("\n");
#endif
                            }
                        }
#if (PRETTY)
                        if (pretty)
                            builder.Length -= 2;
                        else
#endif
                            builder.Length--;
                    }
#if (PRETTY)
                    if (pretty && list.Count > 0)
                    {
                        builder.Append("\n");
                        for (int j = 0; j < depth - 1; j++)
                            builder.Append("\t"); //for a bit more readability
                    }
#endif
                    builder.Append("}");
                    break;
                case Type.ARRAY:
                    builder.Append("[");
                    if (list.Count > 0)
                    {
#if (PRETTY)
                        if (pretty)
                            builder.Append("\n"); //for a bit more readability
#endif
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i])
                            {
#if (PRETTY)
                                if (pretty)
                                    for (int j = 0; j < depth; j++)
                                        builder.Append("\t"); //for a bit more readability
#endif
                                foreach (IEnumerable e in list[i].StringifyAsync(depth, builder, pretty))
                                    yield return e;
                                builder.Append(",");
#if (PRETTY)
                                if (pretty)
                                    builder.Append("\n"); //for a bit more readability
#endif
                            }
                        }
#if (PRETTY)
                        if (pretty)
                            builder.Length -= 2;
                        else
#endif
                            builder.Length--;
                    }
#if (PRETTY)
                    if (pretty && list.Count > 0)
                    {
                        builder.Append("\n");
                        for (int j = 0; j < depth - 1; j++)
                            builder.Append("\t"); //for a bit more readability
                    }
#endif
                    builder.Append("]");
                    break;
                case Type.BOOL:
                    if (b)
                        builder.Append("true");
                    else
                        builder.Append("false");
                    break;
                case Type.NULL:
                    builder.Append("null");
                    break;
            }
            //Profiler.EndSample();
        }
        //TODO: Refactor Stringify functions to share core logic
        /*
         * I know, I know, this is really bad form.  It turns out that there is a
         * significant amount of garbage created when calling as a coroutine, so this
         * method is duplicated.  Hopefully there won't be too many future changes, but
         * I would still like a more elegant way to optionaly yield
         */
        void Stringify(int depth, StringBuilder builder, bool pretty = false)
        {   //Convert the JsonObject into a string
            //Profiler.BeginSample("JSONprint");
            if (depth++ > MAX_DEPTH)
            {
                //#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5
                Debug.Log
                /*#else
                            Debug.WriteLine
                #endif*/
                ("reached max depth!");
                return;
            }
            switch (type)
            {
                case Type.BAKED:
                    builder.Append(str);
                    break;
                case Type.STRING:
                    builder.AppendFormat("\"{0}\"", str);
                    break;
                case Type.NUMBER:
                    if (useInt)
                    {
                        builder.Append(i.ToString());
                    }
                    else
                    {
#if USEFLOAT
                        if (float.IsInfinity(n))
                            builder.Append(INFINITY);
                        else if (float.IsNegativeInfinity(n))
                            builder.Append(NEGINFINITY);
                        else if (float.IsNaN(n))
                            builder.Append(NaN);
#else
                        if (double.IsInfinity(n))
                            builder.Append(INFINITY);
                        else if (double.IsNegativeInfinity(n))
                            builder.Append(NEGINFINITY);
                        else if (double.IsNaN(n))
                            builder.Append(NaN);
#endif
                        else
                            builder.Append(n.ToString());
                    }
                    break;
                case Type.OBJECT:
                    builder.Append("{");
                    if (list.Count > 0)
                    {
#if (PRETTY)        //for a bit more readability, comment the define above to disable system-wide                                                                                  
                        if (pretty)
                            builder.Append("\n");
#endif
                        for (int i = 0; i < list.Count; i++)
                        {
                            string key = keys[i];
                            JsonObject obj = list[i];
                            if (obj)
                            {
#if (PRETTY)
                                if (pretty)
                                    for (int j = 0; j < depth; j++)
                                        builder.Append("\t"); //for a bit more readability
#endif
                                builder.AppendFormat("\"{0}\":", key);
                                obj.Stringify(depth, builder, pretty);
                                builder.Append(",");
#if (PRETTY)
                                if (pretty)
                                    builder.Append("\n");
#endif
                            }
                        }
#if (PRETTY)
                        if (pretty)
                            builder.Length -= 2;
                        else
#endif
                            builder.Length--;
                    }
#if (PRETTY)
                    if (pretty && list.Count > 0)
                    {
                        builder.Append("\n");
                        for (int j = 0; j < depth - 1; j++)
                            builder.Append("\t"); //for a bit more readability
                    }
#endif
                    builder.Append("}");
                    break;
                case Type.ARRAY:
                    builder.Append("[");
                    if (list.Count > 0)
                    {
#if (PRETTY)
                        if (pretty)
                            builder.Append("\n"); //for a bit more readability
#endif
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i])
                            {
#if (PRETTY)
                                if (pretty)
                                    for (int j = 0; j < depth; j++)
                                        builder.Append("\t"); //for a bit more readability
#endif
                                list[i].Stringify(depth, builder, pretty);
                                builder.Append(",");
#if (PRETTY)
                                if (pretty)
                                    builder.Append("\n"); //for a bit more readability
#endif
                            }
                        }
#if (PRETTY)
                        if (pretty)
                            builder.Length -= 2;
                        else
#endif
                            builder.Length--;
                    }
#if (PRETTY)
                    if (pretty && list.Count > 0)
                    {
                        builder.Append("\n");
                        for (int j = 0; j < depth - 1; j++)
                            builder.Append("\t"); //for a bit more readability
                    }
#endif
                    builder.Append("]");
                    break;
                case Type.BOOL:
                    if (b)
                        builder.Append("true");
                    else
                        builder.Append("false");
                    break;
                case Type.NULL:
                    builder.Append("null");
                    break;
            }
            //Profiler.EndSample();
        }
        #endregion
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5
	public static implicit operator WWWForm(JsonObject obj) {
		WWWForm form = new WWWForm();
		for(int i = 0; i < obj.list.Count; i++) {
			string key = i + "";
			if(obj.type == Type.OBJECT)
				key = obj.keys[i];
			string val = obj.list[i].ToString();
			if(obj.list[i].type == Type.STRING)
				val = val.Replace("\"", "");
			form.AddField(key, val);
		}
		return form;
	}
#endif
        public JsonObject this[int index]
        {
            get
            {
                if (list.Count > index) return list[index];
                return null;
            }
            set
            {
                if (list.Count > index)
                    list[index] = value;
            }
        }
        public JsonObject this[string index]
        {
            get
            {
                return GetField(index);
            }
            set
            {
                SetField(index, value);
            }
        }
        public override string ToString()
        {
            return Print();
        }
        public string ToString(bool pretty)
        {
            return Print(pretty);
        }
        public Dictionary<string, string> ToDictionary()
        {
            if (type == Type.OBJECT)
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                for (int i = 0; i < list.Count; i++)
                {
                    JsonObject val = list[i];
                    switch (val.type)
                    {
                        case Type.STRING: result.Add(keys[i], val.str); break;
                        case Type.NUMBER: result.Add(keys[i], val.n + ""); break;
                        case Type.BOOL: result.Add(keys[i], val.b + ""); break;
                        default:
                            //#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5
                            Debug.LogWarning
                            /*#else
                                                    Debug.WriteLine
                            #endif*/
                            ("Omitting object: " + keys[i] + " in dictionary conversion");
                            break;
                    }
                }
                return result;
            }
            //#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5
            Debug.Log
            /*#else
                    Debug.WriteLine
            #endif*/
            ("Tried to turn non-Object JsonObject into a dictionary");
            return null;
        }
        public static implicit operator bool(JsonObject o)
        {
            return o != null;
        }
#if POOLING
	static bool pool = true;
	public static void ClearPool() {
		pool = false;
		releaseQueue.Clear();
		pool = true;
	}

	~JsonObject() {
		if(pool && releaseQueue.Count < MAX_POOL_SIZE) {
			type = Type.NULL;
			list = null;
			keys = null;
			str = "";
			n = 0;
			b = false;
			releaseQueue.Enqueue(this);
		}
	}
#endif

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public JsonObjectEnumer GetEnumerator()
        {
            return new JsonObjectEnumer(this);
        }
    }



    public class JsonObjectEnumer : IEnumerator
    {
        public JsonObject _jobj;

        // Enumerators are positioned before the first element
        // until the first MoveNext() call.
        int position = -1;

        public JsonObjectEnumer(JsonObject jsonObject)
        {
            Debug.Assert(jsonObject.isContainer); //must be an array or object to itterate
            _jobj = jsonObject;
        }

        public bool MoveNext()
        {
            position++;
            return (position < _jobj.Count);
        }

        public void Reset()
        {
            position = -1;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public JsonObject Current
        {
            get
            {
                if (_jobj.IsArray)
                {
                    return _jobj[position];
                }
                else
                {
                    string key = _jobj.keys[position];
                    return _jobj[key];
                }
            }
        }
    }




    public static class JsonUtil
    {
        public static void InitTransform(GameObject gObj)
        {
            gObj.transform.localPosition = Vector3.zero;
            gObj.transform.localRotation = Quaternion.identity;
            gObj.transform.localScale = Vector3.one;
        }

        public static void InitTransform(GameObject gObj, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            gObj.transform.localPosition = pos;
            gObj.transform.localRotation = rot;
            gObj.transform.localScale = scale;
        }

        public static string GetNameFromPath(string path)
        {
            string[] strs = path.Split('/');
            if (strs.Length > 0)
                return strs[strs.Length - 1];
            return "";
        }
        public static string GetPrefabPath(string fullPath)
        {
            const string resourcesFolder = "Resources/";

            // Find the index after "Resources/"
            int startIndex = fullPath.IndexOf(resourcesFolder) + resourcesFolder.Length;
            if (startIndex < resourcesFolder.Length) // "Resources/" not found
            {
                return null;
            }

            // Find the last index of "/"
            int lastIndex = fullPath.LastIndexOf('/');
            if (lastIndex == -1 || lastIndex < startIndex) // "/" not found or in the wrong place
            {
                return null;
            }

            // Extract the substring
            return fullPath.Substring(startIndex, lastIndex - startIndex) + '/';
        }
        public static string GetPrefabNameFromPath(string path)
        {
            string[] strs = path.Split('/');
            if (strs.Length > 0)
            {
                string[] parts = strs[strs.Length - 1].Split(new[] { ".prefab" }, StringSplitOptions.None);

                return parts[0];
            }
            else
                return string.Empty;
        }

        public static string GetFolderFromPath(string path)
        {
            string str = path;

            int lastIndex = str.IndexOf('/');
            if (lastIndex >= 0)
                str = str.Substring(0, lastIndex);

            return str;
        }

        public static void DeleteExt(ref string str)
        {
            int lastIndex = str.LastIndexOf('.');
            if (lastIndex >= 0)
                str = str.Substring(0, lastIndex);
        }

        /// <summary>
        /// �ҷ��� ���� ������Ʈ���� ���̴��� ���������ش�. ������ ����
        /// </summary>
        /// <param name="obj"></param>
        public static void ReassignShader(GameObject obj)
        {
            Renderer[] renderers = obj.transform.GetComponentsInChildren<Renderer>(true);
            string shaderName = "";

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].materials != null)
                {
                    for (int j = 0; j < renderers[i].materials.Length; j++)
                    {
                        shaderName = renderers[i].materials[j].shader.name;
                        renderers[i].materials[j].shader = Shader.Find(shaderName);
                    }
                }
            }
        }
        public static void ChangeLayersRecursively(Transform trans, string name)
        {
            trans.gameObject.layer = LayerMask.NameToLayer(name);
            foreach (Transform child in trans)
            {
                ChangeLayersRecursively(child, name);
            }
        }

        public static T GetComponentInChildrenByName<T>(GameObject parent, string componentName) where T : UnityEngine.Object
        {
            T[] children = parent.GetComponentsInChildren<T>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == componentName)
                    return children[i];
            }

            return null;
        }

        public static bool IsInViewportPoint(float pos)
        {
            if (pos >= 0.0f && pos <= 1.0f)
                return true;

            return false;
        }

        public static float EulerAngleToSignedAngle(float angle)
        {
            return (angle > 180) ? angle - 360 : angle;
        }

        public static void StopCoroutine(MonoBehaviour monoBehaviour, Coroutine cr)
        {
            if (cr == null)
                return;

            monoBehaviour.StopCoroutine(cr);
            cr = null;
        }
        public static Vector3 Bezier(Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float mum1, mum12, mu2;
            Vector3 p;

            mu2 = t * t;
            mum1 = 1 - t;
            mum12 = mum1 * mum1;

            p.x = p1.x * mum12 + 2 * p2.x * mum1 * t + p3.x * mu2;
            p.y = p1.y * mum12 + 2 * p2.y * mum1 * t + p3.y * mu2;
            p.z = p1.z * mum12 + 2 * p2.z * mum1 * t + p3.z * mu2;

            return p;
        }

        public static byte[] StructureToPtr(object data)
        {
            int size = Marshal.SizeOf(data);
            byte[] bytes = new byte[size];

            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);

            Marshal.StructureToPtr(data, ptr, false);
            return bytes;
        }

        public static T PtrToStructure<T>(byte[] bytes)
        {
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
            T t = (T)Marshal.PtrToStructure(ptr, typeof(T));

            return t;
        }

        public static int BytesCopy(Int32 value, Array dest, int destOffset)
        {
            byte[] data = BitConverter.GetBytes(value);
            Buffer.BlockCopy(data, 0, dest, destOffset, data.Length);

            return data.Length;
        }

        public static int BytesCopy(Int64 value, Array dest, int destOffset)
        {
            byte[] data = BitConverter.GetBytes(value);
            Buffer.BlockCopy(data, 0, dest, destOffset, data.Length);

            return data.Length;
        }

        public static int BytesCopy(UInt32 value, Array dest, int destOffset)
        {
            byte[] data = BitConverter.GetBytes(value);
            Buffer.BlockCopy(data, 0, dest, destOffset, data.Length);

            return data.Length;
        }

        public static int BytesCopy(ushort value, Array dest, int destOffset)
        {
            byte[] data = BitConverter.GetBytes(value);
            Buffer.BlockCopy(data, 0, dest, destOffset, data.Length);

            return data.Length;
        }

        public static int BytesCopy(byte value, Array dest, int destOffset)
        {
            dest.SetValue(value, destOffset);
            return 1;
        }

        public static int BytesCopy(sbyte value, Array dest, int destOffset)
        {
            dest.SetValue((byte)value, destOffset);
            return 1;
        }

        public static Int16 ToInt16(byte[] bytes, ref int offset)
        {
            Int16 value = BitConverter.ToInt16(bytes, offset);
            offset += sizeof(Int16);

            return value;
        }

        public static Int32 ToInt32(byte[] bytes, ref int offset)
        {
            Int32 value = BitConverter.ToInt32(bytes, offset);
            offset += sizeof(Int32);

            return value;
        }

        public static Int64 ToInt64(byte[] bytes, ref int offset)
        {
            Int64 value = BitConverter.ToInt64(bytes, offset);
            offset += sizeof(Int64);

            return value;
        }

        public static UInt16 ToUInt16(byte[] bytes, ref int offset)
        {
            UInt16 value = BitConverter.ToUInt16(bytes, offset);
            offset += sizeof(UInt16);

            return value;
        }

        public static UInt32 ToUInt32(byte[] bytes, ref int offset)
        {
            UInt32 value = BitConverter.ToUInt32(bytes, offset);
            offset += sizeof(UInt32);

            return value;
        }

        public static UInt64 ToUInt64(byte[] bytes, ref int offset)
        {
            UInt64 value = BitConverter.ToUInt64(bytes, offset);
            offset += sizeof(UInt64);

            return value;
        }

        public static float ToFloat(byte[] bytes, ref int offset)
        {
            float value = BitConverter.ToSingle(bytes, offset);
            offset += sizeof(float);

            return value;
        }

        public static bool ToBool(byte[] bytes, ref int offset)
        {
            bool value = BitConverter.ToBoolean(bytes, offset);
            offset += sizeof(bool);

            return value;
        }

        public static byte ToByte(byte[] bytes, ref int offset)
        {
            byte value = bytes[offset];
            ++offset;

            return value;
        }

        public static sbyte ToSByte(byte[] bytes, ref int offset)
        {
            sbyte value = (sbyte)bytes[offset];
            ++offset;

            return value;
        }

        public static string ToString(byte[] bytes, ref int offset, int length)
        {
            string str = System.Text.Encoding.UTF8.GetString(bytes, offset, length);
            offset += length;

            return str;
        }

        public static byte[] BlockCopy(byte[] src, ref int srcOffset, Type destType)
        {
            int size = Marshal.SizeOf(destType);
            byte[] bytes = new byte[size];

            Buffer.BlockCopy(src, srcOffset, bytes, 0, size);

            srcOffset += size;
            return bytes;
        }

        public static string ReadString(BinaryReader br, System.Text.Encoding encoding)
        {
            UInt16 len = br.ReadUInt16();
            return encoding.GetString(br.ReadBytes(len));
        }
    }
}