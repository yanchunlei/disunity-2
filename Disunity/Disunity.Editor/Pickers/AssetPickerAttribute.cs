﻿using JetBrains.Annotations;
using UnityEngine;


namespace Disunity.Editor.Pickers {
    public class AssetPickerAttribute : PropertyAttribute {
        [NotNull]
        public string Path;

        public AssetPickerAttribute(string path = "") {
            Path = path;
        }
    }
}