﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SciSharp.Models
{
    public class TaskOptions
    {
        public string ModelPath { get; set; }
        public string LabelPath { get; set; }
        public string DataDir { get; set; }
        public float TestingPercentage { get; set; } = 0.2f;
        public float ValidationPercentage { get; set; } = 0.1f;
        public TaskOptions()
        {
        }
    }
}