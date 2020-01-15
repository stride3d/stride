using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

[DataContract(DefaultMemberMode = DataMemberMode.Default)]
public abstract class VoxelModifierBase
{
    [DataMember(-20)]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;
}