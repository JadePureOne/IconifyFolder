﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace IconifyFolder.Models
{
    public partial class ProgramItem : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _filePath;

        [ObservableProperty]
        private string _folderPath;

        [ObservableProperty]
        private Icon _icon;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isCloseMatch;
    }
}