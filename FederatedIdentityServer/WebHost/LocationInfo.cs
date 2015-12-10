using System.Diagnostics;
using System.Reflection;

namespace WebHost
{
    public class LocationInfo
    {
        public string AssemblyName
        {
            get { return _assemblyName; }
        }

        public string AssemblyFullName
        {
            get { return _assemblyFullName; }
        }

        public string ClassName
        {
            get { return _className; }
        }

        public string MethodName
        {
            get { return _methodName; }
        }

        public int ILOffset
        {
            get { return _ilOffset; }
        }

        public int NativeOffset
        {
            get { return _nativeOffset; }
        }

        public string FileName
        {
            get { return _fileName; }
        }

        public int LineNumber
        {
            get { return _lineNumber; }
        }

        public int ColumnNumber
        {
            get { return _columnNumber; }
        }

        public LocationInfo(StackFrame locationFrame, bool needFileInfo)
        {
            if (locationFrame != null)
            {
                MethodBase method = locationFrame.GetMethod();
                if (method != null)
                {
                    _methodName = method.Name;

                    var declaringType = method.DeclaringType;
                    if (declaringType != null) // dynamic methods
                    {
                        _className = declaringType.FullName;
                        var assemblyName = declaringType.Assembly.GetName();
                        _assemblyName = assemblyName.Name;
                        _assemblyFullName = assemblyName.FullName;
                    }
                }

                _ilOffset = locationFrame.GetILOffset();
                _nativeOffset = locationFrame.GetNativeOffset();

                if (needFileInfo)
                {
                    _fileName = locationFrame.GetFileName();
                    _lineNumber = locationFrame.GetFileLineNumber();
                    _columnNumber = locationFrame.GetFileColumnNumber();
                }
            }
        }

        private readonly string _assemblyName;
        private readonly string _assemblyFullName;
        private readonly string _className;
        private readonly string _methodName;
        private readonly int _ilOffset;
        private readonly int _nativeOffset;
        private readonly string _fileName;
        private readonly int _lineNumber;
        private readonly int _columnNumber;
    }
}