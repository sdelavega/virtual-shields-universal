/*
    Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.
 
    The MIT License(MIT)
 
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :
 
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
 
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/
namespace Shield.Core.Models
{
    [Service("LCDT")]
    [Service("LCDG")]
    [Service("LOG")]
    public class ScreenMessage : MessageBase
    {
        public string Message { get; set; }

        public int? X { get; set; }

        public int? Y { get; set; }

        public int? X2 { get; set; }

        public int? Y2 { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public string Tag { get; set; }

        public string Path { get; set; }

        public int? Pid { get; set; }

        public string ARGB { get; set; }

        public string Foreground { get; set; }

        public string HorizontalAlignment { get; set; }

        public int? Value { get; set; }

        public string FlowDirection { get; set; }

        public int? Size { get; set; }

        public bool? Multi { get; set; }
    }
}