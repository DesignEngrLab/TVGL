#region SharpDxf, Copyright(C) 2012 Lomatus, Licensed under LGPL.

//                        SharpDxf library( Base on netDxf by Daniel Carvajal )
// Copyright (C) 2012 Lomatus (tourszhou@gmail.com)
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 

#endregion

using System;

namespace SharpDxf
{
    internal class DxfException : Exception
    {
        private readonly string file;

        internal DxfException(string file)
        {
            this.file = file;
        }

        internal DxfException(string file, string message)
            : base(message)
        {
            this.file = file;
        }

        internal DxfException(string file, string message, Exception innerException)
            : base(message, innerException)
        {
            this.file = file;
        }

        internal string File
        {
            get { return this.file; }
        }
    }

    #region section exceptions

    internal class DxfHeaderVariableException : DxfException
        {
        private readonly string name;

        internal DxfHeaderVariableException(string name, string file)
            : base(file)
        {
            this.name = name;
        }

        internal DxfHeaderVariableException(string name, string file, string message)
            : base(file, message)
        {
            this.name = name;
        }

        internal string Name
        {
            get { return this.name; }
        }
    }

    internal class DxfSectionException : DxfException
    {
        private readonly string section;

        internal DxfSectionException(string section, string file)
            : base(file)
        {
            this.section = section;
        }

        internal DxfSectionException(string section, string file, string message)
            : base(file, message)
        {
            this.section = section;
        }

        internal string Section
        {
            get { return this.section; }
        }
    }

    internal class InvalidDxfSectionException : DxfSectionException
    {
        internal InvalidDxfSectionException(string section, string file)
            : base(section, file)
        {
        }

        internal InvalidDxfSectionException(string section, string file, string message)
            : base(section, file, message)
        {
        }
    }

    internal class OpenDxfSectionException : DxfSectionException
    {
        internal OpenDxfSectionException(string section, string file)
            : base(section, file)
        {
        }

        internal OpenDxfSectionException(string section, string file, string message)
            : base(section, file, message)
        {
        }
    }

    internal class ClosedDxfSectionException : DxfSectionException
    {
        internal ClosedDxfSectionException(string section, string file)
            : base(section, file)
        {
        }

        internal ClosedDxfSectionException(string section, string file, string message)
            : base(section, file, message)
        {
        }
    }

    #endregion

    #region table exceptions

    internal class DxfTableException : DxfException
    {
        private readonly string table;

        internal DxfTableException(string table, string file)
            : base(file)
        {
            this.table = table;
        }

        internal DxfTableException(string table, string file, string message)
            : base(file, message)
        {
            this.table = table;
        }

        internal string Table
        {
            get { return this.table; }
        }
    }

    internal class InvalidDxfTableException : DxfTableException
    {
        internal InvalidDxfTableException(string table, string file)
            : base(table, file)
        {
        }

        internal InvalidDxfTableException(string table, string file, string message)
            : base(table, file, message)
        {
        }
    }

    internal class OpenDxfTableException : DxfTableException
    {
        internal OpenDxfTableException(string table, string file)
            : base(table, file)
        {
        }

        internal OpenDxfTableException(string table, string file, string message)
            : base(table, file, message)
        {
        }
    }

    internal class ClosedDxfTableException : DxfTableException
    {
        internal ClosedDxfTableException(string table, string file)
            : base(table, file)
        {
        }

        internal ClosedDxfTableException(string table, string file, string message)
            : base(table, file, message)
        {
        }
    }

    #endregion

    #region entity exceptions

    internal class DxfEntityException : DxfException
    {
        private readonly string name;

        internal DxfEntityException(string name, string file, string message)
            : base(file, message)
        {
            this.name = name;
        }

        internal string Name
        {
            get { return this.name; }
        }
    }

    internal class DxfInvalidCodeValueEntityException : DxfException
    {
        private readonly int code;
        private readonly string value;

        internal DxfInvalidCodeValueEntityException(int code, string value, string file, string message)
            : base(file, message)
        {
            this.code = code;
            this.value = value;
        }

        internal int Code
        {
            get { return this.code; }
        }

        internal string Value
        {
            get { return this.value; }
        }
    }

    #endregion
}