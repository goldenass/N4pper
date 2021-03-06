﻿using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest.TestModel
{
    public class TestRel : ITestEntity
    {
        public int Id { get; set; }
        public int Integer { get; set; }
        public int? IntegerNullable { get; set; }
        public double Double { get; set; }
        public DateTime DateTime { get; set; }
        public DateTime? DateTimeNullable { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public DateTimeOffset? DateTimeOffsetNullable { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public TimeSpan? TimeSpanNullable { get; set; }
        public string String { get; set; }
        public Guid GuidValue { get; set; }

        public object Object { get; set; }
        public int ReadonlyInt { get; }
        public int WriteonlyInt { private get; set; }
        public TestEnum EnumValue { get; set; }
        public ICollection<string> Values { get; set; }
        public ICollection<int> ValuesInt { get; set; }
        public ICollection<object> ValuesObject { get; set; }
    }
}
